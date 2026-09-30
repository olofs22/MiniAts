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
bootstrappas manuellt, se `Data/Sql/bootstrap-first-admin.sql`. Dessa endpoints
saknar ännu automatiska tester (inget testprojekt finns i repot än).

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
CV-uppladdning, aktivitetslogg, GDPR-gallring, statistik, AI-funktioner.