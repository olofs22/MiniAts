-- Slår på Row Level Security på alla MiniAts-tabeller.
-- Låser PostgREST från att läsa/skriva tabellerna tills policies läggs till
-- senare (separat steg). API:ts egen Postgres-anslutning (roll med BYPASSRLS)
-- påverkas inte.
--
-- Kör EFTER att `dotnet ef database update` har skapat tabellerna.

ALTER TABLE public.organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.profiles      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.jobs          ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.candidates    ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.applications  ENABLE ROW LEVEL SECURITY;
