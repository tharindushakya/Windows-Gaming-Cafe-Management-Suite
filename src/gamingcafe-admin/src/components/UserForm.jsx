import React, { useState, useEffect } from 'react';

export default function UserForm({ initial = {}, onCancel, onSubmit, errors = {} }) {
  const [username, setUsername] = useState(initial.username ?? initial.Username ?? '');
  const [email, setEmail] = useState(initial.email ?? initial.Email ?? '');
  const [firstName, setFirstName] = useState(initial.firstName ?? initial.FirstName ?? '');
  const [lastName, setLastName] = useState(initial.lastName ?? initial.LastName ?? '');
  const [role, setRole] = useState(initial.role ?? initial.Role ?? 'User');
  const [password, setPassword] = useState('');
  const [initialWalletBalance, setInitialWalletBalance] = useState(initial.walletBalance ?? initial.WalletBalance ?? 0);
  const [saving, setSaving] = useState(false);

  const isEditing = Boolean(initial.userId || initial.UserId);

  useEffect(() => {
    // update when initial changes (edit)
    setUsername(initial.username ?? initial.Username ?? '');
    setEmail(initial.email ?? initial.Email ?? '');
    setFirstName(initial.firstName ?? initial.FirstName ?? '');
    setLastName(initial.lastName ?? initial.LastName ?? '');
    setRole(initial.role ?? initial.Role ?? 'User');
    setInitialWalletBalance(initial.walletBalance ?? initial.WalletBalance ?? 0);
  }, [initial]);

  async function submit(e) {
    e.preventDefault();
    try {
      setSaving(true);
      const payload = {
        username,
        email,
        firstName,
        lastName,
        role,
        // only send password on create or if user provided one
        ...(password ? { password } : {}),
        initialWalletBalance
      };
      await onSubmit(payload);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-6">
      <form onSubmit={submit} className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Username</label>
            <input 
              value={username} 
              onChange={e => setUsername(e.target.value)} 
              className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="Enter username"
              required
            />
            {errors?.username && (
              <p className="mt-1 text-sm text-red-600">{errors.username.join(', ')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
            <input 
              type="email"
              value={email} 
              onChange={e => setEmail(e.target.value)} 
              className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="Enter email address"
              required
            />
            {errors?.email && (
              <p className="mt-1 text-sm text-red-600">{errors.email.join(', ')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">First Name</label>
            <input 
              value={firstName} 
              onChange={e => setFirstName(e.target.value)} 
              className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="Enter first name"
              required
            />
            {errors?.firstName && (
              <p className="mt-1 text-sm text-red-600">{errors.firstName.join(', ')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Last Name</label>
            <input 
              value={lastName} 
              onChange={e => setLastName(e.target.value)} 
              className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="Enter last name"
              required
            />
            {errors?.lastName && (
              <p className="mt-1 text-sm text-red-600">{errors.lastName.join(', ')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Role</label>
            <select 
              value={role} 
              onChange={e => setRole(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="Admin">Admin</option>
              <option value="Manager">Manager</option>
              <option value="Staff">Staff</option>
              <option value="User">User</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Password {isEditing && <span className="text-gray-500 text-xs">(leave blank to keep current)</span>}
            </label>
            <input 
              type="password" 
              value={password} 
              onChange={e => setPassword(e.target.value)} 
              className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder={isEditing ? "Enter new password" : "Enter password"}
              {...(!isEditing && { required: true })}
            />
            {!isEditing && (
              <p className="mt-1 text-xs text-gray-500">Password must be at least 8 characters long</p>
            )}
            {errors?.password && (
              <p className="mt-1 text-sm text-red-600">{errors.password.join(', ')}</p>
            )}
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Initial Wallet Balance ($)</label>
          <div className="relative">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <span className="text-gray-500 sm:text-sm">$</span>
            </div>
            <input 
              type="number" 
              step="0.01" 
              min="0"
              value={initialWalletBalance} 
              onChange={e => setInitialWalletBalance(parseFloat(e.target.value || 0))} 
              className="w-full pl-7 pr-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="0.00"
            />
          </div>
          <p className="mt-1 text-xs text-gray-500">
            {isEditing ? "Adjust the user's wallet balance" : "Set the initial wallet balance for the new user"}
          </p>
        </div>

        {errors?._global && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center gap-2 text-red-700">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {errors._global.join(', ')}
            </div>
          </div>
        )}

        <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
          <button 
            type="button" 
            onClick={onCancel} 
            disabled={saving}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
          >
            Cancel
          </button>
          <button 
            type="submit" 
            disabled={saving}
            className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
          >
            {saving ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                {isEditing ? 'Updating...' : 'Creating...'}
              </>
            ) : (
              isEditing ? 'Update User' : 'Create User'
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
