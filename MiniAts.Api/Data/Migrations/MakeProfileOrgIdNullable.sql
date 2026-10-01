START TRANSACTION;
ALTER TABLE profiles ALTER COLUMN org_id DROP NOT NULL;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261001012506_MakeProfileOrgIdNullable', '10.0.12');

COMMIT;

