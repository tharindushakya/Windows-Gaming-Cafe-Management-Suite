import React, { useEffect, useState, useCallback } from 'react';
import api from '../api';
import { useToast } from '../components/ToastProvider';
import SimpleModal from '../components/SimpleModal';
import UserForm from '../components/UserForm';
import ConfirmDialog from '../components/ConfirmDialog';

export default function Users() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  const [showModal, setShowModal] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [confirm, setConfirm] = useState(null);
  const toast = useToast();

  const fetchUsers = useCallback(async (p = 1) => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.get(`/api/v1.0/users?page=${p}&pageSize=${pageSize}`);
      // Expecting a paged response { data: [...], page, pageSize, totalCount }
      const list = res?.data || res?.Data || res || [];
      setUsers(list);
      setTotalCount(res?.totalCount ?? res?.TotalCount ?? list.length);
    } catch (err) {
      setError(err?.data?.message || err.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  useEffect(() => {
    fetchUsers(page);
  }, [fetchUsers, page]);

  function openCreate() {
    setEditingUser(null);
    setShowModal(true);
  }

  function openEdit(u) {
    setEditingUser(u);
    setShowModal(true);
  }

  async function handleSave(payload) {
    try {
      if (editingUser && (editingUser.userId || editingUser.UserId)) {
        const id = editingUser.userId ?? editingUser.UserId;
  await api.put(`/api/v1.0/users/${id}`, payload);
      } else {
        // Create user. API may expect auth/register or users endpoint. Try users endpoint first.
        await api.post('/api/v1.0/users', payload);
      }
      setShowModal(false);
      fetchUsers(page);
    } catch (err) {
      toast.push(err?.data?.message || err.message || 'Failed to save user', 'error');
    }
  }

  function askDelete(u) { setConfirm(u); }

  async function doDelete(u) {
    try {
      const id = u.userId ?? u.UserId;
      await api.del(`/api/v1.0/users/${id}`);
      setConfirm(null);
      fetchUsers(page);
    } catch (err) {
      toast.push(err?.data?.message || err.message || 'Failed to delete user', 'error');
    }
  }

  function nextPage() {
    if (page * pageSize < totalCount) setPage(prev => prev + 1);
  }

  function prevPage() {
    if (page > 1) setPage(prev => prev - 1);
  }

  return (
    <div style={{ padding: 16 }}>
      <h2>Users</h2>
      <p>Manage user accounts, wallets and profiles.</p>

      {loading && <div>Loading users...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}

      <div style={{ marginBottom: 12 }}>
        <button onClick={openCreate}>Create user</button>
      </div>

      {!loading && !error && (
        <>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 8 }}>ID</th>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 8 }}>Username</th>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 8 }}>Email</th>
                <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 8 }}>Role</th>
                <th style={{ textAlign: 'right', borderBottom: '1px solid #ddd', padding: 8 }}>Wallet</th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 && (
                <tr>
                  <td colSpan={5} style={{ padding: 12 }}>No users found.</td>
                </tr>
              )}
              {users.map(u => (
                <tr key={u.userId ?? u.UserId}>
                  <td style={{ padding: 8, borderBottom: '1px solid #f0f0f0' }}>{u.userId ?? u.UserId}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f0f0f0' }}>{u.username ?? u.Username}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f0f0f0' }}>{u.email ?? u.Email}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f0f0f0' }}>{u.role ?? u.Role}</td>
                  <td style={{ padding: 8, textAlign: 'right', borderBottom: '1px solid #f0f0f0' }}>{(u.walletBalance ?? u.WalletBalance ?? 0).toFixed ? (u.walletBalance ?? u.WalletBalance ?? 0).toFixed(2) : u.walletBalance ?? u.WalletBalance ?? 0}</td>
                  <td style={{ padding: 8, borderBottom: '1px solid #f0f0f0', textAlign: 'right' }}>
                    <button onClick={() => openEdit(u)} style={{ marginRight: 8 }}>Edit</button>
                    <button onClick={() => askDelete(u)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div style={{ marginTop: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              Page {page} — {totalCount} users
            </div>
            <div>
              <button onClick={prevPage} disabled={page === 1} style={{ marginRight: 8 }}>Prev</button>
              <button onClick={nextPage} disabled={page * pageSize >= totalCount}>Next</button>
            </div>
          </div>
        </>
      )}

      {showModal && (
        <SimpleModal title={editingUser ? 'Edit user' : 'Create user'} onClose={() => setShowModal(false)}>
          <UserForm initial={editingUser ?? {}} onCancel={() => setShowModal(false)} onSubmit={handleSave} />
        </SimpleModal>
      )}
      {confirm && (
        <SimpleModal title="Confirm delete" onClose={() => setConfirm(null)}>
          <ConfirmDialog message={`Delete user ${confirm.username || confirm.email || confirm.userId}?`} onConfirm={() => doDelete(confirm)} onCancel={() => setConfirm(null)} />
        </SimpleModal>
      )}
    </div>
  );
}
