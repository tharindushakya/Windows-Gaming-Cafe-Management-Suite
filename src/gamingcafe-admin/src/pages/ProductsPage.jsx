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
    <div className="p-4">
      <h2 className="text-lg font-semibold mb-3">Products</h2>
      <div className="mb-3">
        {hasRole('Staff') || hasRole('Admin') ? (
          <button onClick={openCreate} className="px-3 py-2 rounded bg-sky-500 text-white">Create product</button>
        ) : (
          <span className="text-gray-500">You don't have permission to create products</span>
        )}
      </div>

  <PagedList data={data} page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} loading={loading}
        emptyTitle="No products found"
        emptySubtitle="Create your first product to get started"
        emptyIcon="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M9 1v6m6-6v6"
        renderRow={p => (
          <tr>
            <td className="px-3 py-2">{p.productId}</td>
            <td className="px-3 py-2">{p.category}</td>
            <td className="px-3 py-2 text-right">{(p.price ?? 0).toFixed(2)}</td>
            <td className="px-3 py-2 text-right">{p.stockQuantity ?? 0}</td>
              <td className="px-3 py-2 text-right">
              {hasRole('Staff') || hasRole('Admin') ? (
                <>
                  <button onClick={() => openEdit(p)} className="mr-2 px-2 py-1 border rounded text-sm">Edit</button>
                  <button onClick={() => askDelete(p)} className="px-2 py-1 border rounded text-sm text-red-600">Delete</button>
                </>
              ) : (
                <span className="text-gray-500">Restricted</span>
              )}
            </td>
          </tr>
        )}
      />

      {loading && <div className="mt-2"><LoadingSpinner /></div>}

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
