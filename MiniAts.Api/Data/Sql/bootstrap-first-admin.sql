-- Engångskörning: skapa den första organisationen och admin-profilen åt dig
-- själv, för att kunna logga in och sedan använda POST /api/admin/organizations
-- och POST /api/admin/users för all vidare onboarding.
--
-- Förutsättning: du har redan skapat din egen användare i Supabase Auth
-- (dashboard: Authentication -> Add user, eller loggat in en gång via appen).
-- Kopiera användarens UUID (auth.users.id) från dashboarden innan du kör detta.
--
-- Kör i Supabase SQL editor, för hand, en gång.

insert into public.organizations (name)
values ('<Organisationens namn>')
returning id;  -- spara id:t, används nedan

insert into public.profiles (user_id, org_id, role)
values (
  '<din-auth-user-uuid>',   -- auth.users.id för dig själv
  '<org-id-fran-ovan>',
  'Admin'
);
