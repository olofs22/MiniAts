# Deploy checklist

Everything a non-local deploy needs that isn't already obvious from the code. Written for
getting the first real customer live, not as a general ops manual.

## Backend config

None of this lives in git (per CLAUDE.md: secrets in user-secrets/.env, never in git).
Set these as environment variables (or your host's secrets manager) in the real environment:

| Key | Example | Notes |
|---|---|---|
| `ConnectionStrings__Supabase` | Npgsql connection string to the Supabase Postgres instance | required |
| `Supabase__Url` | `https://<project-ref>.supabase.co` | required |
| `Supabase__Authority` | `https://<project-ref>.supabase.co/auth/v1` | required; used for JWT validation (JWKS discovery) |
| `Supabase__Audience` | `authenticated` | required |
| `Supabase__ServiceRoleKey` | the Supabase service role key | required; app throws at startup if missing. Never expose to the frontend. |
| `App__FrontendUrl` | `https://app.yourdomain.com` | required; used to build the invite-accept redirect link sent to new users |
| `Cors__AllowedOrigins__0` | `https://app.yourdomain.com` | optional — if unset, falls back to `App:FrontendUrl` (see `Program.cs`). Only set this separately if the frontend is served from a different origin than `App:FrontendUrl`. |

Double-quotes/colons: ASP.NET Core reads nested config keys from env vars using `__`
(double underscore) in place of `:`.

## Database

Schema is EF Core migrations, not raw SQL — there is no auto-migrate call in `Program.cs`
(deliberately, to avoid an untested migration running against a live customer's data on
every boot). Before first run against a new environment:

1. Apply migrations: `dotnet ef database update --project MiniAts.Api`
2. Run `Data/Sql/enable-rls.sql` once in the Supabase SQL editor (locks PostgREST out of all
   5 tables; the API's own Postgres role has `BYPASSRLS` so it's unaffected — see note below).
3. Create the first Supabase Auth user (dashboard → Authentication → Add user, or sign in
   once via the app), then run `Data/Sql/bootstrap-first-admin.sql` with that user's UUID.
   From there, all further org/user onboarding goes through `POST /api/admin/organizations`
   and `POST /api/admin/users`.

**RLS note:** `enable-rls.sql` turns RLS on but adds no per-org policies — this is
intentional for the current architecture, not unfinished. As long as only the API's
`BYPASSRLS` Postgres role ever talks to the database directly (i.e. PostgREST is never
exposed publicly), this is a sufficient boundary: RLS-without-policies just means "nothing
but the API can read or write anything," which is already true. If a direct PostgREST
integration is ever added, real per-org policies become a prerequisite for that, not before.

## Frontend build

`web/src/environments/environment.ts` (the production environment file) is checked into
git with placeholder values (`REPLACE-WITH-API-URL` etc.) — `ng build`'s default
`production` configuration has no file replacement for it, so a plain `npm run build` ships
those placeholders verbatim.

Use `npm run build:prod` instead, with these env vars set:

| Env var | Example |
|---|---|
| `API_URL` | `https://api.yourdomain.com` |
| `SUPABASE_URL` | `https://<project-ref>.supabase.co` |
| `SUPABASE_ANON_KEY` | the Supabase anon/publishable key (not the service role key) |

This runs `web/scripts/write-env.mjs` first, which overwrites `environment.ts` from those
env vars, then runs `ng build`. Run this as part of your CI/deploy step — don't commit the
file afterward (it's a build artifact at that point, not a tracked value).

## HTTPS / CORS

- `Program.cs` only adds `UseHttpsRedirection` outside Development — make sure whatever
  reverse proxy / hosting platform terminates TLS in front of the API.
- CORS is locked to the origin(s) configured above; a request from any other origin will
  be rejected by the browser.

## Sanity check after deploy

**Automated:** `node scripts/smoke-test.mjs` (from `MiniAts.Api/`) runs the whole API flow
end-to-end — onboarding, the customer job/candidate/Kanban flow, org isolation, user
deactivation and cascade org delete — then deletes everything it created. Point it at a
deployed environment with `API_URL`, `SUPABASE_URL`, `SUPABASE_SERVICE_ROLE_KEY` and
`SUPABASE_ANON_KEY`; with none set, it uses your local dev config against
`http://localhost:5046`. It exits non-zero on any failure.

It temporarily creates `mini-ats-test-*` users (including a test admin) in that Supabase
project and needs the service role key, so run it from a trusted machine or CI secret
store only. It doesn't touch existing data, but avoid running it in the middle of a live
customer's working day.

**Manual (UI):**

1. Sign in as the bootstrapped admin, confirm `/api/me` returns the Admin role.
2. Create a test organization + invite a test user (`/admin`), confirm the invite email
   link lands on the real frontend URL (not `localhost`).
3. As that invited user, create a job, add a candidate, and drag it across the Kanban
   board — confirms JWT validation, org scoping, and the stage/position write path all work
   end-to-end against the real database.
