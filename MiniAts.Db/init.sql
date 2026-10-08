CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE TABLE organizations (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        name character varying(200) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_organizations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE TABLE candidates (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        org_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        email character varying(320),
        phone character varying(50),
        linked_in_url character varying(500),
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_candidates PRIMARY KEY (id),
        CONSTRAINT fk_candidates_organizations_org_id FOREIGN KEY (org_id) REFERENCES organizations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE TABLE jobs (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        org_id uuid NOT NULL,
        title character varying(200) NOT NULL,
        description text,
        status character varying(20) NOT NULL DEFAULT 'Open',
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_jobs PRIMARY KEY (id),
        CONSTRAINT fk_jobs_organizations_org_id FOREIGN KEY (org_id) REFERENCES organizations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE TABLE profiles (
        user_id uuid NOT NULL,
        org_id uuid NOT NULL,
        role character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_profiles PRIMARY KEY (user_id),
        CONSTRAINT fk_profiles_organizations_org_id FOREIGN KEY (org_id) REFERENCES organizations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE TABLE applications (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        candidate_id uuid NOT NULL,
        job_id uuid NOT NULL,
        org_id uuid NOT NULL,
        stage character varying(20) NOT NULL DEFAULT 'New',
        position double precision NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_applications PRIMARY KEY (id),
        CONSTRAINT fk_applications_candidates_candidate_id FOREIGN KEY (candidate_id) REFERENCES candidates (id) ON DELETE CASCADE,
        CONSTRAINT fk_applications_jobs_job_id FOREIGN KEY (job_id) REFERENCES jobs (id) ON DELETE CASCADE,
        CONSTRAINT fk_applications_organizations_org_id FOREIGN KEY (org_id) REFERENCES organizations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE INDEX ix_applications_candidate_id ON applications (candidate_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE INDEX ix_applications_job_id_stage_position ON applications (job_id, stage, position);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE INDEX ix_applications_org_id ON applications (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE INDEX ix_candidates_org_id ON candidates (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE INDEX ix_jobs_org_id ON jobs (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    CREATE INDEX ix_profiles_org_id ON profiles (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260928142205_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260928142205_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261001012506_MakeProfileOrgIdNullable') THEN
    ALTER TABLE profiles ALTER COLUMN org_id DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261001012506_MakeProfileOrgIdNullable') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261001012506_MakeProfileOrgIdNullable', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261002125636_AddSignupRequests') THEN
    CREATE TABLE signup_requests (
        id uuid NOT NULL DEFAULT (gen_random_uuid()),
        company_name character varying(200) NOT NULL,
        contact_name character varying(200) NOT NULL,
        email character varying(320) NOT NULL,
        message character varying(2000),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT pk_signup_requests PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261002125636_AddSignupRequests') THEN
    ALTER TABLE public.signup_requests ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261002125636_AddSignupRequests') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261002125636_AddSignupRequests', '10.0.12');
    END IF;
END $EF$;
COMMIT;

