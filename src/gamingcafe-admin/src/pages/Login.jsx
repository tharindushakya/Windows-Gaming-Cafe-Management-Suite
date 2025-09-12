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
      toast.push('Email and password are required', 'error');
      return;
    }
    setLoading(true);
    try {
      await login(email, password);
      toast.push('Signed in successfully', 'success');
      if (typeof onSuccess === 'function') {
        onSuccess();
      } else {
        nav('/');
      }
    } catch (err) {
      const msg = err?.data?.message || err.message || 'Login failed';
      setError(msg);
      toast.push(msg, 'error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900">
      <div className="w-[420px] bg-white rounded-lg p-6 shadow-2xl">
        <h2 className="mt-0 mb-2 text-2xl font-semibold">GamingCafe Admin</h2>
        <p className="mt-0 text-gray-600">Sign in to manage stations, users, wallets and reports.</p>
        <form onSubmit={submit}>
          <div className="mt-3">
            <label className="block text-sm">Email</label>
            <input value={email} onChange={e => setEmail(e.target.value)} className="w-full px-3 py-2 rounded border border-gray-300" />
          </div>
          <div className="mt-3">
            <label className="block text-sm">Password</label>
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} className="w-full px-3 py-2 rounded border border-gray-300" />
          </div>
          <div className="mt-3 flex items-center justify-between">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={remember} onChange={e => setRemember(e.target.checked)} className="mr-1" /> Remember me
            </label>
            <button type="button" onClick={() => toast.push('Password reset not implemented', 'info')} className="text-sm text-sky-600 underline">Forgot password?</button>
          </div>

          {error && <div className="text-red-500 mt-3">{error}</div>}

          <div className="mt-4">
            <button type="submit" disabled={loading} className="w-full px-3 py-2 bg-teal-500 text-white rounded hover:bg-teal-600 disabled:opacity-50">
              {loading ? 'Signing in...' : 'Sign in'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
