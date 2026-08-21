# NavigationES

App de test pentru titulaciones náuticas (España): PNB, PER, Patrón de Yate, Capitán de Yate.

Stack: .NET 10 — MAUI client (Android/iOS/Windows) + ASP.NET Core minimal API + EF Core (SQL Server).
Structura și partea de autentificare sunt portate din Church Runner (email/parolă + Google/Apple/Facebook,
flow OAuth server-driven prin WebAuthenticator, tabela UserLogins, JWT).

## Identitate aplicație (schimbă-le DOAR înainte de primul upload în store)

| Ce | Valoare |
|---|---|
| ApplicationId / bundle ID | `com.navigationes.app` |
| Schema deep link | `navigationes://` |
| Port API local (dev) | `7136` (diferit de Church Runner ca să poată rula amândouă) |
| URL API producție | `https://api.navigationes.example` — TODO: domeniu real |

## Configurare necesară înainte să meargă (toate placeholder-e acum)

### NavigationES.Api — appsettings / user-secrets / env vars pe server
- `ConnectionStrings:NavigationESDb` — SQL Server
- `Jwt:*` — cheile JWT (aceleași nume ca în Church Runner)
- `Authentication:Google:{ClientId,ClientSecret}` — client OAuth **nou** tip "Web application"
  (redirect: `https://<api-prod>/api/auth/google/callback` + `http://localhost:7136/api/auth/google/callback`)
- `Authentication:Apple:{ServicesId,TeamId,KeyId,PrivateKey}` — App ID + Services ID **noi** pentru
  bundle `com.navigationes.app`, cheie .p8 nouă; return URL pe domeniul API-ului de producție
  (Apple nu acceptă localhost — testezi social pe API-ul deployat)
- `Authentication:Facebook:{AppId,AppSecret}` — app Facebook **nou** (Consumer + Facebook Login);
  pentru Live Mode: privacy policy URL + data deletion URL (există la `/privacy` și `/data-deletion`)
  + App Review pentru `email` și `public_profile` (aceiași pași ca la Church Runner, aug. 2026)
- Email (verificarea cu cod de 6 cifre + reset parolă) — cheile `EmailSettings:*` în appsettings
- `AppSettings:FrontendUrl` — pagina web `reset-password.html` către care trimite emailul de resetare
  (trebuie găzduită separat, ca la Church Runner)

### Bază de date
- Migrațiile EF nu sunt create încă: `dotnet ef migrations add Initial --project NavigationES.Api` apoi `database update`

### Client — de ajustat în cod
- `Services/Environment/ProductionEnvironmentService.cs` — URL-ul API-ului de producție
- `Services/Environment/DevelopmentEnvironmentService.cs` — IP-ul LAN pentru iOS pe device fizic
- Iconiță/splash — sunt cele default de template

## Build & rulare
- Soluția e `NavigationES.slnx` (formatul nou de soluție din .NET 10) — se deschide normal în Visual Studio
- Rulează comenzile `dotnet` din `C:\NavigationES` (global.json de aici alege SDK-ul 10;
  din alt folder poate prinde alt SDK)
- Token-ul de sesiune al clientului e stocat în `Preferences` sub cheia `AuthKey` (ca în sursă)

## Dev loop (identic cu Church Runner)
- Debug → API local pe `0.0.0.0:7136` (profilul "https"); Release → producție
- Android fizic: `adb reverse tcp:7136 tcp:7136` — se face automat la fiecare build de Debug (target în csproj)
- Android emulator: `10.0.2.2:7136` automat; iOS fizic: IP-ul LAN al PC-ului, același Wi-Fi
- Login social nu merge contra API-ului local pe iOS (providerii refuză IP LAN ca redirect) — doar email/parolă local

## Note
- Pe Windows butoanele de social login sunt ascunse (WebAuthenticator nu e implementat în MAUI Windows)
- Validarea SSL în client e făcută corect (bypass doar pe localhost în DEBUG) — NU copia `return true` din Church Runner
- MAUI 10 gestionează edge-to-edge nativ — nu e nevoie de scrim pentru status bar
