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
    case 'completed': return '#10b981';
    case 'pending': return '#f59e0b';
    case 'failed': return '#ef4444';
    case 'cancelled': return '#6b7280';
    case 'refunded': return '#8b5cf6';
    default: return '#6b7280';
  }
}

function getTypeColor(type) {
  switch (type?.toLowerCase()) {
    case 'gametime': return '#0ea5e9';
    case 'product': return '#10b981';
    case 'wallettopup': return '#8b5cf6';
    case 'refund': return '#ef4444';
    case 'loyaltyredemption': return '#f59e0b';
    default: return '#6b7280';
  }
}

export default function Payments() {
  // Data state
  const [transactions, setTransactions] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [stats, setStats] = useState(null);

  // Filter state
  const [filters, setFilters] = useState({
    search: '',
    type: '',
    status: '',
    paymentMethod: '',
    minAmount: '',
    maxAmount: '',
    startDate: '',
    endDate: '',
    userId: ''
  });

  // Debounced search state
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const searchTimeoutRef = useRef(null);
  // Debounce fetch requests to avoid bursts (throttle network calls)
  const fetchTimeoutRef = useRef(null);

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [modalType, setModalType] = useState(''); // 'create', 'refund', 'status', 'view'
  const [selectedTransaction, setSelectedTransaction] = useState(null);
  const [confirmAction, setConfirmAction] = useState(null);

  // Form state
  const [refundForm, setRefundForm] = useState({ refundAmount: '', reason: '' });
  const [statusForm, setStatusForm] = useState({ status: '', notes: '' });
  const [createForm, setCreateForm] = useState({
    userId: '',
    description: '',
    amount: '',
    type: 'GameTime',
    paymentMethod: 'Cash',
    paymentReference: '',
    notes: ''
  });

  const toast = useToast();
  const hasRole = useHasRole();

  // Fetch transactions with filters - memoized to prevent infinite loops
  const fetchTransactions = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
        sortDescending: 'true'
      });

      // Add filters - use debouncedSearch for search to prevent too many requests
      if (debouncedSearch) params.append('search', debouncedSearch);
      if (filters.type) params.append('type', filters.type);
      if (filters.status) params.append('status', filters.status);
      if (filters.paymentMethod) params.append('paymentMethod', filters.paymentMethod);
      if (filters.minAmount) params.append('minAmount', filters.minAmount);
      if (filters.maxAmount) params.append('maxAmount', filters.maxAmount);
      if (filters.startDate) params.append('startDate', filters.startDate);
      if (filters.endDate) params.append('endDate', filters.endDate);
      if (filters.userId) params.append('userId', filters.userId);

      const resp = await api.get(`/api/v1.0/transactions?${params}`);
      setTransactions(resp?.data || []);
      setTotalCount(resp?.totalCount || 0);
    } catch (err) {
      toast.push(err?.message || 'Failed to load transactions', 'error');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, debouncedSearch, filters.type, filters.status, filters.paymentMethod, 
      filters.minAmount, filters.maxAmount, filters.startDate, filters.endDate, filters.userId, toast]);

  // Fetch statistics - memoized to prevent infinite loops
  const fetchStats = useCallback(async () => {
    try {
      const params = new URLSearchParams();
      if (filters.startDate) params.append('startDate', filters.startDate);
      if (filters.endDate) params.append('endDate', filters.endDate);

      const resp = await api.get(`/api/v1.0/transactions/stats?${params}`);
      setStats(resp);
    } catch (err) {
      console.warn('Failed to load transaction stats:', err);
    }
  }, [filters.startDate, filters.endDate]);

  // Load transactions when page, pageSize, or filters change (debounced)
  useEffect(() => {
    if (fetchTimeoutRef.current) {
      clearTimeout(fetchTimeoutRef.current);
    }
    // small delay to collapse rapid changes (typing, filter toggles)
    fetchTimeoutRef.current = setTimeout(() => {
      fetchTransactions();
    }, 250);

    return () => {
      if (fetchTimeoutRef.current) {
        clearTimeout(fetchTimeoutRef.current);
      }
    };
  }, [fetchTransactions]);

  // Load stats when relevant filters change
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

  // Reset page when filters change (except first load)
  useEffect(() => {
    if (page !== 1) {
      setPage(1);
    }
  }, [debouncedSearch, filters.type, filters.status, filters.paymentMethod, 
      filters.minAmount, filters.maxAmount, filters.startDate, filters.endDate, filters.userId]);

  // Handle filter changes
  function handleFilterChange(e) {
    const { name, value } = e.target;
    setFilters(prev => ({ ...prev, [name]: value }));
    // Don't set page here, let the useEffect handle it
  }

  // Clear all filters
  function clearFilters() {
    setFilters({
      search: '', type: '', status: '', paymentMethod: '',
      minAmount: '', maxAmount: '', startDate: '', endDate: '', userId: ''
    });
    setDebouncedSearch('');
    setPage(1);
  }

  // Modal handlers
  function openCreateModal() {
    setModalType('create');
    setCreateForm({
      userId: '', description: '', amount: '', type: 'GameTime',
      paymentMethod: 'Cash', paymentReference: '', notes: ''
    });
    setShowModal(true);
  }

  function openRefundModal(transaction) {
    setModalType('refund');
    setSelectedTransaction(transaction);
    setRefundForm({ refundAmount: transaction.amount.toString(), reason: '' });
    setShowModal(true);
  }

  function openStatusModal(transaction) {
    setModalType('status');
    setSelectedTransaction(transaction);
    setStatusForm({ status: transaction.status, notes: '' });
    setShowModal(true);
  }

  function openViewModal(transaction) {
    setModalType('view');
    setSelectedTransaction(transaction);
    setShowModal(true);
  }

  function closeModal() {
    setShowModal(false);
    setModalType('');
    setSelectedTransaction(null);
  }

  // Transaction operations
  async function createTransaction() {
    try {
      if (!createForm.userId || !createForm.description || !createForm.amount) {
        toast.push('Please fill in all required fields', 'error');
        return;
      }

      const payload = {
        userId: parseInt(createForm.userId),
        description: createForm.description,
        amount: parseFloat(createForm.amount),
        type: createForm.type,
        paymentMethod: createForm.paymentMethod,
        paymentReference: createForm.paymentReference || null,
        notes: createForm.notes || null
      };

      await api.post('/api/v1.0/transactions', payload);
      toast.push('Transaction created successfully', 'success');
      closeModal();
      fetchTransactions();
    } catch (err) {
      toast.push(err?.message || 'Failed to create transaction', 'error');
    }
  }

  async function processRefund() {
    try {
      if (!refundForm.refundAmount || !refundForm.reason) {
        toast.push('Please enter refund amount and reason', 'error');
        return;
      }

      const payload = {
        refundAmount: parseFloat(refundForm.refundAmount),
        reason: refundForm.reason
      };

      await api.post(`/api/v1.0/transactions/${selectedTransaction.transactionId}/refund`, payload);
      toast.push('Refund processed successfully', 'success');
      closeModal();
      fetchTransactions();
    } catch (err) {
      toast.push(err?.message || 'Failed to process refund', 'error');
    }
  }

  async function updateStatus() {
    try {
      if (!statusForm.status) {
        toast.push('Please select a status', 'error');
        return;
      }

      const payload = {
        status: statusForm.status,
        notes: statusForm.notes || null
      };

      await api.patch(`/api/v1.0/transactions/${selectedTransaction.transactionId}/status`, payload);
      toast.push('Transaction status updated successfully', 'success');
      closeModal();
      fetchTransactions();
    } catch (err) {
      toast.push(err?.message || 'Failed to update status', 'error');
    }
  }

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Payments & Transactions</h1>
          <p className="text-gray-600">Manage payment transactions, process refunds, and view financial data.</p>
        </div>

        {/* Statistics Cards */}
        {stats && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-6 mb-8">
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-200">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-sm font-medium text-gray-600">Total Transactions</div>
                  <div className="text-2xl font-bold text-gray-900 mt-2">{stats.totalTransactions}</div>
                </div>
                <div className="p-3 bg-indigo-100 rounded-lg">
                  <svg className="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-200">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-sm font-medium text-gray-600">Total Revenue</div>
                  <div className="text-2xl font-bold text-emerald-600 mt-2">{fmtAmount(stats.totalRevenue)}</div>
                </div>
                <div className="p-3 bg-emerald-100 rounded-lg">
                  <svg className="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-200">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-sm font-medium text-gray-600">Completed</div>
                  <div className="text-2xl font-bold text-emerald-600 mt-2">{stats.completedTransactions}</div>
                </div>
                <div className="p-3 bg-emerald-100 rounded-lg">
                  <svg className="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-200">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-sm font-medium text-gray-600">Pending</div>
                  <div className="text-2xl font-bold text-amber-500 mt-2">{stats.pendingTransactions}</div>
                </div>
                <div className="p-3 bg-amber-100 rounded-lg">
                  <svg className="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-200">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-sm font-medium text-gray-600">Avg. Amount</div>
                  <div className="text-2xl font-bold text-indigo-600 mt-2">{fmtAmount(stats.averageTransactionAmount)}</div>
                </div>
                <div className="p-3 bg-indigo-100 rounded-lg">
                  <svg className="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z" />
                  </svg>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Filters */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Filter Transactions</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
            <input
              type="text"
              name="search"
              placeholder="Search transactions..."
              value={filters.search}
              onChange={handleFilterChange}
              className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            />
            <select
              name="type"
              value={filters.type}
              onChange={handleFilterChange}
              className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="">All Types</option>
              <option value="GameTime">Game Time</option>
              <option value="Product">Product</option>
              <option value="WalletTopup">Wallet Top-up</option>
              <option value="Refund">Refund</option>
              <option value="LoyaltyRedemption">Loyalty Redemption</option>
            </select>
            <select
              name="status"
              value={filters.status}
              onChange={handleFilterChange}
              className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="Cancelled">Cancelled</option>
            <option value="Refunded">Refunded</option>
          </select>
          <select
            name="paymentMethod"
            value={filters.paymentMethod}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          >
            <option value="">All Payment Methods</option>
            <option value="Cash">Cash</option>
            <option value="CreditCard">Credit Card</option>
            <option value="DebitCard">Debit Card</option>
            <option value="Wallet">Wallet</option>
            <option value="LoyaltyPoints">Loyalty Points</option>
            <option value="BankTransfer">Bank Transfer</option>
          </select>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-4 mb-4">
          <input
            type="number"
            name="minAmount"
            placeholder="Min Amount"
            value={filters.minAmount}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          />
          <input
            type="number"
            name="maxAmount"
            placeholder="Max Amount"
            value={filters.maxAmount}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          />
          <input
            type="date"
            name="startDate"
            value={filters.startDate}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          />
          <input
            type="date"
            name="endDate"
            value={filters.endDate}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          />
          <input
            type="number"
            name="userId"
            placeholder="User ID"
            value={filters.userId}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          />
        </div>
        <div className="flex gap-3">
          <button
            onClick={clearFilters}
            className="inline-flex items-center px-4 py-2 bg-gray-500 hover:bg-gray-600 text-white text-sm font-medium rounded-lg transition-colors duration-200"
          >
            <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            Clear Filters
          </button>
          {(hasRole('Admin') || hasRole('Manager') || hasRole('Staff')) && (
            <button
              onClick={openCreateModal}
              className="inline-flex items-center px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-medium rounded-lg transition-colors duration-200"
            >
              <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              New Transaction
            </button>
          )}
        </div>
      </div>

      {/* Transactions Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <div className="p-12 text-center">
            <LoadingSpinner size={32} />
            <div className="mt-4 text-gray-500 text-lg">Loading transactions...</div>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Method</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {transactions.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="px-6 py-12 text-center text-gray-500">
                        <div className="flex flex-col items-center">
                          <svg className="w-12 h-12 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                          </svg>
                          <p className="text-lg font-medium text-gray-900 mb-1">No transactions found</p>
                          <p className="text-gray-500">Try adjusting your filters or create a new transaction</p>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    transactions.map((transaction) => (
                      <tr key={transaction.transactionId} className="hover:bg-gray-50 transition-colors duration-200">
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          #{transaction.transactionId}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {transaction.username}
                        </td>
                        <td className="px-6 py-4 text-sm text-gray-900">
                          <div className="max-w-xs overflow-hidden text-ellipsis">
                            {transaction.description}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-right text-gray-900 font-semibold">
                          {fmtAmount(transaction.amount)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-center">
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            transaction.type === 'GameTime' ? 'bg-blue-100 text-blue-800' :
                            transaction.type === 'Product' ? 'bg-emerald-100 text-emerald-800' :
                            transaction.type === 'WalletTopup' ? 'bg-purple-100 text-purple-800' :
                            transaction.type === 'Refund' ? 'bg-red-100 text-red-800' :
                            transaction.type === 'LoyaltyRedemption' ? 'bg-amber-100 text-amber-800' :
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {transaction.type}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-center text-sm text-gray-500">
                          {transaction.paymentMethod}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-center">
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            transaction.status === 'Completed' ? 'bg-emerald-100 text-emerald-800' :
                            transaction.status === 'Pending' ? 'bg-amber-100 text-amber-800' :
                            transaction.status === 'Failed' ? 'bg-red-100 text-red-800' :
                            transaction.status === 'Cancelled' ? 'bg-gray-100 text-gray-800' :
                            transaction.status === 'Refunded' ? 'bg-purple-100 text-purple-800' :
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {transaction.status}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {fmtDate(transaction.createdAt)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-center">
                          <div className="flex items-center justify-center gap-1">
                            <button
                              onClick={() => openViewModal(transaction)}
                              className="inline-flex items-center px-2 py-1 text-xs font-medium bg-indigo-100 text-indigo-700 rounded-md hover:bg-indigo-200 transition-colors duration-200"
                            >
                              View
                            </button>
                            {(hasRole('Admin') || hasRole('Manager') || hasRole('Staff')) && (
                              <button
                                onClick={() => openStatusModal(transaction)}
                                className="inline-flex items-center px-2 py-1 text-xs font-medium bg-amber-100 text-amber-700 rounded-md hover:bg-amber-200 transition-colors duration-200"
                              >
                                Status
                              </button>
                            )}
                            {(hasRole('Admin') || hasRole('Manager')) && transaction.status === 'Completed' && (
                              <button
                                onClick={() => openRefundModal(transaction)}
                                className="inline-flex items-center px-2 py-1 text-xs font-medium bg-red-100 text-red-700 rounded-md hover:bg-red-200 transition-colors duration-200"
                              >
                                Refund
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalCount > 0 && (
              <div className="p-4 border-t border-gray-100 flex justify-between items-center">
                <div className="text-gray-500 text-sm">
                  Showing {Math.min((page - 1) * pageSize + 1, totalCount)} to {Math.min(page * pageSize, totalCount)} of {totalCount} transactions
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className={`px-3 py-2 border border-gray-300 rounded-md ${
                      page === 1 
                        ? 'bg-gray-100 cursor-not-allowed text-gray-400' 
                        : 'bg-white cursor-pointer text-gray-700'
                    }`}
                  >
                    Previous
                  </button>
                  <span className="px-3 py-2 text-gray-700">
                    Page {page} of {totalPages}
                  </span>
                  <button
                    onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages}
                    className={`px-3 py-2 border border-gray-300 rounded-md ${
                      page === totalPages 
                        ? 'bg-gray-100 cursor-not-allowed text-gray-400' 
                        : 'bg-white cursor-pointer text-gray-700'
                    }`}
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Modals */}
      {showModal && (
        <SimpleModal
          title={
            modalType === 'create' ? 'Create Transaction' :
            modalType === 'refund' ? 'Process Refund' :
            modalType === 'status' ? 'Update Status' :
            'Transaction Details'
          }
          onClose={closeModal}
        >
          {modalType === 'view' && selectedTransaction && (
            <div className="p-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    Transaction ID
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    #{selectedTransaction.transactionId}
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    User
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    {selectedTransaction.username} (ID: {selectedTransaction.userId})
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    Amount
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    {fmtAmount(selectedTransaction.amount)}
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    Type
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    {selectedTransaction.type}
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    Payment Method
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    {selectedTransaction.paymentMethod}
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    Status
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    <span style={{
                      padding: '4px 8px',
                      borderRadius: '12px',
                      fontSize: '12px',
                      fontWeight: '600',
                      background: getStatusColor(selectedTransaction.status) + '20',
                      color: getStatusColor(selectedTransaction.status)
                    }}>
                      {selectedTransaction.status}
                    </span>
                  </div>
                </div>
                <div className="col-span-2">
                  <label className="block text-sm font-semibold text-gray-700 mb-1">
                    Description
                  </label>
                  <div className="p-2 bg-gray-50 rounded">
                    {selectedTransaction.description}
                  </div>
                </div>
                {selectedTransaction.paymentReference && (
                  <div className="col-span-2">
                    <label className="block text-sm font-semibold text-gray-700 mb-1">
                      Payment Reference
                    </label>
                    <div className="p-2 bg-gray-50 rounded">
                      {selectedTransaction.paymentReference}
                    </div>
                  </div>
                )}
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Created
                  </label>
                  <div style={{ padding: '8px', background: '#f9fafb', borderRadius: '4px' }}>
                    {fmtDate(selectedTransaction.createdAt)}
                  </div>
                </div>
                {selectedTransaction.processedAt && (
                  <div>
                    <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                      Processed
                    </label>
                    <div style={{ padding: '8px', background: '#f9fafb', borderRadius: '4px' }}>
                      {fmtDate(selectedTransaction.processedAt)}
                    </div>
                  </div>
                )}
                {selectedTransaction.notes && (
                  <div style={{ gridColumn: '1 / -1' }}>
                    <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                      Notes
                    </label>
                    <div style={{ padding: '8px', background: '#f9fafb', borderRadius: '4px' }}>
                      {selectedTransaction.notes}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {modalType === 'create' && (
            <div className="p-4">
              <div className="grid gap-4">
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">User ID *</label>
                  <input type="number" value={createForm.userId} onChange={(e) => setCreateForm(prev => ({ ...prev, userId: e.target.value }))} placeholder="Enter user ID" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Description *</label>
                  <input type="text" value={createForm.description} onChange={(e) => setCreateForm(prev => ({ ...prev, description: e.target.value }))} placeholder="Transaction description" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">Amount *</label>
                    <input type="number" step="0.01" min="0" value={createForm.amount} onChange={(e) => setCreateForm(prev => ({ ...prev, amount: e.target.value }))} placeholder="0.00" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">Type</label>
                    <select value={createForm.type} onChange={(e) => setCreateForm(prev => ({ ...prev, type: e.target.value }))} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="GameTime">Game Time</option>
                      <option value="Product">Product</option>
                      <option value="WalletTopup">Wallet Top-up</option>
                      <option value="LoyaltyRedemption">Loyalty Redemption</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Payment Method</label>
                  <select value={createForm.paymentMethod} onChange={(e) => setCreateForm(prev => ({ ...prev, paymentMethod: e.target.value }))} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                    <option value="Cash">Cash</option>
                    <option value="CreditCard">Credit Card</option>
                    <option value="DebitCard">Debit Card</option>
                    <option value="Wallet">Wallet</option>
                    <option value="LoyaltyPoints">Loyalty Points</option>
                    <option value="BankTransfer">Bank Transfer</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Payment Reference</label>
                  <input type="text" value={createForm.paymentReference} onChange={(e) => setCreateForm(prev => ({ ...prev, paymentReference: e.target.value }))} placeholder="Optional payment reference" className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Notes</label>
                  <textarea value={createForm.notes} onChange={(e) => setCreateForm(prev => ({ ...prev, notes: e.target.value }))} placeholder="Optional notes" className="w-full px-3 py-2 border border-gray-300 rounded-md min-h-[80px]" />
                </div>
                <div className="flex gap-3 justify-end">
                  <button onClick={closeModal} className="px-4 py-2 bg-gray-500 text-white rounded-md">Cancel</button>
                  <button onClick={createTransaction} className="px-4 py-2 bg-emerald-600 text-white rounded-md">Create Transaction</button>
                </div>
              </div>
            </div>
          )}

          {modalType === 'refund' && selectedTransaction && (
            <div className="p-4">
              <div className="mb-4 p-3 rounded border border-amber-300 bg-amber-50">
                <div className="font-semibold text-amber-800">Refunding Transaction #{selectedTransaction.transactionId}</div>
                <div className="text-sm text-amber-800 mt-1">Original Amount: {fmtAmount(selectedTransaction.amount)}</div>
              </div>
              <div className="grid gap-4">
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Refund Amount *</label>
                  <input type="number" step="0.01" min="0" max={selectedTransaction.amount} value={refundForm.refundAmount} onChange={(e) => setRefundForm(prev => ({ ...prev, refundAmount: e.target.value }))} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Reason *</label>
                  <textarea value={refundForm.reason} onChange={(e) => setRefundForm(prev => ({ ...prev, reason: e.target.value }))} placeholder="Reason for refund" className="w-full px-3 py-2 border border-gray-300 rounded-md min-h-[80px]" />
                </div>
                <div className="flex gap-3 justify-end">
                  <button onClick={closeModal} className="px-4 py-2 bg-gray-500 text-white rounded-md">Cancel</button>
                  <button onClick={processRefund} className="px-4 py-2 bg-red-500 text-white rounded-md">Process Refund</button>
                </div>
              </div>
            </div>
          )}

          {modalType === 'status' && selectedTransaction && (
            <div className="p-4">
              <div className="mb-4 p-3 rounded border border-sky-200 bg-sky-50">
                <div className="font-semibold text-sky-800">Update Transaction #{selectedTransaction.transactionId}</div>
                <div className="text-sm text-sky-800 mt-1">Current Status: {selectedTransaction.status}</div>
              </div>
              <div className="grid gap-4">
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">New Status *</label>
                  <select value={statusForm.status} onChange={(e) => setStatusForm(prev => ({ ...prev, status: e.target.value }))} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                    <option value="Pending">Pending</option>
                    <option value="Completed">Completed</option>
                    <option value="Failed">Failed</option>
                    <option value="Cancelled">Cancelled</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-gray-700 mb-1">Notes</label>
                  <textarea value={statusForm.notes} onChange={(e) => setStatusForm(prev => ({ ...prev, notes: e.target.value }))} placeholder="Optional notes about status change" className="w-full px-3 py-2 border border-gray-300 rounded-md min-h-[80px]" />
                </div>
                <div className="flex gap-3 justify-end">
                  <button onClick={closeModal} className="px-4 py-2 bg-gray-500 text-white rounded-md">Cancel</button>
                  <button onClick={updateStatus} className="px-4 py-2 bg-sky-500 text-white rounded-md">Update Status</button>
                </div>
              </div>
            </div>
          )}
        </SimpleModal>
      )}

      {/* Confirm Dialog */}
      {confirmAction && (
        <SimpleModal title="Confirm Action" onClose={() => setConfirmAction(null)}>
          <ConfirmDialog
            message={confirmAction.message}
            onConfirm={confirmAction.onConfirm}
            onCancel={() => setConfirmAction(null)}
          />
        </SimpleModal>
      )}
      </div>
    </div>
  );
}
