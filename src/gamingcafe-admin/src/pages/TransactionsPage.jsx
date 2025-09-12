import React, { useEffect, useState, useCallback } from 'react';
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

  useEffect(() => { fetch(page); }, [fetch, page]);

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
    <div style={{ padding: 16 }}>
      <h2>Transactions</h2>
      <div style={{ marginBottom: 12 }}>
        {hasRole('Staff') || hasRole('Admin') ? (
          <button onClick={openCreate}>Create transaction</button>
        ) : (
          <span style={{ color: '#666' }}>No permission to create transactions</span>
        )}
      </div>

  {loading && <div style={{ marginTop: 8 }}><LoadingSpinner /></div>}

  <PagedList data={data} page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} loading={loading}
        renderRow={t => (
          <tr>
            <td style={{ padding: 8 }}>{t.transactionId}</td>
            <td style={{ padding: 8 }}>{t.username}</td>
            <td style={{ padding: 8 }}>{t.type}</td>
            <td style={{ padding: 8, textAlign: 'right' }}>{(t.amount ?? 0).toFixed(2)}</td>
            <td style={{ padding: 8 }}>{t.status}</td>
            <td style={{ padding: 8 }}>{new Date(t.transactionDate).toLocaleString()}</td>
            <td style={{ padding: 8, textAlign: 'right' }}>
              {hasRole('Staff') || hasRole('Admin') ? (
                <>
                  <button onClick={() => openEdit(t)} style={{ marginRight: 8 }}>Edit</button>
                  <button onClick={() => askDelete(t)}>Delete</button>
                </>
              ) : (
                <span style={{ color: '#666' }}>Restricted</span>
              )}
            </td>
          </tr>
        )}
      />

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
