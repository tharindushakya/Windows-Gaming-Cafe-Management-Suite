import React, { useEffect, useState } from 'react';
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

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-4xl mx-auto px-6 py-8">
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
            <div className="inline-flex items-center gap-3 text-gray-500">
              <svg className="animate-spin h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Loading profile...
            </div>
          </div>
        </div>
      </div>
    );
  }
  
  if (!profile) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-4xl mx-auto px-6 py-8">
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center gap-2 text-red-700">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              Failed to load profile
            </div>
          </div>
        </div>
      </div>
    );
  }

  const avatarLetter = (profile.username || profile.email || 'U')[0]?.toUpperCase();

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-6 py-8">
        {showLoginModal && (
          <SimpleModal title="Sign in" onClose={() => setShowLoginModal(false)}>
            <div className="w-[440px]">
              <Login onSuccess={() => { setShowLoginModal(false); loadProfile(); }} />
            </div>
          </SimpleModal>
        )}

        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">Profile</h1>
            <p className="text-gray-600">Manage your account settings and information</p>
          </div>
          <div className="mt-4 sm:mt-0 flex items-center gap-2">
            <button 
              onClick={loadProfile} 
              className="inline-flex items-center px-3 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 transition-colors duration-200"
              title="Refresh profile"
            >
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Refresh
            </button>
            <button 
              onClick={() => setEditing(v => !v)} 
              className={`inline-flex items-center px-4 py-2 text-sm font-medium rounded-lg transition-colors duration-200 ${
                editing 
                  ? 'bg-gray-100 text-gray-700 border border-gray-300 hover:bg-gray-200' 
                  : 'bg-indigo-600 text-white hover:bg-indigo-700'
              }`}
            >
              {editing ? 'Cancel' : 'Edit Profile'}
            </button>
          </div>
        </div>

        {/* Profile Card */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <div className="p-6">
            <div className="flex items-start gap-6">
              {/* Avatar */}
              <div className="flex-shrink-0">
                <div className="w-20 h-20 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-full flex items-center justify-center text-2xl font-bold text-white">
                  {avatarLetter}
                </div>
                <div className="mt-2 text-center">
                  <span className="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-indigo-100 text-indigo-800">
                    {profile.role || 'Customer'}
                  </span>
                </div>
              </div>

              {/* Profile Info */}
              <div className="flex-1">
                <div className="mb-4">
                  <h2 className="text-2xl font-bold text-gray-900">{profile.username}</h2>
                  <p className="text-gray-600">{profile.email}</p>
                </div>

                <form onSubmit={handleSave}>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        First Name {errors.firstName && <span className="text-red-500 ml-1">{errors.firstName}</span>}
                      </label>
                      <input 
                        name="firstName" 
                        value={form.firstName} 
                        onChange={handleChange} 
                        disabled={!editing} 
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-50 disabled:text-gray-500"
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Last Name {errors.lastName && <span className="text-red-500 ml-1">{errors.lastName}</span>}
                      </label>
                      <input 
                        name="lastName" 
                        value={form.lastName} 
                        onChange={handleChange} 
                        disabled={!editing} 
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-50 disabled:text-gray-500"
                      />
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Username {errors.username && <span className="text-red-500 ml-1">{errors.username}</span>}
                      </label>
                      <input 
                        name="username" 
                        value={form.username} 
                        onChange={handleChange} 
                        disabled={!editing} 
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-50 disabled:text-gray-500"
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Phone Number</label>
                      <input 
                        name="phoneNumber" 
                        value={form.phoneNumber} 
                        onChange={handleChange} 
                        disabled={!editing} 
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-50 disabled:text-gray-500"
                      />
                    </div>
                  </div>

                  {editing && (
                    <div className="mt-6 flex gap-3">
                      <button 
                        type="submit" 
                        disabled={saving} 
                        className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                      >
                        {saving ? 'Saving...' : 'Save Changes'}
                      </button>
                      <button 
                        type="button" 
                        onClick={loadProfile} 
                        className="inline-flex items-center px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg transition-colors duration-200"
                      >
                        Reset
                      </button>
                    </div>
                  )}
                </form>
              </div>
            </div>
          </div>
        </div>

        {/* Security Section */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 mt-6">
          <div className="p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Security</h3>
            <p className="text-gray-600 mb-6">Update your password to keep your account secure</p>
            
            <form onSubmit={handleChangePassword} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
                  <input 
                    name="current" 
                    type="password" 
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                  <input 
                    name="password" 
                    type="password" 
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                  />
                </div>
              </div>
              <div>
                <button 
                  type="submit" 
                  disabled={saving} 
                  className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                >
                  Change Password
                </button>
              </div>
            </form>
          </div>
        </div>

        {/* Additional Info */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-6">
          {/* Wallet */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <div className="flex items-center gap-4">
              <div className="p-3 bg-green-100 rounded-lg">
                <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
              <div>
                <h4 className="text-sm font-medium text-gray-700">Wallet Balance</h4>
                <p className="text-2xl font-bold text-gray-900">${(profile.walletBalance || 0).toFixed(2)}</p>
              </div>
            </div>
          </div>

          {/* Activity */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <div className="flex items-center gap-4">
              <div className="p-3 bg-blue-100 rounded-lg">
                <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                </svg>
              </div>
              <div>
                <h4 className="text-sm font-medium text-gray-700">Account Activity</h4>
                <p className="text-sm text-gray-500 mt-1">Recent activity tracking available</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
