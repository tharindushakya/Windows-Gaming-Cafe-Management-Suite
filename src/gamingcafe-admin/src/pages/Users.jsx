import React, { useEffect, useState, useCallback, useRef } from 'react';
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
  const [search, setSearch] = useState('');
  const searchRef = useRef(null);
  const toast = useToast();

  const fetchUsers = useCallback(async (p = 1, q = '') => {
    setLoading(true);
    setError(null);
    try {
      const qs = `?page=${p}&pageSize=${pageSize}` + (q ? `&search=${encodeURIComponent(q)}` : '');
      const res = await api.get(`/api/v1.0/users${qs}`);
      const list = res?.data ?? res ?? [];
      setUsers(list);
      setTotalCount(res?.totalCount ?? res?.total ?? list.length);
    } catch (err) {
      setError(err?.data?.message || err.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  useEffect(() => { fetchUsers(page, search); }, [fetchUsers, page, search]);

  // debounced search
  useEffect(() => {
    if (searchRef.current) clearTimeout(searchRef.current);
    searchRef.current = setTimeout(() => { setPage(1); fetchUsers(1, search); }, 300);
    return () => clearTimeout(searchRef.current);
  }, [search, fetchUsers]);

  function openCreate() { setEditingUser(null); setShowModal(true); }
  function openEdit(u) { setEditingUser(u); setShowModal(true); }

  async function handleSave(payload) {
    try {
      if (editingUser && (editingUser.userId || editingUser.UserId)) {
        const id = editingUser.userId ?? editingUser.UserId;
        await api.put(`/api/v1.0/users/${id}`, payload);
        toast?.push('User updated', 'success');
      } else {
        await api.post('/api/v1.0/users', payload);
        toast?.push('User created', 'success');
      }
      setShowModal(false);
      fetchUsers(page, search);
    } catch (err) {
      toast?.push(err?.data?.message || err.message || 'Failed to save user', 'error');
      throw err;
    }
  }

  function askDelete(u) { setConfirm(u); }

  async function doDelete(u) {
    try {
      const id = u.userId ?? u.UserId;
      await api.del(`/api/v1.0/users/${id}`);
      setConfirm(null);
      toast?.push('User deleted', 'success');
      fetchUsers(page, search);
    } catch (err) {
      toast?.push(err?.data?.message || err.message || 'Failed to delete user', 'error');
    }
  }

  function nextPage() { if (page * pageSize < totalCount) setPage(prev => prev + 1); }
  function prevPage() { if (page > 1) setPage(prev => prev - 1); }

  return (
    <div className="p-4">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-4">
        <div>
          <h2 className="text-2xl font-semibold">Users</h2>
          <p className="text-sm text-gray-600">Manage user accounts, wallets and profiles.</p>
        </div>

        <div className="flex gap-2 items-center">
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by username or email" className="border rounded px-3 py-2 w-60" />
          <button className="px-3 py-2 bg-sky-600 text-white rounded" onClick={openCreate}>Create user</button>
        </div>
      </div>

      {loading && <div className="py-6 text-center">Loading users...</div>}
      {error && <div className="py-2 text-red-600">{error}</div>}

      <div className="overflow-x-auto bg-white border border-gray-200 rounded">
        <table className="w-full min-w-[720px] table-auto">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left p-3 text-sm font-medium">ID</th>
              <th className="text-left p-3 text-sm font-medium">Username</th>
              <th className="text-left p-3 text-sm font-medium">Email</th>
              <th className="text-left p-3 text-sm font-medium">Role</th>
              <th className="text-right p-3 text-sm font-medium">Wallet</th>
              <th className="text-right p-3 text-sm font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.length === 0 && !loading && (
              <tr>
                <td colSpan={6} className="p-6 text-center text-gray-500">No users found.</td>
              </tr>
            )}
            {users.map(u => (
              <tr key={u.userId ?? u.UserId} className="border-t hover:bg-gray-50">
                <td className="p-3 text-sm">{u.userId ?? u.UserId}</td>
                <td className="p-3 text-sm">{u.username ?? u.Username}</td>
                <td className="p-3 text-sm">{u.email ?? u.Email}</td>
                <td className="p-3 text-sm">{u.role ?? u.Role}</td>
                <td className="p-3 text-sm text-right">{Number(u.walletBalance ?? u.WalletBalance ?? 0).toFixed(2)}</td>
                <td className="p-3 text-sm text-right">
                  <div className="inline-flex items-center gap-2">
                    <button className="px-2 py-1 text-sm border rounded" onClick={() => openEdit(u)}>Edit</button>
                    <button className="px-2 py-1 text-sm border rounded text-red-600" onClick={() => askDelete(u)}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mt-3 flex items-center justify-between">
        <div className="text-sm">Page {page} — {totalCount} users</div>
        <div>
          <button className="px-3 py-1 mr-2 border rounded disabled:opacity-50" onClick={prevPage} disabled={page === 1}>Prev</button>
          <button className="px-3 py-1 border rounded disabled:opacity-50" onClick={nextPage} disabled={page * pageSize >= totalCount}>Next</button>
        </div>
      </div>

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
