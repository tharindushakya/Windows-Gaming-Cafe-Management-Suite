import React, { useEffect, useState, useCallback, useRef } from 'react';
import api from '../api';
import { useToast } from '../components/ToastProvider';
import ProductForm from '../components/ProductForm';
import SimpleModal from '../components/SimpleModal';
import PagedList from '../components/PagedList';

function fmtDate(d) {
  if (!d) return '';
  try { return new Date(d).toLocaleString(); } catch { return String(d); }
}

export default function Inventory() {
  // products list
  const [products, setProducts] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');

  // low stock and movements
  const [lowStock, setLowStock] = useState([]);
  const [movements, setMovements] = useState([]);
  const [movementLoading, setMovementLoading] = useState(false);

  // product modal
  const [showProductModal, setShowProductModal] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [productErrors, setProductErrors] = useState(null);

  // adjust modal (single)
  const [showAdjustModal, setShowAdjustModal] = useState(false);
  const [adjustingProduct, setAdjustingProduct] = useState(null);
  const [adjustQty, setAdjustQty] = useState(0);
  const [adjustReason, setAdjustReason] = useState('');

  // bulk adjust
  const [showBulkAdjust, setShowBulkAdjust] = useState(false);
  const [bulkText, setBulkText] = useState('productId,quantityChange,reason\n');

  const [error, setError] = useState(null);
  const toast = useToast();

  // fetch products (supports PagedResponse or plain array)
  const fetchProducts = useCallback(async (opts = {}) => {
    setLoading(true);
    setError(null);
    try {
      const q = `?page=${opts.page ?? page}&pageSize=${opts.pageSize ?? pageSize}` + (search ? `&search=${encodeURIComponent(search)}` : '');
      const resp = await api.get(`/api/v1.0/products${q}`);
      if (resp && resp.data) {
        setProducts(resp.data);
        setTotal(resp.totalCount || 0);
      } else if (Array.isArray(resp)) {
        setProducts(resp);
        setTotal(resp.length);
      } else {
        setProducts([]);
        setTotal(0);
      }
    } catch (err) {
      setError(err?.message || 'Failed to load products');
    } finally { setLoading(false); }
  }, [page, pageSize, search]);

  const fetchLowStock = async () => {
    try {
      const resp = await api.get('/api/v1.0/inventory/low-stock');
      setLowStock(resp || []);
    } catch (e) { /* ignore */ }
  };

  const fetchMovements = async (opts = { page: 1, pageSize: 20, productId: null, type: null }) => {
    setMovementLoading(true);
    try {
      let q = `?page=${opts.page}&pageSize=${opts.pageSize}`;
      if (opts.productId) q += `&productId=${opts.productId}`;
      if (opts.type) q += `&type=${opts.type}`;
      const resp = await api.get(`/api/v1.0/inventory/movements${q}`);
      if (resp && resp.data) setMovements(resp.data);
      else if (Array.isArray(resp)) setMovements(resp);
      else setMovements([]);
    } catch (e) { /* ignore */ }
    finally { setMovementLoading(false); }
  };

  useEffect(() => { fetchProducts(); }, [fetchProducts]);
  useEffect(() => { fetchLowStock(); fetchMovements(); }, []);

  // Debounced search
  const searchTimeoutRef = useRef(null);
  useEffect(() => {
    if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current);
    searchTimeoutRef.current = setTimeout(() => {
      setPage(1);
      fetchProducts({ page: 1 });
    }, 300);
    return () => { if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current); };
  }, [search, fetchProducts]);

  // create or update product
  async function saveProduct(data) {
    setProductErrors(null);
    try {
      if (editingProduct && editingProduct.productId) {
        await api.put(`/api/v1.0/products/${editingProduct.productId}`, data);
      } else {
        await api.post('/api/v1.0/products', data);
      }
      setShowProductModal(false);
      setEditingProduct(null);
      fetchProducts();
      fetchLowStock();
  toast?.push('Product saved', 'success');
    } catch (err) {
      setProductErrors(err?.errors || { _global: [err?.message || 'Save failed'] });
  toast?.push(err?.message || 'Save failed', 'error');
      throw err;
    }
  }

  async function removeProduct(id) {
    if (!window.confirm('Delete product? This cannot be undone.')) return;
    try {
      await api.del(`/api/v1.0/products/${id}`);
      fetchProducts();
      fetchLowStock();
  toast?.push('Product deleted', 'success');
    } catch (err) { setError(err?.message || 'Delete failed'); }
  }

  // single adjust
  async function submitAdjustment(e) {
    e && e.preventDefault();
    setError(null);
    if (!adjustingProduct) return setError('Select a product to adjust');
    const body = {
      productId: Number(adjustingProduct.productId),
      quantityChange: Number(adjustQty),
      reason: adjustReason || 'Manual adjustment (admin)'
    };
    try {
      await api.post('/api/v1.0/inventory/adjust', body);
      setShowAdjustModal(false);
      setAdjustingProduct(null);
      setAdjustQty(0);
      setAdjustReason('');
      fetchProducts();
      fetchLowStock();
      fetchMovements();
  toast?.push('Inventory adjusted', 'success');
    } catch (err) {
      setError(err?.data?.message || err?.message || 'Adjustment failed');
  toast?.push(err?.message || 'Adjustment failed', 'error');
    }
  }

  // bulk adjust: CSV lines productId,quantityChange,reason
  async function submitBulkAdjust(e) {
    e && e.preventDefault();
    setError(null);
    const lines = bulkText.split('\n').map(l => l.trim()).filter(l => l);
    const adjustments = [];
    for (const ln of lines) {
      if (ln.startsWith('productId')) continue; // header
      const parts = ln.split(',').map(p => p.trim());
      const pid = Number(parts[0]);
      const qty = Number(parts[1]);
      const reason = parts.slice(2).join(',') || 'Bulk adjust';
      if (!Number.isFinite(pid) || !Number.isFinite(qty)) continue;
      adjustments.push({ productId: pid, quantityChange: qty, reason });
    }
    if (adjustments.length === 0) return setError('No valid adjustments found');
    try {
      await api.post('/api/v1.0/inventory/bulk-adjust', { adjustments });
      setShowBulkAdjust(false);
      fetchProducts(); fetchLowStock(); fetchMovements();
  toast?.push('Bulk adjustments applied', 'success');
    } catch (err) { setError(err?.message || 'Bulk adjust failed'); }
  }

  async function updateStock(productId, newQty) {
    setError(null);
    try {
      const token = api.getToken && api.getToken();
      const headers = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;
      const res = await fetch(`/api/v1.0/products/${productId}/stock`, {
        method: 'PATCH', headers, body: JSON.stringify({ stockQuantity: Number(newQty) })
      });
      if (!res.ok) {
        const txt = await res.text();
        throw new Error(txt || res.statusText || 'Failed to update stock');
      }
      fetchProducts(); fetchLowStock();
  toast?.push('Stock updated', 'success');
    } catch (err) { setError(err?.message || 'Failed to update stock'); }
  }

  function openAdjust(p) {
    setAdjustingProduct(p);
    setAdjustQty(0);
    setAdjustReason('');
    setShowAdjustModal(true);
  }

  function exportCSV() {
    const hdr = ['productId,name,category,price,stockQuantity,minStockLevel,isActive,updatedAt'];
    const rows = products.map(p => [p.productId, `"${(p.name||'').replace(/"/g,'""')}"`, p.category, p.price ?? 0, p.stockQuantity ?? 0, p.minStockLevel ?? '', p.isActive ? 1 : 0, p.updatedAt || p.createdAt || ''].join(','));
    const csv = hdr.concat(rows).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = 'products.csv'; document.body.appendChild(a); a.click(); a.remove(); URL.revokeObjectURL(url);
  }

  // UI
  return (
    <div>
      <h2 className="text-2xl font-semibold">Inventory</h2>
      <p className="text-sm text-gray-600">Manage products, stock levels, and inventory movements.</p>

      {error && <div className="text-red-600 mb-2">{error}</div>}

      <div className="flex gap-4 mt-4">
        <div className="flex-[2]">
          <div className="mb-2 flex gap-2 items-center">
            <input className="border rounded px-2 py-1 flex-1" placeholder="Search products..." value={search} onChange={e => setSearch(e.target.value)} />
            <button className="px-2 py-1 border rounded bg-gray-100 hover:bg-gray-200" onClick={() => { setPage(1); fetchProducts({ page: 1 }); }}>Search</button>
            <button className="px-2 py-1 bg-sky-600 text-white rounded" onClick={() => { setEditingProduct(null); setShowProductModal(true); }}>Add product</button>
            <button className="px-2 py-1 border rounded" onClick={() => setShowBulkAdjust(true)}>Bulk adjust</button>
            <button className="px-2 py-1 border rounded" onClick={exportCSV}>Export CSV</button>
          </div>

          <div className="border border-gray-200 p-2 rounded">
            <h3 className="font-medium">Products</h3>
            <PagedList
              data={products}
              page={page}
              pageSize={pageSize}
              totalCount={total}
              loading={loading}
              onPageChange={(p) => { setPage(p); fetchProducts({ page: p }); }}
              renderRow={(p) => (
                <tr className="border-t bg-white" key={p.productId}>
                  <td className="p-2 text-sm">{p.productId}</td>
                  <td className="p-2 text-sm">{p.name}</td>
                  <td className="p-2 text-sm">{p.category}</td>
                  <td className="p-2 text-sm">${(p.price || 0).toFixed(2)}</td>
                  <td className="p-2 text-sm">
                    <div className="flex flex-col">
                      <div>{p.stockQuantity}</div>
                      <div className="mt-2 flex flex-wrap gap-2">
                        <button className="px-2 py-1 border rounded text-xs" onClick={() => { const val = prompt('Set new stock quantity', String(p.stockQuantity)); if (val !== null) updateStock(p.productId, Number(val)); }}>Set</button>
                        <button className="px-2 py-1 bg-blue-600 text-white rounded text-xs" onClick={() => { setEditingProduct(p); setShowProductModal(true); }}>Edit</button>
                        <button className="px-2 py-1 border rounded text-xs" onClick={() => openAdjust(p)}>Adjust</button>
                        <button className="px-2 py-1 border rounded text-xs text-red-600" onClick={() => removeProduct(p.productId)}>Delete</button>
                      </div>
                    </div>
                  </td>
                  <td className="p-2 text-sm">{p.minStockLevel}</td>
                  <td className="p-2 text-sm">{fmtDate(p.updatedAt || p.createdAt)}</td>
                  <td className="p-2 text-sm">{p.isActive ? 'Active' : 'Inactive'}</td>
                </tr>
              )}
            />
          </div>
        </div>
        <div className="flex-1">
          <div className="border border-gray-200 p-2 mb-3 rounded">
            <h4 className="font-medium">Low stock</h4>
            {lowStock.length === 0 ? <div className="text-sm text-gray-500">No low stock products</div> : (
              <ul className="text-sm">
                {lowStock.map(p => (
                  <li key={p.productId}>{p.productName} — {p.currentStock} (min {p.minStockLevel || p.lowStockThreshold})</li>
                ))}
              </ul>
            )}
          </div>

          <div className="border border-gray-200 p-2 rounded">
            <h4 className="font-medium">Recent movements</h4>
            <div className="mb-2 flex gap-2">
              <select className="border rounded px-2 py-1 text-sm" onChange={e => fetchMovements({ page: 1, pageSize: 20, type: e.target.value || null })}>
                <option value="">All types</option>
                <option value="StockIn">Stock In</option>
                <option value="StockOut">Stock Out</option>
              </select>
              <select className="border rounded px-2 py-1 text-sm" onChange={e => fetchMovements({ page: 1, pageSize: 20, productId: e.target.value || null })}>
                <option value="">All products</option>
                {products.map(p => <option key={p.productId} value={p.productId}>{p.name}</option>)}
              </select>
              <button className="px-2 py-1 border rounded text-sm" onClick={() => fetchMovements({ page: 1, pageSize: 20 })}>Refresh</button>
            </div>
            {movementLoading ? <div>Loading...</div> : (
              <div className="max-h-72 overflow-auto">
                <table className="w-full">
                  <thead>
                    <tr className="text-sm text-left"><th className="p-1">Date</th><th className="p-1">Product</th><th className="p-1">Qty</th><th className="p-1">By</th></tr>
                  </thead>
                  <tbody>
                    {movements.map(m => (
                      <tr key={m.movementId} className="text-sm border-t">
                        <td className="whitespace-nowrap p-1">{fmtDate(m.movementDate)}</td>
                        <td className="p-1">{m.productName}</td>
                        <td className="p-1">{m.quantity} ({m.type})</td>
                        <td className="p-1">{m.username}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Product modal */}
      {showProductModal && (
        <SimpleModal title={editingProduct ? 'Edit product' : 'Add product'} onClose={() => { setShowProductModal(false); setEditingProduct(null); setProductErrors(null); }}>
          <ProductForm initial={editingProduct || {}} onCancel={() => { setShowProductModal(false); setEditingProduct(null); }} onSubmit={saveProduct} errors={productErrors} />
        </SimpleModal>
      )}

      {/* Adjust modal */}
      {showAdjustModal && adjustingProduct && (
        <SimpleModal title={`Adjust ${adjustingProduct.name}`} onClose={() => setShowAdjustModal(false)}>
          <form onSubmit={submitAdjustment} className="p-2">
            <div className="grid gap-2">
              <label className="text-sm">Quantity change (use negative to reduce)
                <input name="qty" type="number" value={adjustQty} onChange={e => setAdjustQty(e.target.value)} required className="border rounded px-2 py-1 w-full mt-1" />
              </label>
              <label className="text-sm">Reason
                <input value={adjustReason} onChange={e => setAdjustReason(e.target.value)} className="border rounded px-2 py-1 w-full mt-1" />
              </label>
              <div className="flex gap-2">
                <button type="submit" className="px-3 py-1 bg-sky-600 text-white rounded">Submit</button>
                <button type="button" className="px-3 py-1 border rounded" onClick={() => setShowAdjustModal(false)}>Cancel</button>
              </div>
            </div>
          </form>
        </SimpleModal>
      )}

      {/* Bulk adjust modal */}
      {showBulkAdjust && (
        <SimpleModal title="Bulk adjust" onClose={() => setShowBulkAdjust(false)}>
          <form onSubmit={submitBulkAdjust} className="p-2">
            <div className="grid gap-2">
              <div className="text-sm">CSV rows: productId,quantityChange,reason (header optional)</div>
              <textarea value={bulkText} onChange={e => setBulkText(e.target.value)} rows={8} className="w-full border rounded p-2" />
              <div className="flex gap-2">
                <button type="submit" className="px-3 py-1 bg-sky-600 text-white rounded">Submit</button>
                <button type="button" className="px-3 py-1 border rounded" onClick={() => setShowBulkAdjust(false)}>Cancel</button>
              </div>
            </div>
          </form>
        </SimpleModal>
      )}
    </div>
  );
}
