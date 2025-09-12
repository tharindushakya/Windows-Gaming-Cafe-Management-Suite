// Minimal API client for the GamingCafe API with auth support

function getToken() {
  try { const u = JSON.parse(localStorage.getItem('gc_user')); return u?.token || null; } catch { return null; }
}

function setToken(token) {
  try {
    const u = JSON.parse(localStorage.getItem('gc_user')) || {};
    u.token = token;
    localStorage.setItem('gc_user', JSON.stringify(u));
  } catch { }
}

async function request(path, { method = 'GET', body = null, useCredentials = false } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  const token = getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(path, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
    credentials: useCredentials ? 'include' : 'same-origin'
  });

  const text = await res.text();
  let data = null;
  try { data = text && JSON.parse(text); } catch { data = text; }
  if (!res.ok) {
    // Normalize validation errors from ASP.NET ModelState or custom shapes into a consistent `errors` map
    const err = new Error(data?.message || res.statusText || 'Request failed');
    err.status = res.status;
    err.data = data;
    // If data contains modelState style errors, normalize to { field: [messages] }
    if (data) {
      if (data.errors && typeof data.errors === 'object') {
        err.errors = data.errors; // already shaped
      } else if (data.modelState && typeof data.modelState === 'object') {
        err.errors = data.modelState;
      } else if (data?.errorsDescription && Array.isArray(data.errorsDescription)) {
        // Some endpoints return an array of error strings
        err.errors = { _global: data.errorsDescription };
      } else if (data?.errors && Array.isArray(data.errors)) {
        err.errors = { _global: data.errors };
      } else {
        // fallback: check for validation problem details
        if (data?.title && data?.errors) err.errors = data.errors;
      }
    }
    throw err;
  }
  return data;
}

async function post(path, body) {
  return request(path, { method: 'POST', body });
}

async function get(path) {
  return request(path, { method: 'GET' });
}

async function del(path) {
  return request(path, { method: 'DELETE' });
}

async function put(path, body) {
  return request(path, { method: 'PUT', body });
}

const api = { post, get, put, del, getToken, setToken };
export default api;
