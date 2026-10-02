#!/usr/bin/env node
// End-to-end smoke test against a running API and its real Supabase project.
// Creates temporary mini-ats-test-* accounts and an org, exercises onboarding, the customer
// flow, org isolation, deactivation (real Supabase ban) and cascade org delete, then deletes
// everything it created. Secrets are never printed. Exits non-zero if any check fails.
//
// Config via env vars: API_URL, SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY, SUPABASE_ANON_KEY.
// Unset values fall back to local dev config (user-secrets + web environment.development.ts).
// See DEPLOY.md before pointing this at an environment with real customer data.
import { execSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { randomBytes, randomUUID } from 'node:crypto';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const apiDir = join(dirname(fileURLToPath(import.meta.url)), '..');

function localSecrets() {
  try {
    return Object.fromEntries(
      execSync('dotnet user-secrets list', { cwd: apiDir, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] })
        .split(/\r?\n/).filter((l) => l.includes(' = '))
        .map((l) => { const i = l.indexOf(' = '); return [l.slice(0, i).trim(), l.slice(i + 3).trim()]; }),
    );
  } catch {
    return {};
  }
}

function localAnonKey() {
  const file = join(apiDir, 'web/src/environments/environment.development.ts');
  return existsSync(file) ? readFileSync(file, 'utf8').match(/supabaseAnonKey:\s*'([^']+)'/)?.[1] : undefined;
}

const needsLocal = !process.env.SUPABASE_URL || !process.env.SUPABASE_SERVICE_ROLE_KEY;
const secrets = needsLocal ? localSecrets() : {};
const API = (process.env.API_URL ?? 'http://localhost:5046').replace(/\/$/, '');
const SUPA = (process.env.SUPABASE_URL ?? secrets['Supabase:Url'])?.replace(/\/$/, '');
const SRK = process.env.SUPABASE_SERVICE_ROLE_KEY ?? secrets['Supabase:ServiceRoleKey'];
const ANON = process.env.SUPABASE_ANON_KEY ?? localAnonKey();

const missing = Object.entries({ SUPABASE_URL: SUPA, SUPABASE_SERVICE_ROLE_KEY: SRK, SUPABASE_ANON_KEY: ANON })
  .filter(([, v]) => !v).map(([k]) => k);
if (missing.length > 0) {
  console.error(`Missing config: ${missing.join(', ')} (set env vars or configure local dev secrets).`);
  process.exit(2);
}

console.log(`Smoke testing ${API} against ${SUPA}`);

const tag = randomBytes(3).toString('hex');
const pw = () => randomBytes(18).toString('base64url');
const svc = { apikey: SRK, Authorization: `Bearer ${SRK}`, 'Content-Type': 'application/json' };

let failures = 0;
const check = (name, ok, detail = '') => {
  if (!ok) failures++;
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? `  (${detail})` : ''}`);
};

async function call(url, opts = {}) {
  const res = await fetch(url, opts);
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { /* non-JSON body */ }
  return { status: res.status, json, text };
}

const api = (token, method, path, body) => call(`${API}${path}`, {
  method,
  headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
  body: body === undefined ? undefined : JSON.stringify(body),
});

async function createAuthUser(email, password) {
  const r = await call(`${SUPA}/auth/v1/admin/users`, {
    method: 'POST', headers: svc, body: JSON.stringify({ email, password, email_confirm: true }),
  });
  if (r.status >= 300) throw new Error(`create auth user ${email}: ${r.status} ${r.text}`);
  return r.json.id;
}

const signIn = (email, password) => call(`${SUPA}/auth/v1/token?grant_type=password`, {
  method: 'POST',
  headers: { apikey: ANON, 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password }),
});

const created = { userIds: [], orgId: null };
let adminToken = null;

try {
  // Temporary admin; the profile is inserted with the service role, which bypasses RLS.
  const adminEmail = `mini-ats-test-admin-${tag}@example.com`;
  const adminPw = pw();
  const adminId = await createAuthUser(adminEmail, adminPw);
  created.userIds.push(adminId);
  const ins = await call(`${SUPA}/rest/v1/profiles`, {
    method: 'POST', headers: { ...svc, Prefer: 'return=minimal' },
    body: JSON.stringify({ user_id: adminId, org_id: null, role: 'Admin' }),
  });
  if (ins.status >= 300) throw new Error(`insert admin profile: ${ins.status} ${ins.text}`);
  const adminLogin = await signIn(adminEmail, adminPw);
  adminToken = adminLogin.json?.access_token;
  check('admin can sign in', !!adminToken, `${adminLogin.status}`);

  let r = await api(adminToken, 'GET', '/api/me');
  check('GET /api/me as admin -> Admin', r.status === 200 && r.json?.role === 'Admin', `${r.status} ${r.json?.role}`);

  // Pre-creating the auth user exercises the invite "resume" path, so no invite email is sent.
  r = await api(adminToken, 'POST', '/api/admin/organizations', { name: `mini-ats-test-org-${tag}` });
  created.orgId = r.json?.id;
  check('admin creates organization', r.status === 201 && !!created.orgId, `${r.status}`);

  const c1Email = `mini-ats-test-customer1-${tag}@example.com`;
  const c1Pw = pw();
  const c1Id = await createAuthUser(c1Email, c1Pw);
  created.userIds.push(c1Id);
  r = await api(adminToken, 'POST', '/api/admin/users', { email: c1Email, orgId: created.orgId, role: 'Customer' });
  check('admin onboards customer into org', r.status === 201, `${r.status} ${r.text.slice(0, 120)}`);

  const c1Token = (await signIn(c1Email, c1Pw)).json?.access_token;
  check('customer can sign in', !!c1Token);

  r = await api(c1Token, 'GET', '/api/me');
  check('GET /api/me as customer -> own org', r.status === 200 && r.json?.orgId === created.orgId
    && r.json?.orgName === `mini-ats-test-org-${tag}`, `${r.status}`);

  const job = await api(c1Token, 'POST', '/api/jobs', { title: 'Test engineer', description: null, status: null });
  const cand = await api(c1Token, 'POST', '/api/candidates', { name: 'Test Candidate', email: null, phone: null, linkedInUrl: null, notes: null });
  check('customer creates job + candidate', job.status === 201 && cand.status === 201, `${job.status}/${cand.status}`);
  const appl = await api(c1Token, 'POST', '/api/applications', { candidateId: cand.json?.id, jobId: job.json?.id });
  check('customer adds candidate to job (position 0)', appl.status === 201 && appl.json?.position === 0, `${appl.status}`);
  r = await api(c1Token, 'PUT', `/api/applications/${appl.json?.id}`, { stage: 'Interview', position: -1 });
  check('drag to top of Interview (position -1) saves', r.status === 204, `${r.status}`);
  r = await api(c1Token, 'GET', `/api/applications?jobId=${job.json?.id}`);
  check('board reload shows Interview / -1', r.status === 200 && r.json?.[0]?.stage === 'Interview' && r.json?.[0]?.position === -1);

  r = await api(c1Token, 'GET', '/api/admin/users');
  check('customer is denied admin endpoints (403)', r.status === 403, `${r.status}`);
  r = await api(c1Token, 'GET', `/api/jobs?orgId=${randomUUID()}`);
  check('customer passing another orgId still only sees own jobs', r.status === 200
    && r.json.length > 0 && r.json.every((j) => j.orgId === created.orgId), `${r.status}`);
  r = await api('not-a-real-token', 'GET', '/api/jobs');
  check('garbage token -> 401 (what the frontend redirects on)', r.status === 401, `${r.status}`);

  r = await api(adminToken, 'GET', `/api/admin/users?orgId=${created.orgId}`);
  check('admin lists org users with real email', r.status === 200
    && r.json?.some((u) => u.userId === c1Id && u.email === c1Email), `${r.status} ${r.text.slice(0, 120)}`);

  r = await api(adminToken, 'DELETE', `/api/admin/users/${c1Id}`);
  check('admin deactivates customer (real Supabase ban)', r.status === 204, `${r.status} ${r.text.slice(0, 160)}`);
  const relogin = await signIn(c1Email, c1Pw);
  check('deactivated customer can no longer sign in', !relogin.json?.access_token,
    `${relogin.status} ${relogin.json?.error_code ?? relogin.json?.msg ?? ''}`);
  r = await api(c1Token, 'GET', '/api/jobs');
  check('deactivated customer\'s still-valid token gets clean 403', r.status === 403
    && r.json?.title === 'Not authorized for this organization.', `${r.status} ${r.text.slice(0, 120)}`);

  const c2Email = `mini-ats-test-customer2-${tag}@example.com`;
  const c2Pw = pw();
  const c2Id = await createAuthUser(c2Email, c2Pw);
  created.userIds.push(c2Id);
  r = await api(adminToken, 'POST', '/api/admin/users', { email: c2Email, orgId: created.orgId, role: 'Customer' });
  check('second customer onboarded', r.status === 201, `${r.status}`);

  r = await api(adminToken, 'DELETE', `/api/admin/organizations/${created.orgId}`);
  check('admin deletes org (with job/candidate/application/user)', r.status === 204, `${r.status} ${r.text.slice(0, 160)}`);
  if (r.status === 204) {
    const deletedOrgId = created.orgId;
    created.orgId = null;
    r = await api(adminToken, 'GET', `/api/admin/organizations/${deletedOrgId}`);
    check('deleted org is gone (404)', r.status === 404, `${r.status}`);
    r = await api(adminToken, 'GET', `/api/jobs?orgId=${deletedOrgId}`);
    check('deleted org\'s jobs are gone', r.status === 200 && r.json.length === 0, `${r.status}`);
    r = await api(adminToken, 'GET', `/api/admin/users?orgId=${deletedOrgId}`);
    check('deleted org\'s profiles are gone', r.status === 200 && r.json.length === 0, `${r.status}`);
    const c2Login = await signIn(c2Email, c2Pw);
    check('deleted org\'s user can no longer sign in', !c2Login.json?.access_token, `${c2Login.status}`);
  }
} catch (err) {
  failures++;
  console.log(`FAIL  aborted: ${err.message}`);
} finally {
  console.log('--- cleanup');
  if (created.orgId && adminToken) {
    const r = await api(adminToken, 'DELETE', `/api/admin/organizations/${created.orgId}`);
    console.log(`org delete: ${r.status}`);
  }
  for (const id of created.userIds) {
    const p = await call(`${SUPA}/rest/v1/profiles?user_id=eq.${id}`, { method: 'DELETE', headers: svc });
    const u = await call(`${SUPA}/auth/v1/admin/users/${id}`, { method: 'DELETE', headers: svc });
    console.log(`user ${id}: profile ${p.status}, auth user ${u.status}`);
  }
  if (created.userIds.length > 0) {
    const left = await call(`${SUPA}/rest/v1/profiles?select=user_id&user_id=in.(${created.userIds.join(',')})`, { headers: svc });
    console.log(`leftover test profiles: ${left.json ? left.json.length : left.status}`);
  }
  console.log(failures === 0 ? 'ALL CHECKS PASSED' : `${failures} CHECK(S) FAILED`);
  process.exitCode = failures === 0 ? 0 : 1;
}
