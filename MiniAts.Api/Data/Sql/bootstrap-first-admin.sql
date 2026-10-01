-- Engångskörning: skapa admin-profilen åt dig själv, för att kunna logga in
-- och sedan använda POST /api/admin/organizations och POST /api/admin/users
-- för all vidare onboarding.
--
-- Förutsättning: du har redan skapat din egen användare i Supabase Auth
-- (dashboard: Authentication -> Add user, eller loggat in en gång via appen).
-- Kopiera användarens UUID (auth.users.id) från dashboarden innan du kör detta.
--
-- Kör i Supabase SQL editor, för hand, en gång.
--
-- org_id är nullable för Admin-profiler (en admin är inte knuten till en
-- specifik organisation - den väljer org per request via ?orgId=).

insert into public.profiles (user_id, org_id, role)
values (
  '<din-auth-user-uuid>',   -- auth.users.id för dig själv
  null,
  'Admin'
);
