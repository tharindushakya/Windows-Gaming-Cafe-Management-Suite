import http from 'k6/http';
import { check, group } from 'k6';
import { Rate } from 'k6/metrics';

export let options = {
  vus: 20,
  duration: '30s',
  thresholds: {
    'http_req_failed': ['rate<0.01'], // <1% failures
    'http_req_duration{p95:login}': ['p(95)<300'],
    'http_req_duration{p95:start_session}': ['p(95)<500']
  }
};

const BASE = __ENV.BASE_URL || 'http://localhost:5000';
const loginPayload = JSON.stringify({ email: 'test@example.com', password: 'P@ssw0rd' });
const loginParams = { headers: { 'Content-Type': 'application/json' }, tags: { p95: 'login' } };
const sessionParams = { headers: { 'Content-Type': 'application/json' }, tags: { p95: 'start_session' } };

export default function () {
  group('login', function () {
    let res = http.post(`${BASE}/auth/login`, loginPayload, loginParams);
    check(res, { 'login status 200': (r) => r.status === 200 });
  });

  group('start session', function () {
    // for demo we hit a placeholder session-start endpoint; replace with real flow
    let res = http.post(`${BASE}/sessions/start`, JSON.stringify({ stationId: 1 }), sessionParams);
    check(res, { 'start status 200': (r) => r.status === 200 });
  });
}
