import { test, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { login, logout, request, clearSession } from '../src/api.ts';

globalThis.__DEV__ = true;
process.env.EXPO_PUBLIC_API_URL = 'http://localhost:5080';
const originalFetch = globalThis.fetch;
afterEach(() => { clearSession(); globalThis.fetch = originalFetch; });
const json = (body, status = 200) => new Response(JSON.stringify(body), { status });

test('conta pendente usa onboarding somente após 401', async () => {
  const calls = [];
  globalThis.fetch = async url => {
    calls.push(url);
    if (url.endsWith('/auth/login')) return json({}, 401);
    if (url.endsWith('/auth/onboarding/login')) return json({ accessToken: 'fixture', refreshToken: null });
    return json({ role: 'Courier', status: 'Pending' });
  };
  assert.equal((await login('fixture@example.test', 'fixture-password')).status, 'Pending');
  assert.equal(calls.length, 3);
});

test('falha de serviço não tenta onboarding', async () => {
  let calls = 0;
  globalThis.fetch = async () => { calls++; return json({}, 503); };
  await assert.rejects(login('fixture@example.test', 'fixture-password'), { status: 503 });
  assert.equal(calls, 1);
});

test('logout atrasado não apaga novo login', async () => {
  let release;
  let token = 'first';
  let lastHeader;
  globalThis.fetch = async (url, init) => {
    if (url.endsWith('/auth/logout')) return new Promise(resolve => { release = resolve; });
    if (url.endsWith('/auth/login')) return json({ accessToken: token, refreshToken: null });
    lastHeader = init.headers.Authorization;
    return json({ role: 'Courier' });
  };
  await login('fixture@example.test', 'fixture-password');
  const pending = logout().catch(() => {});
  token = 'second';
  await login('fixture@example.test', 'fixture-password');
  release(new Response(null, { status: 204 }));
  await pending;
  await request('/auth/me');
  assert.equal(lastHeader, 'Bearer second');
});
