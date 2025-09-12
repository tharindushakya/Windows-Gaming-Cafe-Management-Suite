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

  // Load transactions when page, pageSize, or filters change
  useEffect(() => {
    fetchTransactions();
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
    <div className="p-5">
      <div className="mb-6">
        <h1 className="mb-2 text-3xl font-bold">Payments & Transactions</h1>
        <p className="m-0 text-gray-500">Manage payment transactions, process refunds, and view financial data.</p>
      </div>

      {/* Statistics Cards */}
      {stats && (
        <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-4 mb-6">
          <div className="bg-white p-5 rounded-lg shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Total Transactions</div>
            <div className="text-2xl font-bold text-gray-900">{stats.totalTransactions}</div>
          </div>
          <div className="bg-white p-5 rounded-lg shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Total Revenue</div>
            <div className="text-2xl font-bold text-emerald-600">{fmtAmount(stats.totalRevenue)}</div>
          </div>
          <div className="bg-white p-5 rounded-lg shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Completed</div>
            <div className="text-2xl font-bold text-emerald-600">{stats.completedTransactions}</div>
          </div>
          <div className="bg-white p-5 rounded-lg shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Pending</div>
            <div className="text-2xl font-bold text-amber-500">{stats.pendingTransactions}</div>
          </div>
          <div className="bg-white p-5 rounded-lg shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Avg. Amount</div>
            <div className="text-2xl font-bold text-indigo-600">{fmtAmount(stats.averageTransactionAmount)}</div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="bg-white p-5 rounded-lg shadow-sm mb-5">
        <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-4 mb-4">
          <input
            type="text"
            name="search"
            placeholder="Search transactions..."
            value={filters.search}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
          />
          <select
            name="type"
            value={filters.type}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
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
            className="px-3 py-2 border border-gray-300 rounded-md"
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
            className="px-3 py-2 border border-gray-300 rounded-md"
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
        <div className="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-4 mb-4">
          <input
            type="number"
            name="minAmount"
            placeholder="Min Amount"
            value={filters.minAmount}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
          />
          <input
            type="number"
            name="maxAmount"
            placeholder="Max Amount"
            value={filters.maxAmount}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
          />
          <input
            type="date"
            name="startDate"
            value={filters.startDate}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
          />
          <input
            type="date"
            name="endDate"
            value={filters.endDate}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
          />
          <input
            type="number"
            name="userId"
            placeholder="User ID"
            value={filters.userId}
            onChange={handleFilterChange}
            className="px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div className="flex gap-3">
          <button
            onClick={clearFilters}
            className="px-4 py-2 bg-gray-500 text-white border-none rounded-md cursor-pointer"
          >
            Clear Filters
          </button>
          {(hasRole('Admin') || hasRole('Manager') || hasRole('Staff')) && (
            <button
              onClick={openCreateModal}
              className="px-4 py-2 bg-emerald-600 text-white border-none rounded-md cursor-pointer"
            >
              New Transaction
            </button>
          )}
        </div>
      </div>

      {/* Transactions Table */}
      <div className="bg-white rounded-lg shadow-sm overflow-hidden">
        {loading ? (
          <div className="p-10 text-center">
            <LoadingSpinner size={32} />
            <div className="mt-3 text-gray-500">Loading transactions...</div>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full border-collapse">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="p-3 text-left font-semibold text-gray-700">ID</th>
                    <th className="p-3 text-left font-semibold text-gray-700">User</th>
                    <th className="p-3 text-left font-semibold text-gray-700">Description</th>
                    <th className="p-3 text-right font-semibold text-gray-700">Amount</th>
                    <th className="p-3 text-center font-semibold text-gray-700">Type</th>
                    <th className="p-3 text-center font-semibold text-gray-700">Method</th>
                    <th className="p-3 text-center font-semibold text-gray-700">Status</th>
                    <th className="p-3 text-left font-semibold text-gray-700">Date</th>
                    <th className="p-3 text-center font-semibold text-gray-700">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {transactions.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="p-10 text-center text-gray-500">
                        No transactions found
                      </td>
                    </tr>
                  ) : (
                    transactions.map((transaction) => (
                      <tr key={transaction.transactionId} className="border-t border-gray-100">
                        <td className="p-3 text-gray-700">#{transaction.transactionId}</td>
                        <td className="p-3 text-gray-700">{transaction.username}</td>
                        <td className="p-3 text-gray-700">
                          <div className="max-w-[200px] overflow-hidden text-ellipsis whitespace-nowrap">
                            {transaction.description}
                          </div>
                        </td>
                        <td className="p-3 text-right text-gray-700 font-semibold">
                          {fmtAmount(transaction.amount)}
                        </td>
                        <td className="p-3 text-center">
                          <span style={{
                            padding: '4px 8px',
                            borderRadius: '12px',
                            fontSize: '12px',
                            fontWeight: '600',
                            background: getTypeColor(transaction.type) + '20',
                            color: getTypeColor(transaction.type)
                          }}>
                            {transaction.type}
                          </span>
                        </td>
                        <td className="p-3 text-center text-gray-500 text-sm">
                          {transaction.paymentMethod}
                        </td>
                        <td className="p-3 text-center">
                          <span style={{
                            padding: '4px 8px',
                            borderRadius: '12px',
                            fontSize: '12px',
                            fontWeight: '600',
                            background: getStatusColor(transaction.status) + '20',
                            color: getStatusColor(transaction.status)
                          }}>
                            {transaction.status}
                          </span>
                        </td>
                        <td className="p-3 text-gray-500 text-sm">
                          {fmtDate(transaction.createdAt)}
                        </td>
                        <td className="p-3 text-center">
                          <div className="flex gap-1 justify-center">
                            <button
                              onClick={() => openViewModal(transaction)}
                              className="px-2 py-1 text-xs bg-indigo-600 text-white border-none rounded cursor-pointer"
                            >
                              View
                            </button>
                            {(hasRole('Admin') || hasRole('Manager') || hasRole('Staff')) && (
                              <button
                                onClick={() => openStatusModal(transaction)}
                                className="px-2 py-1 text-xs bg-amber-500 text-white border-none rounded cursor-pointer"
                              >
                                Status
                              </button>
                            )}
                            {(hasRole('Admin') || hasRole('Manager')) && transaction.status === 'Completed' && (
                              <button
                                onClick={() => openRefundModal(transaction)}
                                className="px-2 py-1 text-xs bg-red-500 text-white border-none rounded cursor-pointer"
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
            <div style={{ padding: '16px' }}>
              <div style={{ display: 'grid', gap: '16px' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    User ID *
                  </label>
                  <input
                    type="number"
                    value={createForm.userId}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, userId: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                    placeholder="Enter user ID"
                  />
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Description *
                  </label>
                  <input
                    type="text"
                    value={createForm.description}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, description: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                    placeholder="Transaction description"
                  />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                  <div>
                    <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                      Amount *
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={createForm.amount}
                      onChange={(e) => setCreateForm(prev => ({ ...prev, amount: e.target.value }))}
                      style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                      placeholder="0.00"
                    />
                  </div>
                  <div>
                    <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                      Type
                    </label>
                    <select
                      value={createForm.type}
                      onChange={(e) => setCreateForm(prev => ({ ...prev, type: e.target.value }))}
                      style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                    >
                      <option value="GameTime">Game Time</option>
                      <option value="Product">Product</option>
                      <option value="WalletTopup">Wallet Top-up</option>
                      <option value="LoyaltyRedemption">Loyalty Redemption</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Payment Method
                  </label>
                  <select
                    value={createForm.paymentMethod}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, paymentMethod: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                  >
                    <option value="Cash">Cash</option>
                    <option value="CreditCard">Credit Card</option>
                    <option value="DebitCard">Debit Card</option>
                    <option value="Wallet">Wallet</option>
                    <option value="LoyaltyPoints">Loyalty Points</option>
                    <option value="BankTransfer">Bank Transfer</option>
                  </select>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Payment Reference
                  </label>
                  <input
                    type="text"
                    value={createForm.paymentReference}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, paymentReference: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                    placeholder="Optional payment reference"
                  />
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Notes
                  </label>
                  <textarea
                    value={createForm.notes}
                    onChange={(e) => setCreateForm(prev => ({ ...prev, notes: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px', minHeight: '80px' }}
                    placeholder="Optional notes"
                  />
                </div>
                <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
                  <button
                    onClick={closeModal}
                    style={{ padding: '8px 16px', background: '#6b7280', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
                  >
                    Cancel
                  </button>
                  <button
                    onClick={createTransaction}
                    style={{ padding: '8px 16px', background: '#10b981', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
                  >
                    Create Transaction
                  </button>
                </div>
              </div>
            </div>
          )}

          {modalType === 'refund' && selectedTransaction && (
            <div style={{ padding: '16px' }}>
              <div style={{ marginBottom: '16px', padding: '12px', background: '#fef3c7', borderRadius: '6px', border: '1px solid #f59e0b' }}>
                <div style={{ fontWeight: '600', color: '#92400e' }}>Refunding Transaction #{selectedTransaction.transactionId}</div>
                <div style={{ fontSize: '14px', color: '#92400e', marginTop: '4px' }}>
                  Original Amount: {fmtAmount(selectedTransaction.amount)}
                </div>
              </div>
              <div style={{ display: 'grid', gap: '16px' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Refund Amount *
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    max={selectedTransaction.amount}
                    value={refundForm.refundAmount}
                    onChange={(e) => setRefundForm(prev => ({ ...prev, refundAmount: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Reason *
                  </label>
                  <textarea
                    value={refundForm.reason}
                    onChange={(e) => setRefundForm(prev => ({ ...prev, reason: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px', minHeight: '80px' }}
                    placeholder="Reason for refund"
                  />
                </div>
                <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
                  <button
                    onClick={closeModal}
                    style={{ padding: '8px 16px', background: '#6b7280', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
                  >
                    Cancel
                  </button>
                  <button
                    onClick={processRefund}
                    style={{ padding: '8px 16px', background: '#ef4444', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
                  >
                    Process Refund
                  </button>
                </div>
              </div>
            </div>
          )}

          {modalType === 'status' && selectedTransaction && (
            <div style={{ padding: '16px' }}>
              <div style={{ marginBottom: '16px', padding: '12px', background: '#f0f9ff', borderRadius: '6px', border: '1px solid #0ea5e9' }}>
                <div style={{ fontWeight: '600', color: '#0c4a6e' }}>Update Transaction #{selectedTransaction.transactionId}</div>
                <div style={{ fontSize: '14px', color: '#0c4a6e', marginTop: '4px' }}>
                  Current Status: {selectedTransaction.status}
                </div>
              </div>
              <div style={{ display: 'grid', gap: '16px' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    New Status *
                  </label>
                  <select
                    value={statusForm.status}
                    onChange={(e) => setStatusForm(prev => ({ ...prev, status: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px' }}
                  >
                    <option value="Pending">Pending</option>
                    <option value="Completed">Completed</option>
                    <option value="Failed">Failed</option>
                    <option value="Cancelled">Cancelled</option>
                  </select>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', color: '#374151', marginBottom: '4px' }}>
                    Notes
                  </label>
                  <textarea
                    value={statusForm.notes}
                    onChange={(e) => setStatusForm(prev => ({ ...prev, notes: e.target.value }))}
                    style={{ width: '100%', padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px', minHeight: '80px' }}
                    placeholder="Optional notes about status change"
                  />
                </div>
                <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
                  <button
                    onClick={closeModal}
                    style={{ padding: '8px 16px', background: '#6b7280', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
                  >
                    Cancel
                  </button>
                  <button
                    onClick={updateStatus}
                    style={{ padding: '8px 16px', background: '#0ea5e9', color: '#fff', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
                  >
                    Update Status
                  </button>
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
  );
}
