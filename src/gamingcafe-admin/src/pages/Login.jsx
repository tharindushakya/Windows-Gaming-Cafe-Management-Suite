import React, { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useNavigate } from 'react-router-dom';
import { useToast } from '../components/ToastProvider';

export default function Login({ onSuccess }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [remember, setRemember] = useState(true);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const nav = useNavigate();
  const toast = useToast();

  async function submit(e) {
    e.preventDefault();
    setError(null);
    if (!email || !password) {
      setError('Email and password are required');
      return;
    }
    setLoading(true);
    try {
      await login(email, password);
      if (typeof onSuccess === 'function') {
        onSuccess();
      } else {
        nav('/');
      }
    } catch (err) {
      setError(err?.data?.message || err.message || 'Login failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#0f172a' }}>
      <div style={{ width: 420, background: '#fff', borderRadius: 8, padding: 24, boxShadow: '0 6px 24px rgba(2,6,23,0.6)' }}>
        <h2 style={{ marginTop: 0, marginBottom: 8 }}>GamingCafe Admin</h2>
        <p style={{ marginTop: 0, color: '#475569' }}>Sign in to manage stations, users, wallets and reports.</p>
        <form onSubmit={submit}>
          <div style={{ marginTop: 12 }}>
            <label style={{ display: 'block', fontSize: 13 }}>Email</label>
            <input value={email} onChange={e => setEmail(e.target.value)} style={{ width: '100%', padding: '8px 10px', borderRadius: 4, border: '1px solid #cbd5e1' }} />
          </div>
          <div style={{ marginTop: 12 }}>
            <label style={{ display: 'block', fontSize: 13 }}>Password</label>
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} style={{ width: '100%', padding: '8px 10px', borderRadius: 4, border: '1px solid #cbd5e1' }} />
          </div>
          <div style={{ marginTop: 10, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input type="checkbox" checked={remember} onChange={e => setRemember(e.target.checked)} /> Remember me
            </label>
            <button type="button" onClick={() => toast.push('Password reset not implemented', 'info')} style={{ background: 'none', border: 'none', color: '#2563eb', textDecoration: 'underline', cursor: 'pointer' }}>Forgot password?</button>
          </div>

          {error && <div style={{ color: 'red', marginTop: 12 }}>{error}</div>}

          <div style={{ marginTop: 16 }}>
            <button type="submit" disabled={loading} style={{ width: '100%', padding: '10px 12px', background: '#0ea5a9', color: '#fff', border: 'none', borderRadius: 6, cursor: 'pointer' }}>
              {loading ? 'Signing in...' : 'Sign in'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
