import React, { useState, useEffect, useCallback, useRef } from 'react';
import api from '../api';
import { useToast } from '../components/ToastProvider';
import SimpleModal from '../components/SimpleModal';
import ConfirmDialog from '../components/ConfirmDialog';
import LoadingSpinner from '../components/LoadingSpinner';
import { useHasRole } from '../utils/security';

function fmtDate(d) {
  if (!d) return '';
  try { return new Date(d).toLocaleString(); } catch { return String(d); }
}

function fmtAmount(amount) {
  return `$${(amount || 0).toFixed(2)}`;
}

function getStatusColor(status) {
  switch (status?.toLowerCase()) {
    case 'active': return '#10b981';
    case 'inactive': return '#ef4444';
    default: return '#6b7280';
  }
}

function getTransactionTypeColor(type) {
  switch (type?.toLowerCase()) {
    case 'deposit': return '#10b981';
    case 'withdrawal': return '#ef4444';
    case 'transfer': return '#0ea5e9';
    case 'refund': return '#8b5cf6';
    case 'purchase': return '#f59e0b';
    default: return '#6b7280';
  }
}

export default function Wallet() {
  // Data state
  const [users, setUsers] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [stats, setStats] = useState(null);
  const [selectedWallet, setSelectedWallet] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [transactionPage, setTransactionPage] = useState(1);
  const [transactionTotal, setTransactionTotal] = useState(0);

  // Filter state
  const [filters, setFilters] = useState({
    search: '',
    role: '',
    isActive: '',
    minBalance: '',
    maxBalance: ''
  });

  // Debounced search state
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const searchTimeoutRef = useRef(null);

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [modalType, setModalType] = useState(''); // 'deposit', 'withdraw', 'transfer', 'view', 'status'
  const [confirmAction, setConfirmAction] = useState(null);

  // Form state
  const [depositForm, setDepositForm] = useState({ 
    userId: '', amount: '', description: '', paymentMethod: 'Cash' 
  });
  const [withdrawForm, setWithdrawForm] = useState({ 
    userId: '', amount: '', description: '', paymentMethod: 'Cash' 
  });
  const [transferForm, setTransferForm] = useState({
    fromUserId: '', toUserId: '', amount: '', description: ''
  });
  const [statusForm, setStatusForm] = useState({ userId: '', isActive: true });

  const toast = useToast();
  const hasRole = useHasRole();

  // Load users when page or filters change
  useEffect(() => {
    const fetchUsers = async () => {
      setLoading(true);
      try {
        const params = new URLSearchParams({
          page: page.toString(),
          pageSize: pageSize.toString(),
          sortBy: 'Username',
          sortDirection: 'asc'
        });

        // Add filters - use debouncedSearch for search to prevent too many requests
        if (debouncedSearch) params.append('searchTerm', debouncedSearch);
        if (filters.role) params.append('role', filters.role);
        if (filters.isActive !== '') params.append('isActive', filters.isActive);

        const resp = await api.get(`/api/v1.0/users?${params}`);
        
        // Filter by balance range if specified
        let filteredUsers = resp?.data || [];
        if (filters.minBalance) {
          const minBalance = parseFloat(filters.minBalance);
          filteredUsers = filteredUsers.filter(u => (u.walletBalance || 0) >= minBalance);
        }
        if (filters.maxBalance) {
          const maxBalance = parseFloat(filters.maxBalance);
          filteredUsers = filteredUsers.filter(u => (u.walletBalance || 0) <= maxBalance);
        }

        setUsers(filteredUsers);
        setTotalCount(resp?.totalCount || 0);
      } catch (err) {
        toast.push(err?.message || 'Failed to load wallet data', 'error');
      } finally {
        setLoading(false);
      }
    };

    fetchUsers();
  }, [page, pageSize, debouncedSearch, filters.role, filters.isActive, filters.minBalance, filters.maxBalance, toast]);

  // Reset page when filters change (but not when page itself changes)
  useEffect(() => {
    if (page !== 1) {
      setPage(1);
    }
  }, [debouncedSearch, filters.role, filters.isActive, filters.minBalance, filters.maxBalance]);

  // Fetch wallet statistics - memoized to prevent infinite loops
  const fetchStats = useCallback(async () => {
    try {
      const resp = await api.get('/api/v1.0/wallet/statistics');
      setStats(resp);
    } catch (err) {
      console.warn('Failed to load wallet statistics:', err);
    }
  }, []);

  // Fetch wallet transactions for selected user
  const fetchTransactions = useCallback(async (userId, page = 1) => {
    if (!userId) return;
    
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: '10',
        sortBy: 'Date',
        sortDescending: 'true'
      });

      const resp = await api.get(`/api/v1.0/wallet/${userId}/transactions?${params}`);
      setTransactions(resp?.data || []);
      setTransactionPage(resp?.page || page);
      setTransactionTotal(resp?.totalCount || 0);
    } catch (err) {
      toast.push(err?.message || 'Failed to load transactions', 'error');
    } finally {
      setLoading(false);
    }
  }, [toast]);

  // Load stats on component mount
  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  // Debounce search input to prevent too many requests
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    
    searchTimeoutRef.current = setTimeout(() => {
      setDebouncedSearch(filters.search);
    }, 300); // 300ms delay

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [filters.search]);

  // Refresh data after transactions
  const refreshData = useCallback(() => {
    // Trigger a refresh by updating a dummy state
    setUsers([]); // This will cause the main useEffect to re-run
    fetchStats();
  }, [fetchStats]);

  // Handle filter changes
  function handleFilterChange(e) {
    const { name, value } = e.target;
    setFilters(prev => ({ ...prev, [name]: value }));
  }

  // Clear all filters
  function clearFilters() {
    setFilters({
      search: '', role: '', isActive: '', minBalance: '', maxBalance: ''
    });
    setDebouncedSearch('');
    setPage(1);
  }

  // Modal handlers
  function openDepositModal(user) {
    setModalType('deposit');
    setDepositForm({ 
      userId: user.userId, 
      amount: '', 
      description: '', 
      paymentMethod: 'Cash' 
    });
    setShowModal(true);
  }

  function openWithdrawModal(user) {
    setModalType('withdraw');
    setWithdrawForm({ 
      userId: user.userId, 
      amount: '', 
      description: '', 
      paymentMethod: 'Cash' 
    });
    setShowModal(true);
  }

  function openTransferModal() {
    setModalType('transfer');
    setTransferForm({ fromUserId: '', toUserId: '', amount: '', description: '' });
    setShowModal(true);
  }

  function openWalletDetails(user) {
    setModalType('view');
    setSelectedWallet(user);
    fetchTransactions(user.userId, 1);
    setShowModal(true);
  }

  function openStatusModal(user) {
    setModalType('status');
    setStatusForm({ userId: user.userId, isActive: user.isActive });
    setShowModal(true);
  }

  // Transaction handlers
  async function handleDeposit() {
    try {
      if (!depositForm.amount || parseFloat(depositForm.amount) <= 0) {
        toast.push('Please enter a valid amount', 'error');
        return;
      }

      const request = {
        amount: parseFloat(depositForm.amount),
        description: depositForm.description || 'Admin deposit',
        paymentMethod: depositForm.paymentMethod
      };

      await api.post(`/api/v1.0/wallet/${depositForm.userId}/deposit`, request);
      toast.push('Deposit successful', 'success');
      setShowModal(false);
      refreshData();
    } catch (err) {
      toast.push(err?.message || 'Failed to process deposit', 'error');
    }
  }

  async function handleWithdraw() {
    try {
      if (!withdrawForm.amount || parseFloat(withdrawForm.amount) <= 0) {
        toast.push('Please enter a valid amount', 'error');
        return;
      }

      const request = {
        amount: parseFloat(withdrawForm.amount),
        description: withdrawForm.description || 'Admin withdrawal',
        paymentMethod: withdrawForm.paymentMethod
      };

      await api.post(`/api/v1.0/wallet/${withdrawForm.userId}/withdraw`, request);
      toast.push('Withdrawal successful', 'success');
      setShowModal(false);
      refreshData();
    } catch (err) {
      toast.push(err?.message || 'Failed to process withdrawal', 'error');
    }
  }

  async function handleTransfer() {
    try {
      if (!transferForm.amount || parseFloat(transferForm.amount) <= 0) {
        toast.push('Please enter a valid amount', 'error');
        return;
      }

      if (transferForm.fromUserId === transferForm.toUserId) {
        toast.push('Cannot transfer to the same user', 'error');
        return;
      }

      const request = {
        fromUserId: parseInt(transferForm.fromUserId),
        toUserId: parseInt(transferForm.toUserId),
        amount: parseFloat(transferForm.amount),
        description: transferForm.description || 'Admin transfer'
      };

      await api.post('/api/v1.0/wallet/transfer', request);
      toast.push('Transfer successful', 'success');
      setShowModal(false);
      refreshData();
    } catch (err) {
      toast.push(err?.message || 'Failed to process transfer', 'error');
    }
  }

  async function handleStatusUpdate() {
    try {
      const request = { isActive: statusForm.isActive };
      await api.put(`/api/v1.0/wallet/${statusForm.userId}/status`, request);
      toast.push('Wallet status updated successfully', 'success');
      setShowModal(false);
      refreshData();
    } catch (err) {
      toast.push(err?.message || 'Failed to update wallet status', 'error');
    }
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Wallet Management</h1>
        
        {hasRole(['Admin', 'Manager']) && (
          <div className="flex space-x-3">
            <button
              onClick={openTransferModal}
              className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md font-medium transition-colors"
            >
              Transfer Money
            </button>
          </div>
        )}
      </div>

      {/* Statistics Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex items-center">
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-600">Total Wallets</p>
                <p className="text-2xl font-bold text-gray-900">{stats.totalWallets}</p>
              </div>
              <div className="p-3 bg-blue-100 rounded-full">
                <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v2" />
                </svg>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex items-center">
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-600">Active Wallets</p>
                <p className="text-2xl font-bold text-green-600">{stats.activeWallets}</p>
              </div>
              <div className="p-3 bg-green-100 rounded-full">
                <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex items-center">
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-600">Total Balance</p>
                <p className="text-2xl font-bold text-purple-600">{fmtAmount(stats.totalBalance)}</p>
              </div>
              <div className="p-3 bg-purple-100 rounded-full">
                <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
                </svg>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex items-center">
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-600">Avg Balance</p>
                <p className="text-2xl font-bold text-indigo-600">{fmtAmount(stats.averageWalletBalance)}</p>
              </div>
              <div className="p-3 bg-indigo-100 rounded-full">
                <svg className="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="bg-white p-6 rounded-lg shadow mb-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Search Users</label>
            <input
              type="text"
              name="search"
              value={filters.search}
              onChange={handleFilterChange}
              placeholder="Search by name, username, email..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Role</label>
            <select
              name="role"
              value={filters.role}
              onChange={handleFilterChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Roles</option>
              <option value="Admin">Admin</option>
              <option value="Manager">Manager</option>
              <option value="Staff">Staff</option>
              <option value="Customer">Customer</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Status</label>
            <select
              name="isActive"
              value={filters.isActive}
              onChange={handleFilterChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Status</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Min Balance</label>
            <input
              type="number"
              name="minBalance"
              value={filters.minBalance}
              onChange={handleFilterChange}
              placeholder="0.00"
              step="0.01"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Max Balance</label>
            <input
              type="number"
              name="maxBalance"
              value={filters.maxBalance}
              onChange={handleFilterChange}
              placeholder="999.99"
              step="0.01"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div className="mt-4 flex justify-end">
          <button
            onClick={clearFilters}
            className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition-colors"
          >
            Clear Filters
          </button>
        </div>
      </div>

      {/* Wallet List */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          {loading ? (
            <div className="flex justify-center items-center py-12">
              <LoadingSpinner />
            </div>
          ) : (
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    User
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Role
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Balance
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Last Login
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {users.map((user) => (
                  <tr key={user.userId} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div className="flex-shrink-0 h-10 w-10">
                          <div className="h-10 w-10 rounded-full bg-gray-300 flex items-center justify-center">
                            <span className="text-sm font-medium text-gray-700">
                              {user.firstName?.[0] || user.username?.[0] || 'U'}
                            </span>
                          </div>
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-medium text-gray-900">
                            {user.firstName} {user.lastName}
                          </div>
                          <div className="text-sm text-gray-500">@{user.username}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                        user.role === 'Admin' ? 'bg-red-100 text-red-800' :
                        user.role === 'Manager' ? 'bg-purple-100 text-purple-800' :
                        user.role === 'Staff' ? 'bg-blue-100 text-blue-800' :
                        'bg-gray-100 text-gray-800'
                      }`}>
                        {user.role}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {fmtAmount(user.walletBalance)}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span 
                        className="inline-flex px-2 py-1 text-xs font-semibold rounded-full"
                        style={{ 
                          backgroundColor: `${getStatusColor(user.isActive ? 'Active' : 'Inactive')}20`,
                          color: getStatusColor(user.isActive ? 'Active' : 'Inactive')
                        }}
                      >
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {fmtDate(user.lastLoginAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <div className="flex justify-end space-x-2">
                        <button
                          onClick={() => openWalletDetails(user)}
                          className="text-blue-600 hover:text-blue-900 px-2 py-1 rounded transition-colors"
                          title="View Details"
                        >
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                        </button>

                        {hasRole(['Admin', 'Manager', 'Staff']) && (
                          <>
                            <button
                              onClick={() => openDepositModal(user)}
                              className="text-green-600 hover:text-green-900 px-2 py-1 rounded transition-colors"
                              title="Deposit"
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                              </svg>
                            </button>

                            <button
                              onClick={() => openWithdrawModal(user)}
                              className="text-red-600 hover:text-red-900 px-2 py-1 rounded transition-colors"
                              title="Withdraw"
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M20 12H4" />
                              </svg>
                            </button>
                          </>
                        )}

                        {hasRole(['Admin', 'Manager']) && (
                          <button
                            onClick={() => openStatusModal(user)}
                            className="text-gray-600 hover:text-gray-900 px-2 py-1 rounded transition-colors"
                            title="Update Status"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            </svg>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {totalCount > pageSize && (
          <div className="bg-white px-4 py-3 flex items-center justify-between border-t border-gray-200">
            <div className="flex-1 flex justify-between sm:hidden">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Previous
              </button>
              <button
                onClick={() => setPage(p => Math.min(Math.ceil(totalCount / pageSize), p + 1))}
                disabled={page === Math.ceil(totalCount / pageSize)}
                className="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next
              </button>
            </div>
            <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
              <div>
                <p className="text-sm text-gray-700">
                  Showing <span className="font-medium">{((page - 1) * pageSize) + 1}</span> to{' '}
                  <span className="font-medium">{Math.min(page * pageSize, totalCount)}</span> of{' '}
                  <span className="font-medium">{totalCount}</span> wallets
                </p>
              </div>
              <div>
                <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px">
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                  </button>
                  <span className="relative inline-flex items-center px-4 py-2 border border-gray-300 bg-white text-sm font-medium text-gray-700">
                    Page {page} of {Math.ceil(totalCount / pageSize)}
                  </span>
                  <button
                    onClick={() => setPage(p => Math.min(Math.ceil(totalCount / pageSize), p + 1))}
                    disabled={page === Math.ceil(totalCount / pageSize)}
                    className="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
                    </svg>
                  </button>
                </nav>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Modals */}
      {showModal && modalType === 'deposit' && (
        <SimpleModal
          title="Deposit to Wallet"
          onClose={() => setShowModal(false)}
          onSave={handleDeposit}
          saveText="Process Deposit"
        >
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Amount</label>
              <input
                type="number"
                value={depositForm.amount}
                onChange={(e) => setDepositForm(prev => ({ ...prev, amount: e.target.value }))}
                placeholder="0.00"
                step="0.01"
                min="0.01"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Payment Method</label>
              <select
                value={depositForm.paymentMethod}
                onChange={(e) => setDepositForm(prev => ({ ...prev, paymentMethod: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="Cash">Cash</option>
                <option value="Card">Card</option>
                <option value="Bank Transfer">Bank Transfer</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <textarea
                value={depositForm.description}
                onChange={(e) => setDepositForm(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Optional description"
                rows="3"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </SimpleModal>
      )}

      {showModal && modalType === 'withdraw' && (
        <SimpleModal
          title="Withdraw from Wallet"
          onClose={() => setShowModal(false)}
          onSave={handleWithdraw}
          saveText="Process Withdrawal"
        >
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Amount</label>
              <input
                type="number"
                value={withdrawForm.amount}
                onChange={(e) => setWithdrawForm(prev => ({ ...prev, amount: e.target.value }))}
                placeholder="0.00"
                step="0.01"
                min="0.01"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Payment Method</label>
              <select
                value={withdrawForm.paymentMethod}
                onChange={(e) => setWithdrawForm(prev => ({ ...prev, paymentMethod: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="Cash">Cash</option>
                <option value="Card">Card</option>
                <option value="Bank Transfer">Bank Transfer</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <textarea
                value={withdrawForm.description}
                onChange={(e) => setWithdrawForm(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Optional description"
                rows="3"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </SimpleModal>
      )}

      {showModal && modalType === 'transfer' && (
        <SimpleModal
          title="Transfer Money Between Wallets"
          onClose={() => setShowModal(false)}
          onSave={handleTransfer}
          saveText="Process Transfer"
        >
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">From User</label>
              <select
                value={transferForm.fromUserId}
                onChange={(e) => setTransferForm(prev => ({ ...prev, fromUserId: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              >
                <option value="">Select source user</option>
                {users.map(user => (
                  <option key={user.userId} value={user.userId}>
                    {user.firstName} {user.lastName} (@{user.username}) - {fmtAmount(user.walletBalance)}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">To User</label>
              <select
                value={transferForm.toUserId}
                onChange={(e) => setTransferForm(prev => ({ ...prev, toUserId: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              >
                <option value="">Select destination user</option>
                {users.map(user => (
                  <option key={user.userId} value={user.userId}>
                    {user.firstName} {user.lastName} (@{user.username}) - {fmtAmount(user.walletBalance)}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Amount</label>
              <input
                type="number"
                value={transferForm.amount}
                onChange={(e) => setTransferForm(prev => ({ ...prev, amount: e.target.value }))}
                placeholder="0.00"
                step="0.01"
                min="0.01"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <textarea
                value={transferForm.description}
                onChange={(e) => setTransferForm(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Transfer description"
                rows="3"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
          </div>
        </SimpleModal>
      )}

      {showModal && modalType === 'view' && selectedWallet && (
        <SimpleModal
          title={`Wallet Details - ${selectedWallet.firstName} ${selectedWallet.lastName}`}
          onClose={() => setShowModal(false)}
          size="large"
        >
          <div className="space-y-6">
            {/* Wallet Info */}
            <div className="bg-gray-50 p-4 rounded-lg">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm font-medium text-gray-600">Current Balance</p>
                  <p className="text-2xl font-bold text-green-600">{fmtAmount(selectedWallet.walletBalance)}</p>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-600">Status</p>
                  <span 
                    className="inline-flex px-2 py-1 text-xs font-semibold rounded-full"
                    style={{ 
                      backgroundColor: `${getStatusColor(selectedWallet.isActive ? 'Active' : 'Inactive')}20`,
                      color: getStatusColor(selectedWallet.isActive ? 'Active' : 'Inactive')
                    }}
                  >
                    {selectedWallet.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              </div>
            </div>

            {/* Recent Transactions */}
            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-4">Recent Transactions</h3>
              
              {transactions.length === 0 ? (
                <p className="text-gray-500 text-center py-4">No transactions found</p>
              ) : (
                <div className="space-y-3">
                  {transactions.map((transaction, index) => (
                    <div key={index} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                      <div className="flex items-center space-x-3">
                        <div 
                          className="w-8 h-8 rounded-full flex items-center justify-center"
                          style={{ backgroundColor: `${getTransactionTypeColor(transaction.type)}20` }}
                        >
                          <svg className="w-4 h-4" style={{ color: getTransactionTypeColor(transaction.type) }} fill="currentColor" viewBox="0 0 20 20">
                            {transaction.type === 'Deposit' ? (
                              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-11a1 1 0 10-2 0v2H7a1 1 0 100 2h2v2a1 1 0 102 0v-2h2a1 1 0 100-2h-2V7z" clipRule="evenodd" />
                            ) : transaction.type === 'Withdrawal' ? (
                              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM7 9a1 1 0 000 2h6a1 1 0 100-2H7z" clipRule="evenodd" />
                            ) : (
                              <path fillRule="evenodd" d="M12 7a1 1 0 110 2h-3a1 1 0 01-.707-.293L6.586 7l1.707-1.707A1 1 0 019 5h3a1 1 0 110 2H9.414L8.707 7.707A1 1 0 019 8h3z" clipRule="evenodd" />
                            )}
                          </svg>
                        </div>
                        
                        <div>
                          <p className="text-sm font-medium text-gray-900">{transaction.type}</p>
                          <p className="text-xs text-gray-500">{transaction.description}</p>
                        </div>
                      </div>
                      
                      <div className="text-right">
                        <p className={`text-sm font-medium ${
                          transaction.type === 'Deposit' ? 'text-green-600' : 
                          transaction.type === 'Withdrawal' ? 'text-red-600' : 
                          'text-blue-600'
                        }`}>
                          {transaction.type === 'Deposit' ? '+' : transaction.type === 'Withdrawal' ? '-' : ''}
                          {fmtAmount(transaction.amount)}
                        </p>
                        <p className="text-xs text-gray-500">{fmtDate(transaction.transactionDate)}</p>
                      </div>
                    </div>
                  ))}
                  
                  {transactionTotal > 10 && (
                    <div className="text-center py-2">
                      <p className="text-sm text-gray-500">
                        Showing {Math.min(10, transactionTotal)} of {transactionTotal} transactions
                      </p>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </SimpleModal>
      )}

      {showModal && modalType === 'status' && (
        <SimpleModal
          title="Update Wallet Status"
          onClose={() => setShowModal(false)}
          onSave={handleStatusUpdate}
          saveText="Update Status"
        >
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Wallet Status</label>
              <select
                value={statusForm.isActive}
                onChange={(e) => setStatusForm(prev => ({ ...prev, isActive: e.target.value === 'true' }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
            
            <div className="bg-yellow-50 p-3 rounded-md">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="ml-3">
                  <p className="text-sm text-yellow-800">
                    Deactivating a wallet will prevent the user from making transactions until reactivated.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </SimpleModal>
      )}

      {confirmAction && (
        <ConfirmDialog
          title={confirmAction.title}
          message={confirmAction.message}
          onConfirm={() => {
            confirmAction.onConfirm();
            setConfirmAction(null);
          }}
          onCancel={() => setConfirmAction(null)}
        />
      )}
    </div>
  );
}
