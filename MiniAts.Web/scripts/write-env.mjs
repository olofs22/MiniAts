#!/usr/bin/env node
// Writes src/environments/environment.ts from env vars before a production build, so the
// shipped bundle never contains the committed 'REPLACE-WITH-...' placeholders. Run via
// `npm run build:prod` (see DEPLOY.md). Do not commit the file after running this locally.
import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const required = ['API_URL', 'SUPABASE_URL', 'SUPABASE_ANON_KEY'];
const missing = required.filter((name) => !process.env[name]);

if (missing.length > 0) {
  console.error(`Missing required env var(s) for production build: ${missing.join(', ')}`);
  console.error('Set them before running "npm run build:prod" (see DEPLOY.md).');
  process.exit(1);
}

const outPath = join(
  dirname(fileURLToPath(import.meta.url)),
  '../src/environments/environment.ts',
);

const contents = `export const environment = {
  production: true,
  apiUrl: '${process.env.API_URL}',
  supabaseUrl: '${process.env.SUPABASE_URL}',
  supabaseAnonKey: '${process.env.SUPABASE_ANON_KEY}',
};
`;

writeFileSync(outPath, contents);
console.log(`Wrote ${outPath} from environment variables.`);
