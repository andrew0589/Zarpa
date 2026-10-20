# Pregunta del día — Facebook

Generează imagini cu întrebări din teme și le programează pe pagina NavigationES prin Graph API.

| Fișier | Rol |
|---|---|
| `config.json` | orele postărilor, rotația PNB/PER/PY/CY, sursele JSON, link-ul cu UTM |
| `generar_postari.py` | alege întrebările, randează PNG 1080×1350 în `out/<zi>/`, adaugă în `coada.json` |
| `programa_facebook.py` | trimite `coada.json` pe Facebook ca postări programate, le mută în `publicate.json` |
| `publicate.json` | istoricul — întrebările de aici nu se mai repetă (de păstrat în git) |

## Rulare lunară

```
python generar_postari.py --zile 28
python programa_facebook.py programeaza
python programa_facebook.py lista
```

Imaginile se pot verifica în `out/` înainte de programare; o postare nedorită se scoate din `coada.json`
(și se rulează din nou generatorul, care umple doar orele libere). Facebook acceptă programări între
10 minute și ~30 de zile în avans; ce e mai departe rămâne în coadă pentru rularea următoare.
Orele sunt ora locală a PC-ului (Romance Standard Time = Madrid).

## Token (o singură dată)

1. developers.facebook.com → **My Apps → Create App**, cazul de utilizare „Manage everything on your Page”.
2. **Tools → Graph API Explorer**: alege aplicația, „Get User Access Token”, permisiunile
   `pages_show_list`, `pages_read_engagement`, `pages_manage_posts`, **Generate Access Token**.
3. **Tools → Access Token Debugger**: lipește token-ul → **Extend Access Token** (token de user pe 60 de zile).
4. Înapoi în Explorer, cu token-ul extins, cererea `GET me/accounts` → la pagina NavigationES
   copiază `id` și `access_token` (token de pagină care nu expiră).
5. Creează `%USERPROFILE%\.navigationes\facebook.json`:
   ```json
   { "page_id": "ID-UL PAGINII", "token": "TOKEN-UL DE PAGINĂ" }
   ```
   Fișierul stă în afara repo-ului; token-ul nu se pune niciodată în `config.json`.
6. Aplicația trebuie trecută pe **Live** (App settings → Basic: Privacy Policy URL
   `https://navigationes.eu/privacidad.html`, categoria, iconița), altfel postările făcute
   de o aplicație în modul Development le văd doar administratorii ei.
7. `python programa_facebook.py verifica` → trebuie să afișeze numele paginii.
