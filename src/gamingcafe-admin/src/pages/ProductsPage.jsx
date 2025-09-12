import React, { useEffect, useState, useCallback } from 'react';
import api from '../api';
import SimpleModal from '../components/SimpleModal';
import ProductForm from '../components/ProductForm';
import PagedList from '../components/PagedList';
import ConfirmDialog from '../components/ConfirmDialog';
import { useToast } from '../components/ToastProvider';
import LoadingSpinner from '../components/LoadingSpinner';
import { useHasRole } from '../utils/security';

export default function ProductsPage() {
  const [data, setData] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const toast = useToast();
  const hasRole = useHasRole();
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [formErrors, setFormErrors] = useState({});
  const [confirm, setConfirm] = useState(null);

  const fetch = useCallback(async (p = 1) => {
    setLoading(true);
    try {
      const res = await api.get(`/api/v1.0/products?page=${p}&pageSize=${pageSize}`);
      const paged = res || res?.data || res?.Data || res;
      setData(paged?.data ?? paged?.Data ?? paged ?? []);
      setPage(paged?.page ?? p);
      setTotalCount(paged?.totalCount ?? paged?.TotalCount ?? (paged?.data?.length ?? paged?.length ?? 0));
    } catch (err) {
      toast.push(err?.data?.message || err.message || 'Failed to load products', 'error');
    } finally { setLoading(false); }
  }, [pageSize, toast]);

  useEffect(() => { fetch(page); }, [fetch, page]);

  function openCreate() { setEditing(null); setShowModal(true); }
  function openEdit(p) { setEditing(p); setShowModal(true); }
  function askDelete(p) { setConfirm(p); }

  async function handleSave(payload) {
    try {
      if (editing && (editing.productId || editing.productId === 0)) {
        await api.put(`/api/v1.0/products/${editing.productId}`, payload);
      } else {
        await api.post('/api/v1.0/products', payload);
      }
      setShowModal(false);
      fetch(page);
    } catch (err) {
  toast.push(err?.data?.message || err.message || 'Save failed', 'error');
    }
  }

  async function doDelete(p) {
    try {
      await api.del(`/api/v1.0/products/${p.productId}`);
      setConfirm(null);
      fetch(page);
    } catch (err) {
  toast.push(err?.data?.message || err.message || 'Delete failed', 'error');
    }
  }

  return (
    <div style={{ padding: 16 }}>
      <h2>Products</h2>
      <div style={{ marginBottom: 12 }}>
        {hasRole('Staff') || hasRole('Admin') ? (
          <button onClick={openCreate}>Create product</button>
        ) : (
          <span style={{ color: '#666' }}>You don't have permission to create products</span>
        )}
      </div>

  <PagedList data={data} page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} loading={loading}
        renderRow={p => (
          <tr>
            <td style={{ padding: 8 }}>{p.productId}</td>
            <td style={{ padding: 8 }}>{p.category}</td>
            <td style={{ padding: 8, textAlign: 'right' }}>{(p.price ?? 0).toFixed(2)}</td>
            <td style={{ padding: 8, textAlign: 'right' }}>{p.stockQuantity ?? 0}</td>
              <td style={{ padding: 8, textAlign: 'right' }}>
              {hasRole('Staff') || hasRole('Admin') ? (
                <>
                  <button onClick={() => openEdit(p)} style={{ marginRight: 8 }}>Edit</button>
                  <button onClick={() => askDelete(p)}>Delete</button>
                </>
              ) : (
                <span style={{ color: '#666' }}>Restricted</span>
              )}
            </td>
          </tr>
        )}
      />

      {loading && <div style={{ marginTop: 8 }}><LoadingSpinner /></div>}

      {showModal && (
        <SimpleModal title={editing ? 'Edit product' : 'Create product'} onClose={() => setShowModal(false)}>
    <ProductForm initial={editing ?? {}} onCancel={() => { setShowModal(false); setFormErrors({}); }} onSubmit={handleSave} errors={formErrors} />
        </SimpleModal>
      )}

      {confirm && (
        <SimpleModal title="Confirm delete" onClose={() => setConfirm(null)}>
          <ConfirmDialog message={`Delete product ${confirm.name}?`} onConfirm={() => doDelete(confirm)} onCancel={() => setConfirm(null)} />
        </SimpleModal>
      )}
    </div>
  );
}
