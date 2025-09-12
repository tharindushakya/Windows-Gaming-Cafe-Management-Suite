import React, { useState, useEffect } from 'react';

export default function ProductForm({ initial = {}, onCancel, onSubmit, errors = {} }) {
  const [form, setForm] = useState({
    name: '', description: '', price: 0, category: '', stockQuantity: 0, minStockLevel: 0, isActive: true
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => { setForm(f => ({ ...f, ...initial })); }, [initial]);

  function change(e) {
    const { name, value, type, checked } = e.target;
    setForm(p => ({ ...p, [name]: type === 'checkbox' ? checked : (name === 'price' || name === 'stockQuantity' || name === 'minStockLevel' ? Number(value) : value) }));
  }

  async function submit(e) {
    e.preventDefault();
    try {
      setSaving(true);
      await onSubmit(form);
    } finally { setSaving(false); }
  }

  return (
    <form onSubmit={submit} style={{ padding: 8 }}>
      <div style={{ display: 'grid', gap: 8 }}>
  <label>Name<input name="name" value={form.name} onChange={change} className="input" required /></label>
  {errors?.name && <div style={{ color: '#ef4444' }}>{errors.name.join(', ')}</div>}
        <label>Category<input name="category" value={form.category} onChange={change} className="input" required /></label>
  <label>Price<input name="price" type="number" step="0.01" value={form.price} onChange={change} className="input" required /></label>
  {errors?.price && <div style={{ color: '#ef4444' }}>{errors.price.join(', ')}</div>}
        <label>Stock Quantity<input name="stockQuantity" type="number" value={form.stockQuantity} onChange={change} className="input" /></label>
        <label>Min Stock Level<input name="minStockLevel" type="number" value={form.minStockLevel} onChange={change} className="input" /></label>
        <label>Description<textarea name="description" value={form.description} onChange={change} className="input" /></label>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}><input name="isActive" type="checkbox" checked={form.isActive} onChange={change} /> Active</label>
        <div style={{ display: 'flex', gap: 8 }}>
          <button type="button" onClick={onCancel} disabled={saving}>Cancel</button>
          <button type="submit" style={{ background: '#0ea5a9', color: '#fff' }} disabled={saving}>{saving ? 'Saving...' : 'Save'}</button>
        </div>
        {errors?._global && <div style={{ color: '#ef4444', marginTop: 8 }}>{errors._global.join(', ')}</div>}
      </div>
    </form>
  );
}
