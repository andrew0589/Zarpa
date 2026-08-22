# Deploy pe VPS (Dokploy) — navigationes.eu

| | |
|---|---|
| VPS | `72.60.92.50` (Hostinger, Dokploy — același pe care rulează Church Runner) |
| Site | `https://navigationes.eu` + `https://www.navigationes.eu` — Blazor WebAssembly servit de nginx |
| API | `https://api.navigationes.eu` — ASP.NET Core |
| Bază de date | `NavigationESDb`, în containerul `mssql` care rulează deja pentru Church Runner |
| Stack | [docker-compose.dokploy.yml](docker-compose.dokploy.yml) |

Site-ul e WebAssembly: se compilează în fișiere statice și rulează integral în browser.
Serverul nu execută nimic din el — de aceea imaginea de runtime e doar nginx.

---

## Pasul 1 — DNS

La registrarul domeniului `navigationes.eu`, trei înregistrări **A**:

| Tip | Nume | Valoare | TTL |
|---|---|---|---|
| A | `@` | `72.60.92.50` | 3600 |
| A | `www` | `72.60.92.50` | 3600 |
| A | `api` | `72.60.92.50` | 3600 |

Fă asta **înainte** de deploy: Let's Encrypt validează prin HTTP-01, deci dacă DNS-ul
nu s-a propagat, Traefik nu poate emite certificatul și rămâi cu eroare de TLS.

Verifică propagarea înainte să mergi mai departe:

```bash
dig +short navigationes.eu api.navigationes.eu www.navigationes.eu
# toate trei trebuie să răspundă 72.60.92.50
```

---

## Pasul 2 — Pregătește baza de date (o singură dată)

NavigationES folosește instanța SQL Server existentă, cu bază și login proprii.
Toate comenzile se dau prin SSH pe VPS: `ssh root@72.60.92.50`.

### 2a. Fă `mssql` vizibil pe rețeaua Dokploy

Containerul `mssql` e acum doar pe `church-network`, deci API-ul NavigationES nu-l vede.
Îl atașezi și la `dokploy-network`, rețeaua partajată de toate stack-urile:

```bash
docker network connect dokploy-network mssql
docker network inspect dokploy-network --format '{{range .Containers}}{{.Name}} {{end}}'
# în listă trebuie să apară `mssql`
```

Comanda de mai sus e live, fără restart și fără downtime pentru Church — dar **nu
supraviețuiește unui redeploy** al stack-ului Church. Ca să fie permanentă, adaugă
în `C:\Sermon\docker-compose.dokploy.yml`, la serviciul `mssql`:

```yaml
    networks:
      - church-network
      - dokploy-network      # <-- linia nouă
```

(`dokploy-network` e deja declarată ca `external: true` la finalul acelui fișier.)
Aplic-o la următorul redeploy al Church — atunci containerul se recreează, deci
alege un moment în care o întrerupere de câteva zeci de secunde e ok.

### 2b. Creează baza și login-ul

Nu folosim `sa`: un login dedicat, cu drepturi doar pe `NavigationESDb`, înseamnă că
o scurgere a credențialelor NavigationES nu atinge `ChurchDb`.

```bash
SA_PASS='<parola sa a containerului mssql>'        # DB_PASSWORD din stack-ul Church
NAV_PASS='<parolă nouă, lungă, aleatoare>'         # devine DB_PASSWORD la NavigationES

docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASS" -C -Q "
CREATE DATABASE NavigationESDb;
"

docker exec -i mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASS" -C -Q "
CREATE LOGIN navigationes WITH PASSWORD = '$NAV_PASS', CHECK_POLICY = ON;
USE NavigationESDb;
CREATE USER navigationes FOR LOGIN navigationes;
ALTER ROLE db_owner ADD MEMBER navigationes;
"
```

`db_owner` e necesar pentru că API-ul rulează migrațiile EF Core la pornire și trebuie
să poată crea tabele.

Dacă `mssql-tools18` nu există în imagine, încearcă `/opt/mssql-tools/bin/sqlcmd` și
scoate flag-ul `-C`.

---

## Pasul 3 — Creează aplicația în Dokploy

1. Deschide dashboard-ul Dokploy și **Create Application** → tip **Docker Compose**
   (nu *Stack* — compose-ul folosește `build:` și `container_name`, care nu merg în Swarm).
2. Nume: `navigationes`.
3. **Provider**: Git → `https://github.com/andrew0589/Zarpa.git`, branch `main`.
   Dacă repo-ul e privat, conectează întâi contul GitHub din *Settings → Git Providers*.
4. **Compose Path**: `docker-compose.dokploy.yml`.
5. **Nu** adăuga nimic în tab-ul *Domains*: rutarea e deja scrisă ca label-uri Traefik
   în compose. Dacă adaugi și acolo, ajungi cu două routere pe același host.

## Pasul 4 — Variabile de mediu

În tab-ul **Environment** al aplicației, lipește cheile din
[.env.production.example](.env.production.example) și completează valorile reale.
Minimul ca să pornească:

```
DB_USER=navigationes
DB_PASSWORD=<NAV_PASS de la pasul 2b>
JWT_SECRET=<openssl rand -base64 48>
ADMIN_API_KEY=<openssl rand -hex 32>
```

Restul (Google / Apple / Facebook / SMTP) pot rămâne goale la primul deploy — atunci
merge doar login-ul cu email + parolă, iar emailurile de verificare nu pleacă.

Nu pune niciodată valorile astea în `docker-compose.dokploy.yml`: fișierul e în git.

## Pasul 5 — Deploy

Apasă **Deploy** și urmărește logurile. Primul build durează câteva minute (restore
NuGet + compilare WASM). Ce trebuie să vezi:

- `navigationes-api` — `Applying migration '20260815150045_InitialCreate'` … până la
  `ExplanationImageUrl`, apoi `Now listening on: http://[::]:8080`
- `navigationes-web` — `10-api-base-url.sh: ApiBaseUrl set to https://api.navigationes.eu/`

---

## Pasul 6 — Verificare

```bash
curl -i https://api.navigationes.eu/ping                 # 200, "pong"
curl -i https://navigationes.eu/appsettings.json         # {"ApiBaseUrl": "https://api.navigationes.eu/"}
curl -i https://navigationes.eu/signin                   # 200 + index.html (nu 404 — fallback SPA)
curl -i http://navigationes.eu                           # 301 către https
```

Apoi în browser pe `https://navigationes.eu`: creează un cont cu email + parolă. Dacă
sign-up-ul răspunde, atunci DNS, TLS, CORS, JWT și baza de date sunt toate corecte.

O eroare de CORS în consola browserului înseamnă că `Cors__AllowedOrigins__*` nu se
potrivește exact cu originea din bara de adrese (schemă + host, fără slash final).

---

## Pasul 7 — Login social (după ce site-ul e sus)

Fiecare provider trebuie să accepte noile URL-uri de callback. Domeniul e al API-ului,
nu al site-ului:

| Provider | Unde | Ce adaugi |
|---|---|---|
| Google Cloud Console | OAuth client "Web application" → *Authorized redirect URIs* | `https://api.navigationes.eu/api/auth/google/callback` |
| Apple Developer | Services ID → *Return URLs* (+ *Domains*: `api.navigationes.eu`) | `https://api.navigationes.eu/api/auth/apple/callback` |
| Meta for Developers | Facebook Login → *Valid OAuth Redirect URIs* | `https://api.navigationes.eu/api/auth/facebook/callback` |

Fluxul se închide pe site: API-ul redirecționează la `https://navigationes.eu/auth-callback`
cu token-ul în fragmentul URL-ului (`#`), care nu ajunge niciodată în logurile serverului.

Pentru Facebook în Live Mode mai trebuie și URL-urile legale, care există deja pe API:
`https://api.navigationes.eu/privacy` și `https://api.navigationes.eu/data-deletion`.

---

## Update-uri ulterioare

`git push` pe `main` → **Redeploy** în Dokploy. Dacă activezi webhook-ul din
*Settings → Webhooks* și îl adaugi în GitHub, se face automat la fiecare push.

Migrațiile EF noi se aplică singure la pornirea containerului.

```bash
docker logs -f navigationes-api
docker logs -f navigationes-web
docker exec -it navigationes-api curl -s localhost:8080/ping
```

---

## Ce a rămas nerezolvat

Lucruri pe care deploy-ul le scoate la iveală, dar care nu blochează lansarea:

- **Nu există niciun buton „he olvidado mi contraseña" în site.** Pagina
  `reset-password.html` există și funcționează, dar `/api/forgotPassword` — endpoint-ul
  care trimite emailul cu linkul — nu e apelat de nicăieri în `NavigationES.Web`.
  `Signin.razor` nu are link către el. Practic utilizatorul nu poate declanșa emailul,
  deci pagina de resetare e de neatins fără o cerere făcută manual către API.
- **Emailul de resetare e în engleză** („Password Reset Request", „Hi {name}…") deși
  restul aplicației e în spaniolă — `AuthService.ForgotPasswordAsync`.
- **Imaginile la întrebări sunt în imaginea Docker**, nu într-un volum. Cele 44 MB din
  `NavigationES.Api/wwwroot/images` sunt în git, deci vin cu fiecare build — corect
  pentru conținut versionat. Dar dacă vreodată încarci imagini prin `/api/admin/*`
  direct pe server, se pierd la primul redeploy.
- **Portul 1433 e publicat public în stack-ul Church.** `C:\Sermon\docker-compose.dokploy.yml`
  are `ports: - "1433:1433"` pe `mssql`, deci SQL Server ascultă pe `72.60.92.50:1433`
  de pe internet, protejat doar de parola `sa`. NavigationES nu are nevoie de asta —
  merge prin rețeaua Docker. Merită scoasă linia (și la fel `5000:80` de pe church-api).
