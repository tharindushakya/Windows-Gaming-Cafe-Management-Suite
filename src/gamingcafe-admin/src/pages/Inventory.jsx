import React, { useEffect, useState, useCallback, useRef } from 'react';
// FilterSearch removed — use native inputs
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
  const [debouncedSearch, setDebouncedSearch] = useState('');

  const [error, setError] = useState(null);
  const toast = useToast();

  // fetch products (supports PagedResponse or plain array)
  const fetchProducts = useCallback(async (opts = {}) => {
    setLoading(true);
    setError(null);
    try {
      const searchTerm = opts.search !== undefined ? opts.search : debouncedSearch;
      const q = `?page=${opts.page ?? page}&pageSize=${opts.pageSize ?? pageSize}` + (searchTerm ? `&search=${encodeURIComponent(searchTerm)}` : '');
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
  }, [page, pageSize, debouncedSearch]);

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

  useEffect(() => { 
    // Load products when page or debouncedSearch change
    fetchProducts();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fetchProducts, page, debouncedSearch]);
  
  useEffect(() => { fetchLowStock(); fetchMovements(); }, []);

  // Debounced search
  const searchTimeoutRef = useRef(null);
  useEffect(() => {
    if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current);
    searchTimeoutRef.current = setTimeout(() => {
      setDebouncedSearch(search || '');
      setPage(1); // Reset to page 1 when searching
    }, 500);
    return () => { if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current); };
  }, [search]);

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
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900">Inventory</h2>
        <p className="text-gray-600 mt-2">Manage products, stock levels, and inventory movements.</p>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-700">
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
            {error}
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-2 space-y-6">
          {/* Search and Actions Bar */}
          <div className="bg-white rounded-lg shadow p-4">
            <div className="flex flex-col sm:flex-row gap-3">
                <div className="flex-1">
                  <input 
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500" 
                    placeholder="Search products..." 
                    value={search} 
                    onChange={e => { setSearch(e.target.value); }} 
                  />
                </div>
                <div className="flex gap-2">
                <button 
                  className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg transition-colors duration-200" 
                  onClick={() => { setEditingProduct(null); setShowProductModal(true); }}
                >
                  <svg className="w-4 h-4 mr-2 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                  </svg>
                  Add Product
                </button>
                <button 
                  className="px-4 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors duration-200" 
                  onClick={() => setShowBulkAdjust(true)}
                >
                  Bulk Adjust
                </button>
                <button 
                  className="px-4 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors duration-200" 
                  onClick={exportCSV}
                >
                  Export CSV
                </button>
              </div>
            </div>
          </div>

          {/* Products Table */}
          <div className="bg-white rounded-lg shadow">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-medium text-gray-900">Products</h3>
            </div>
            <div className="overflow-hidden">
              <PagedList
                data={products}
                page={page}
                pageSize={pageSize}
                totalCount={total}
                loading={loading}
                onPageChange={(p) => { setPage(p); fetchProducts({ page: p }); }}
                tableHeaders={['ID', 'Name', 'Category', 'Price', 'Stock', 'Min Level', 'Updated', 'Status']}
                emptyTitle={search ? `No products found for "${search}"` : "No products found"}
                emptySubtitle={search ? "Try adjusting your search or add new products" : "Add your first product to get started"}
                emptyIcon="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M9 1v6m6-6v6"
                renderRow={(p) => (
                  <tr className="hover:bg-gray-50" key={p.productId}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      #{p.productId}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">{p.name}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                        {p.category}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      ${(p.price || 0).toFixed(2)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900 mb-2">{p.stockQuantity}</div>
                      <div className="flex flex-wrap gap-1">
                        <button 
                          className="inline-flex items-center px-2 py-1 border border-gray-300 text-xs font-medium rounded text-gray-700 bg-white hover:bg-gray-50 transition-colors duration-200" 
                          onClick={() => { const val = prompt('Set new stock quantity', String(p.stockQuantity)); if (val !== null) updateStock(p.productId, Number(val)); }}
                        >
                          Set
                        </button>
                        <button 
                          className="inline-flex items-center px-2 py-1 border border-transparent text-xs font-medium rounded text-white bg-indigo-600 hover:bg-indigo-700 transition-colors duration-200" 
                          onClick={() => { setEditingProduct(p); setShowProductModal(true); }}
                        >
                          Edit
                        </button>
                        <button 
                          className="inline-flex items-center px-2 py-1 border border-gray-300 text-xs font-medium rounded text-gray-700 bg-white hover:bg-gray-50 transition-colors duration-200" 
                          onClick={() => openAdjust(p)}
                        >
                          Adjust
                        </button>
                        <button 
                          className="inline-flex items-center px-2 py-1 border border-red-300 text-xs font-medium rounded text-red-700 bg-white hover:bg-red-50 transition-colors duration-200" 
                          onClick={() => removeProduct(p.productId)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {p.minStockLevel}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {fmtDate(p.updatedAt || p.createdAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        p.isActive 
                          ? 'bg-green-100 text-green-800' 
                          : 'bg-red-100 text-red-800'
                      }`}>
                        {p.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                  </tr>
                )}
              />
            </div>
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Low Stock Alert */}
          <div className="bg-white rounded-lg shadow">
            <div className="px-6 py-4 border-b border-gray-200">
              <h4 className="text-lg font-medium text-gray-900">Low Stock Alert</h4>
            </div>
            <div className="p-6">
              {lowStock.length === 0 ? (
                <div className="text-center py-4">
                  <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <p className="mt-2 text-sm text-gray-500">No low stock products</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {lowStock.map(p => (
                    <div key={p.productId} className="flex items-center justify-between p-3 bg-red-50 rounded-lg">
                      <div>
                        <div className="text-sm font-medium text-gray-900">{p.productName}</div>
                        <div className="text-xs text-red-600">
                          Stock: {p.currentStock} (Min: {p.minStockLevel || p.lowStockThreshold})
                        </div>
                      </div>
                      <svg className="w-5 h-5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                      </svg>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Recent Movements */}
          <div className="bg-white rounded-lg shadow">
            <div className="px-6 py-4 border-b border-gray-200">
              <h4 className="text-lg font-medium text-gray-900">Recent Movements</h4>
            </div>
            <div className="p-6">
              <div className="space-y-3 mb-4">
                <select 
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-indigo-500 focus:border-indigo-500" 
                  onChange={e => fetchMovements({ page: 1, pageSize: 20, type: e.target.value || null })}
                >
                  <option value="">All types</option>
                  <option value="StockIn">Stock In</option>
                  <option value="StockOut">Stock Out</option>
                </select>
                <select 
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-indigo-500 focus:border-indigo-500" 
                  onChange={e => fetchMovements({ page: 1, pageSize: 20, productId: e.target.value || null })}
                >
                  <option value="">All products</option>
                  {products.map(p => <option key={p.productId} value={p.productId}>{p.name}</option>)}
                </select>
                <button 
                  className="w-full px-3 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors duration-200" 
                  onClick={() => fetchMovements({ page: 1, pageSize: 20 })}
                >
                  Refresh
                </button>
              </div>

              {movementLoading ? (
                <div className="text-center py-4">
                  <svg className="animate-spin mx-auto h-6 w-6 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                </div>
              ) : (
                <div className="max-h-80 overflow-y-auto">
                  <div className="space-y-2">
                    {movements.map(m => (
                      <div key={m.movementId} className="p-3 border border-gray-200 rounded-lg">
                        <div className="flex justify-between items-start">
                          <div className="flex-1">
                            <div className="text-sm font-medium text-gray-900">{m.productName}</div>
                            <div className="text-xs text-gray-500">{fmtDate(m.movementDate)}</div>
                          </div>
                          <div className="text-right">
                            <div className={`text-sm font-medium ${m.type === 'StockIn' ? 'text-green-600' : 'text-red-600'}`}>
                              {m.type === 'StockIn' ? '+' : '-'}{Math.abs(m.quantity)}
                            </div>
                            <div className="text-xs text-gray-500">{m.username}</div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
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
