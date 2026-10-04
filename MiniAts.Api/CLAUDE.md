# Mini-ATS

Ett applicant tracking system för rekryteringsbyråer/företag. Ska kunna användas live av en första kund så snabbt som möjligt. MVP först, extra features sist.

## Stack
- Backend: ASP.NET Core Web API (.NET 10, controllers), JWT, EF Core + Npgsql, projekt i /MiniAts.Api
- Frontend: Angular (standalone components, signals), Angular Material + CDK DragDrop + Tailwind, i /web
- Supabase: Postgres + Auth. Angular loggar in via supabase-js. API:t validerar Supabase-JWT (JwtBearer, JWKS).
- Service role key används bara i API:t (för att skapa användare), aldrig i frontend.

## Roller och multi-tenancy
- Roller: admin, customer. Roll och org_id lagras i `profiles`.
- Kund ser och ändrar bara data för sin egen organisation (org_id kommer alltid från profilen, aldrig från klienten).
- Admin kan agera åt valfri org genom att skicka orgId. Denna logik ligger centralt (policy/service), inte i varje controller.

## Datamodell
organizations, profiles (user_id, org_id, role), jobs (org_id, title, description, status),
candidates (org_id, name, email, phone, linkedin_url, notes),
applications (candidate_id, job_id, stage, position).
Stages: New, Screening, Interview, Offer, Hired, Rejected.
Kanban visar applications. En kandidat kan ligga på flera jobb.

## Onboarding
Ny org + första användaren för en kund skapas via `POST /api/admin/organizations` och
`POST /api/admin/users` (kräver Admin-roll). Den allra första admin-användaren
bootstrappas manuellt, se `Data/Sql/bootstrap-first-admin.sql`. Testprojekt finns i
`/MiniAts.Api.Tests` (xUnit, syskon-mapp till `/MiniAts.Api`). Alla controllers har
tester där, med fokus på org-isolering; använd `TestDb` och `TestUsers` för nya tester.
Frontend-tester (Vitest) körs med `npx ng test --watch=false` i /web.

Se `DEPLOY.md` för vad som krävs för en riktig deploy (config, migrations, RLS-ordning,
frontend-build).

## Frontend (implementerat)
I /web finns inloggning (Supabase) och dashboard, samt CRUD + Kanban för hela MVP-flödet:
- Jobs (`features/jobs`): lista, skapa, redigera, ta bort, länk till Kanban-board per jobb.
- Candidates (`features/candidates`): lista med sök, skapa, redigera, ta bort, detaljvy som visar
  kandidatens ansökningar och kan lägga till kandidaten på ett jobb.
- Kanban-board (`features/kanban`, route `/jobs/:jobId/board`): en board per jobb, en kolumn per
  stage, drag-and-drop med Angular CDK. API:t saknar en reorder-endpoint, så klienten räknar ut ett
  nytt fraktionellt `position`-värde själv vid varje drag och skriver `stage`+`position` tillsammans
  via PUT, med optimistisk uppdatering och rollback vid fel.
- Delade typer/modeller ligger i `shared/models/`.
- CV-analys med AI (`features/candidates/candidate-from-cv`, `POST /api/candidates/analyze-cv`,
  `CvAnalysis/ClaudeCvAnalyzer.cs`): ladda upp en PDF, Claude fyller i kontaktuppgifter och
  rankar orgens öppna jobb. CV:t sparas aldrig. Kräver `Anthropic:ApiKey`.
- Admin-onboarding (`features/admin`): lista/skapa/ta bort organisationer (borttagning
  kaskaderar explicit i endpointen och spärrar orgens användare i Supabase), lista och
  inaktivera användare per org (`/admin/organizations/:id/users`), bjuda in användare
  (inbjudan till en org blir alltid Customer; "Invite admin" i navbaren, `/admin/admins/new`,
  skapar admins utan org – API:t avvisar Admin med orgId),
  samt en invite-accept-flow (`features/auth/accept-invite`) där en inbjuden användare
  sätter sitt lösenord via en Supabase-länk.
- Glömt lösenord: länk på login → `/forgot-password` (Supabase `resetPasswordForEmail`) →
  mejllänk till `/reset-password`, som återanvänder `AcceptInvite` med `data: { mode: 'reset' }`.
- Header visar inloggad org/e-post (hämtas via `/api/me`, cachead i `MeService`).
- Publik startsida (`features/welcome`, `/welcome`): utloggade hamnar här. Formulär för att
  begära en org (`POST /api/signup-requests`, anonymt, max 5/h per IP + honeypot) och länk till
  inloggning. Admin ser förfrågningarna under `/admin/requests`.
- En global HTTP-interceptor loggar ut och skickar till `/login` vid 401 (sessionen är
  ogiltig/utgången), så appen inte bara visar ett generiskt felmeddelande.

## Konventioner
- Controllers returnerar DTO:er, aldrig EF-entiteter.
- Alla queries filtreras på org_id.
- Enable RLS på alla tabeller utan policies, så att PostgREST inte kan nås direkt.
- Hemligheter i user-secrets/.env, aldrig i git.
- Angular: en feature-mapp per område, API-anrop via en genererad/typad service.
- UI-text skrivs på engelska.
- Skriv ett litet test per ny endpoint där det är rimligt.
- Committa efter varje fungerande steg.

## Utanför MVP (bara om jag ber om det)
Lagring av CV-filer, aktivitetslogg, GDPR-gallring, statistik, fler AI-funktioner.