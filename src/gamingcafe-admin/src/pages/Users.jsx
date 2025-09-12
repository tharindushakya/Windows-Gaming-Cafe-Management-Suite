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
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">Users</h1>
            <p className="text-gray-600">Manage user accounts, wallets and profiles</p>
          </div>
          <div className="mt-4 sm:mt-0">
            <button 
              className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg shadow-sm transition-colors duration-200 gap-2"
              onClick={openCreate}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Create User
            </button>
          </div>
        </div>

        {/* Search & Filters */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
          <div className="flex flex-col sm:flex-row sm:items-center gap-4">
            <div className="relative flex-1">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </div>
              <input 
                value={search} 
                onChange={e => setSearch(e.target.value)} 
                placeholder="Search by username or email..." 
                className="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg leading-5 bg-white placeholder-gray-500 focus:outline-none focus:placeholder-gray-400 focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
          </div>
        </div>

        {loading && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
            <div className="inline-flex items-center gap-3 text-gray-500">
              <svg className="animate-spin h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Loading users...
            </div>
          </div>
        )}
        
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
            <div className="flex items-center gap-2 text-red-700">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {error}
            </div>
          </div>
        )}

        
        {/* Users Table */}
        {!loading && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px]">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Role</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Wallet Balance</th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {users.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-12 text-center text-gray-500">
                        <div className="flex flex-col items-center">
                          <svg className="w-12 h-12 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                          </svg>
                          <p className="text-lg font-medium text-gray-900 mb-1">No users found</p>
                          <p className="text-gray-500">Create your first user to get started</p>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    users.map(u => (
                      <tr key={u.userId ?? u.UserId} className="hover:bg-gray-50 transition-colors duration-200">
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {u.userId ?? u.UserId}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center">
                            <div className="flex-shrink-0 h-8 w-8">
                              <div className="h-8 w-8 rounded-full bg-indigo-100 flex items-center justify-center">
                                <span className="text-sm font-medium text-indigo-600">
                                  {(u.username ?? u.Username ?? 'U').charAt(0).toUpperCase()}
                                </span>
                              </div>
                            </div>
                            <div className="ml-3">
                              <div className="text-sm font-medium text-gray-900">{u.username ?? u.Username}</div>
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{u.email ?? u.Email}</td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                            (u.role ?? u.Role) === 'admin' ? 'bg-purple-100 text-purple-800' : 
                            (u.role ?? u.Role) === 'staff' ? 'bg-blue-100 text-blue-800' : 
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {u.role ?? u.Role ?? 'user'}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          ${Number(u.walletBalance ?? u.WalletBalance ?? 0).toFixed(2)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          <div className="flex items-center justify-end gap-2">
                            <button 
                              className="text-indigo-600 hover:text-indigo-700 font-medium transition-colors duration-200"
                              onClick={() => openEdit(u)}
                            >
                              Edit
                            </button>
                            <button 
                              className="text-red-600 hover:text-red-700 font-medium transition-colors duration-200"
                              onClick={() => askDelete(u)}
                            >
                              Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Pagination */}
        {!loading && users.length > 0 && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 px-6 py-4 mt-6">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700">
                Page <span className="font-medium">{page}</span> — <span className="font-medium">{totalCount}</span> users total
              </div>
              <div className="flex items-center gap-2">
                <button 
                  className="px-3 py-1 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  onClick={prevPage} 
                  disabled={page === 1}
                >
                  Previous
                </button>
                <button 
                  className="px-3 py-1 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  onClick={nextPage} 
                  disabled={page * pageSize >= totalCount}
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Modals */}
        {showModal && (
          <SimpleModal title={editingUser ? 'Edit User' : 'Create User'} onClose={() => setShowModal(false)}>
            <UserForm initial={editingUser ?? {}} onCancel={() => setShowModal(false)} onSubmit={handleSave} />
          </SimpleModal>
        )}
        
        {confirm && (
          <SimpleModal title="Confirm Delete" onClose={() => setConfirm(null)}>
            <ConfirmDialog 
              message={`Are you sure you want to delete user "${confirm.username || confirm.email || confirm.userId}"? This action cannot be undone.`} 
              onConfirm={() => doDelete(confirm)} 
              onCancel={() => setConfirm(null)} 
            />
          </SimpleModal>
        )}
      </div>
      
    </div>
  );
}
