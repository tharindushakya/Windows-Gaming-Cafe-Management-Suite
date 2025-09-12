import React, { useState, useEffect } from 'react';

export default function UserForm({ initial = {}, onCancel, onSubmit }) {
  const [username, setUsername] = useState(initial.username ?? initial.Username ?? '');
  const [email, setEmail] = useState(initial.email ?? initial.Email ?? '');
  const [firstName, setFirstName] = useState(initial.firstName ?? initial.FirstName ?? '');
  const [lastName, setLastName] = useState(initial.lastName ?? initial.LastName ?? '');
  const [role, setRole] = useState(initial.role ?? initial.Role ?? 'User');
  const [password, setPassword] = useState('');
  const [initialWalletBalance, setInitialWalletBalance] = useState(initial.walletBalance ?? initial.WalletBalance ?? 0);

  useEffect(() => {
    // update when initial changes (edit)
    setUsername(initial.username ?? initial.Username ?? '');
    setEmail(initial.email ?? initial.Email ?? '');
    setFirstName(initial.firstName ?? initial.FirstName ?? '');
    setLastName(initial.lastName ?? initial.LastName ?? '');
    setRole(initial.role ?? initial.Role ?? 'User');
    setInitialWalletBalance(initial.walletBalance ?? initial.WalletBalance ?? 0);
  }, [initial]);

  function submit(e) {
    e.preventDefault();
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
    onSubmit(payload);
  }

  return (
    <form onSubmit={submit}>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
        <div>
          <label>Username</label>
          <input value={username} onChange={e => setUsername(e.target.value)} />
        </div>
        <div>
          <label>Email</label>
          <input value={email} onChange={e => setEmail(e.target.value)} />
        </div>
        <div>
          <label>First name</label>
          <input value={firstName} onChange={e => setFirstName(e.target.value)} />
        </div>
        <div>
          <label>Last name</label>
          <input value={lastName} onChange={e => setLastName(e.target.value)} />
        </div>
        <div>
          <label>Role</label>
          <select value={role} onChange={e => setRole(e.target.value)}>
            <option>Admin</option>
            <option>Manager</option>
            <option>Staff</option>
            <option>User</option>
          </select>
        </div>
        <div>
          <label>Password (leave blank to keep)</label>
          <input type="password" value={password} onChange={e => setPassword(e.target.value)} />
        </div>
      </div>

      <div style={{ marginTop: 12 }}>
        <label>Initial wallet balance</label>
        <input type="number" step="0.01" value={initialWalletBalance} onChange={e => setInitialWalletBalance(parseFloat(e.target.value || 0))} />
      </div>

      <div style={{ marginTop: 12, display: 'flex', gap: 8 }}>
        <button type="submit">Save</button>
        <button type="button" onClick={onCancel}>Cancel</button>
      </div>
    </form>
  );
}
