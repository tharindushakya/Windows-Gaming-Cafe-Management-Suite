import React, { useEffect, useState, useCallback, useRef } from 'react';
import api from '../api';
import SimpleModal from '../components/SimpleModal';
import TransactionForm from '../components/TransactionForm';
import PagedList from '../components/PagedList';
import ConfirmDialog from '../components/ConfirmDialog';
import { useToast } from '../components/ToastProvider';
import LoadingSpinner from '../components/LoadingSpinner';
import { useHasRole } from '../utils/security';

export default function TransactionsPage() {
  const [data, setData] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const toast = useToast();
  const hasRole = useHasRole();
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [confirm, setConfirm] = useState(null);

  const fetch = useCallback(async (p = 1) => {
    setLoading(true);
    try {
      const res = await api.get(`/api/v1.0/transactions?page=${p}&pageSize=${pageSize}`);
      const paged = res || res?.data || res?.Data || res;
      setData(paged?.data ?? paged?.Data ?? paged ?? []);
      setPage(paged?.page ?? p);
      setTotalCount(paged?.totalCount ?? paged?.TotalCount ?? (paged?.data?.length ?? paged?.length ?? 0));
    } catch (err) {
  toast.push(err?.data?.message || err.message || 'Failed to load transactions', 'error');
    } finally { setLoading(false); }
  }, [pageSize, toast]);

  // Debounce fetch to avoid bursts that can trigger server rate limits
  const fetchTimeoutRef = useRef(null);
  useEffect(() => {
    if (fetchTimeoutRef.current) clearTimeout(fetchTimeoutRef.current);
    fetchTimeoutRef.current = setTimeout(() => {
      fetch(page);
    }, 250);

    return () => {
      if (fetchTimeoutRef.current) clearTimeout(fetchTimeoutRef.current);
    };
  }, [fetch, page]);

  function openCreate() { setEditing(null); setShowModal(true); }
  function openEdit(t) { setEditing(t); setShowModal(true); }
  function askDelete(t) { setConfirm(t); }

  async function handleSave(payload) {
    try {
      if (editing && (editing.transactionId || editing.transactionId === 0)) {
        await api.put(`/api/v1.0/transactions/${editing.transactionId}`, payload);
      } else {
        await api.post('/api/v1.0/transactions', payload);
      }
      setShowModal(false);
      fetch(page);
    } catch (err) {
  toast.push(err?.data?.message || err.message || 'Save failed', 'error');
    }
  }

  async function doDelete(t) {
    try {
      await api.del(`/api/v1.0/transactions/${t.transactionId}`);
      setConfirm(null);
      fetch(page);
    } catch (err) {
  toast.push(err?.data?.message || err.message || 'Delete failed', 'error');
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">Transactions</h1>
            <p className="text-gray-600">Manage financial transactions and payment records</p>
          </div>
          <div className="mt-4 sm:mt-0">
            {hasRole('Staff') || hasRole('Admin') ? (
              <button 
                onClick={openCreate}
                className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg shadow-sm transition-colors duration-200 gap-2"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
                Create Transaction
              </button>
            ) : (
              <div className="flex items-center px-4 py-2 bg-gray-100 text-gray-500 text-sm font-medium rounded-lg gap-2">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
                No permission to create transactions
              </div>
            )}
          </div>
        </div>

        {/* Loading state */}
        {loading && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8">
            <div className="flex items-center justify-center">
              <LoadingSpinner />
            </div>
          </div>
        )}

        {/* Transactions table */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <PagedList 
            data={data} 
            page={page} 
            pageSize={pageSize} 
            totalCount={totalCount} 
            onPageChange={setPage} 
            loading={loading}
            emptyTitle="No transactions found"
            emptySubtitle="Create your first transaction to get started"
            renderRow={t => (
              <tr className="hover:bg-gray-50 transition-colors duration-200">
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  #{t.transactionId}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {t.username}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    t.type === 'Payment' ? 'bg-emerald-100 text-emerald-800' :
                    t.type === 'Refund' ? 'bg-red-100 text-red-800' :
                    'bg-blue-100 text-blue-800'
                  }`}>
                    {t.type}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-right text-gray-900">
                  ${(t.amount ?? 0).toFixed(2)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    t.status === 'Completed' ? 'bg-emerald-100 text-emerald-800' :
                    t.status === 'Pending' ? 'bg-amber-100 text-amber-800' :
                    t.status === 'Failed' ? 'bg-red-100 text-red-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>
                    {t.status}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {new Date(t.transactionDate).toLocaleString()}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                  {hasRole('Staff') || hasRole('Admin') ? (
                    <div className="flex items-center justify-end gap-2">
                      <button 
                        onClick={() => openEdit(t)}
                        className="text-indigo-600 hover:text-indigo-700 font-medium"
                      >
                        Edit
                      </button>
                      <button 
                        onClick={() => askDelete(t)}
                        className="text-red-600 hover:text-red-700 font-medium"
                      >
                        Delete
                      </button>
                    </div>
                  ) : (
                    <span className="text-gray-400 text-sm">Restricted</span>
                  )}
                </td>
              </tr>
            )}
          />
        </div>
      </div>

      {showModal && (
        <SimpleModal title={editing ? 'Edit transaction' : 'Create transaction'} onClose={() => setShowModal(false)}>
          <TransactionForm initial={editing ?? {}} onCancel={() => setShowModal(false)} onSubmit={handleSave} />
        </SimpleModal>
      )}

      {confirm && (
        <SimpleModal title="Confirm delete" onClose={() => setConfirm(null)}>
          <ConfirmDialog message={`Delete transaction ${confirm.transactionId}?`} onConfirm={() => doDelete(confirm)} onCancel={() => setConfirm(null)} />
        </SimpleModal>
      )}
    </div>
  );
}
