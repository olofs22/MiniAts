CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE organizations (
    id uuid NOT NULL DEFAULT (gen_random_uuid()),
    name character varying(200) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT pk_organizations PRIMARY KEY (id)
);

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

CREATE TABLE profiles (
    user_id uuid NOT NULL,
    org_id uuid NOT NULL,
    role character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT pk_profiles PRIMARY KEY (user_id),
    CONSTRAINT fk_profiles_organizations_org_id FOREIGN KEY (org_id) REFERENCES organizations (id) ON DELETE RESTRICT
);

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

CREATE INDEX ix_applications_candidate_id ON applications (candidate_id);

CREATE INDEX ix_applications_job_id_stage_position ON applications (job_id, stage, position);

CREATE INDEX ix_applications_org_id ON applications (org_id);

CREATE INDEX ix_candidates_org_id ON candidates (org_id);

CREATE INDEX ix_jobs_org_id ON jobs (org_id);

CREATE INDEX ix_profiles_org_id ON profiles (org_id);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260928142205_InitialCreate', '10.0.12');

COMMIT;

