import React, { useEffect, useState } from 'react';
import { Wallet, RefreshCcw } from 'lucide-react';
import { motion } from 'framer-motion';
import api from '../api';
import Login from './Login';
import SimpleModal from '../components/SimpleModal';
import { useToast } from '../components/ToastProvider';

export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({});
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState({});
  const [showLoginModal, setShowLoginModal] = useState(false);
  const toast = useToast();

  const loadProfile = React.useCallback(async () => {
    try {
      setLoading(true);
      const data = await api.get('/api/v1.0/auth/profile');
      setProfile(data || {});
      setForm({
        firstName: data?.firstName || '',
        lastName: data?.lastName || '',
        email: data?.email || '',
        username: data?.username || '',
        phoneNumber: data?.phoneNumber || '',
      });
    } catch (err) {
      console.error('Failed to load profile', err);
      if (err && err.status === 401) {
        toast.push('Not authenticated — please login', 'error');
        setShowLoginModal(true);
        return;
      }
      toast.push('Failed to load profile', 'error');
      setProfile(null);
    } finally {
      setLoading(false);
    }
  }, [toast]);

  useEffect(() => { loadProfile(); }, [loadProfile]);

  function handleChange(e) {
    const { name, value } = e.target;
    setForm(p => ({ ...p, [name]: value }));
  }

  async function handleSave(e) {
    e?.preventDefault();
    if (!profile?.userId) return toast.push('Profile is not loaded', 'error');

    const newErrors = {};
    if (!form.firstName || !form.firstName.trim()) newErrors.firstName = 'Required';
    if (!form.lastName || !form.lastName.trim()) newErrors.lastName = 'Required';
    if (!form.username || !form.username.trim()) newErrors.username = 'Required';
    setErrors(newErrors);
    if (Object.keys(newErrors).length) {
      toast.push('Please fix the highlighted fields', 'error');
      return;
    }

    try {
      setSaving(true);
      await api.put(`/api/v1.0/users/${profile.userId}`, form);
      setProfile(p => ({ ...p, ...form }));
      setEditing(false);
      toast.push('Profile saved', 'success');
    } catch (err) {
      console.error('Save failed', err);
      toast.push(err?.data?.message || err.message || 'Save failed', 'error');
    } finally {
      setSaving(false);
    }
  }

  async function handleChangePassword(e) {
    e.preventDefault();
    const current = e.target.current.value;
    const next = e.target.password.value;
    const pwErrors = [];
    if (!current || !next) pwErrors.push('Provide current and new password');
    if (next.length < 8) pwErrors.push('New password must be at least 8 characters');
    if (!/\d/.test(next)) pwErrors.push('New password must contain at least one number');
    if (pwErrors.length) {
      pwErrors.forEach(m => toast.push(m, 'error'));
      return;
    }

    try {
      setSaving(true);
      await api.post('/api/v1.0/auth/change-password', { currentPassword: current, newPassword: next });
      toast.push('Password changed', 'success');
      e.target.reset();
    } catch (err) {
      console.error('Password change failed', err);
      toast.push(err?.data?.message || err.message || 'Password change failed', 'error');
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <div className="dc-content">Loading profile...</div>;
  if (!profile) return <div className="dc-content">Failed to load profile</div>;

  const avatarLetter = (profile.username || profile.email || 'U')[0]?.toUpperCase();

  return (
    <div className="dc-content">
      {showLoginModal && (
        <SimpleModal title="Sign in" onClose={() => setShowLoginModal(false)}>
          <div className="w-[440px]">
            <Login onSuccess={() => { setShowLoginModal(false); loadProfile(); }} />
          </div>
        </SimpleModal>
      )}

      <div className="max-w-6xl mx-auto py-6">
        <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} className="bg-gray-900 border border-gray-800 rounded-lg p-6 shadow-md">
          <div className="flex gap-6 items-center">
            <div className="w-28 text-center">
              <div className="w-22 h-22 mx-auto mb-2 rounded-xl flex items-center justify-center text-3xl font-bold text-white" style={{ background: 'linear-gradient(135deg,var(--accent-blue),var(--accent-purple))', width: 88, height: 88 }}>
                {avatarLetter}
              </div>
              <div className="text-sm text-gray-400">{profile.role || 'Customer'}</div>
            </div>

            <div className="flex-1">
              <div className="flex items-start justify-between">
                <div>
                  <h1 className="text-2xl font-semibold text-gray-100">{profile.username}</h1>
                  <div className="text-sm text-gray-400 mt-1">{profile.email}</div>
                </div>

                <div className="flex items-center gap-2">
                  <button onClick={loadProfile} title="Refresh profile" className="text-gray-300 hover:text-white p-2 rounded border border-gray-800 bg-gray-800"><RefreshCcw size={16} /></button>
                  <button onClick={() => setEditing(v => !v)} className="px-3 py-1.5 rounded bg-teal-500 hover:bg-teal-600 text-white text-sm">{editing ? 'Cancel' : 'Edit'}</button>
                </div>
              </div>

              <form onSubmit={handleSave} className="mt-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm text-gray-300">First name {errors.firstName && <span className="text-red-400 ml-2">{errors.firstName}</span>}</label>
                    <input name="firstName" value={form.firstName} onChange={handleChange} disabled={!editing} className="mt-1 w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-400" />
                  </div>
                  <div>
                    <label className="text-sm text-gray-300">Last name {errors.lastName && <span className="text-red-400 ml-2">{errors.lastName}</span>}</label>
                    <input name="lastName" value={form.lastName} onChange={handleChange} disabled={!editing} className="mt-1 w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-400" />
                  </div>

                  <div>
                    <label className="text-sm text-gray-300">Username {errors.username && <span className="text-red-400 ml-2">{errors.username}</span>}</label>
                    <input name="username" value={form.username} onChange={handleChange} disabled={!editing} className="mt-1 w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-400" />
                  </div>
                  <div>
                    <label className="text-sm text-gray-300">Phone</label>
                    <input name="phoneNumber" value={form.phoneNumber} onChange={handleChange} disabled={!editing} className="mt-1 w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-400" />
                  </div>
                </div>

                <div className="mt-4 flex gap-3">
                  {editing && <button type="submit" disabled={saving} className="px-3 py-1.5 rounded bg-teal-500 hover:bg-teal-600 text-white text-sm">{saving ? 'Saving...' : 'Save changes'}</button>}
                  <button type="button" onClick={loadProfile} className="px-3 py-1.5 rounded bg-gray-800 border border-gray-700 text-sm text-gray-200">Reset</button>
                </div>
              </form>

              <div className="mt-6">
                <h3 className="text-lg font-medium text-gray-100">Security</h3>
                <p className="text-sm text-gray-400 mt-1">Change password</p>
                <form onSubmit={handleChangePassword} className="mt-3 grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label className="text-sm text-gray-300">Current password</label>
                    <input name="current" type="password" className="mt-1 w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100" />
                  </div>
                  <div>
                    <label className="text-sm text-gray-300">New password</label>
                    <input name="password" type="password" className="mt-1 w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100" />
                  </div>
                  <div className="md:col-span-2">
                    <button type="submit" disabled={saving} className="px-3 py-1.5 rounded bg-teal-500 hover:bg-teal-600 text-white text-sm">Change password</button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </motion.div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-4">
          <motion.div className="md:col-span-2 bg-gray-900 border border-gray-800 rounded-lg p-4" initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
            <h3 className="text-lg font-medium text-gray-100">Activity</h3>
            <p className="text-sm text-gray-400 mt-2">Recent activity placeholder — integrate with backend events if desired.</p>
          </motion.div>

          <motion.div className="bg-gray-900 border border-gray-800 rounded-lg p-4" initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }}>
            <div className="flex items-center gap-3">
              <Wallet className="text-gray-300" />
              <div>
                <div className="text-sm text-gray-400">Wallet balance</div>
                <div className="text-xl font-semibold text-gray-100">${(profile.walletBalance || 0).toFixed(2)}</div>
              </div>
            </div>
          </motion.div>
        </div>
      </div>
    </div>
  );
}
