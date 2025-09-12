import React, { useState, useEffect } from 'react';

export default function ProductForm({ initial = {}, onCancel, onSubmit, errors = {} }) {
  const [form, setForm] = useState({ name: '', description: '', price: 0, category: '', stockQuantity: 0, minStockLevel: 0, isActive: true });
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
    <form onSubmit={submit} className="p-2">
      <div className="grid gap-2">
        <label className="flex flex-col">
          <span className="font-medium text-sm">Name</span>
          <input name="name" value={form.name} onChange={change} className="border rounded px-2 py-1 w-full" required />
        </label>
        {errors?.name && <div className="text-red-500 text-sm">{errors.name.join(', ')}</div>}

        <label className="flex flex-col">
          <span className="font-medium text-sm">Category</span>
          <input name="category" value={form.category} onChange={change} className="border rounded px-2 py-1 w-full" required />
        </label>

        <label className="flex flex-col">
          <span className="font-medium text-sm">Price</span>
          <input name="price" type="number" step="0.01" value={form.price} onChange={change} className="border rounded px-2 py-1 w-full" required />
        </label>
        {errors?.price && <div className="text-red-500 text-sm">{errors.price.join(', ')}</div>}

        <label className="flex flex-col">
          <span className="font-medium text-sm">Stock Quantity</span>
          <input name="stockQuantity" type="number" value={form.stockQuantity} onChange={change} className="border rounded px-2 py-1 w-full" />
        </label>

        <label className="flex flex-col">
          <span className="font-medium text-sm">Min Stock Level</span>
          <input name="minStockLevel" type="number" value={form.minStockLevel} onChange={change} className="border rounded px-2 py-1 w-full" />
        </label>

        <label className="flex flex-col">
          <span className="font-medium text-sm">Description</span>
          <textarea name="description" value={form.description} onChange={change} className="border rounded px-2 py-1 w-full" />
        </label>

        <label className="flex items-center gap-2">
          <input name="isActive" type="checkbox" checked={form.isActive} onChange={change} className="h-4 w-4" />
          <span className="text-sm">Active</span>
        </label>

        <div className="flex gap-2 mt-2">
          <button type="button" onClick={onCancel} disabled={saving} className="px-3 py-1 rounded bg-gray-200 hover:bg-gray-300">Cancel</button>
          <button type="submit" disabled={saving} className="px-3 py-1 rounded bg-teal-500 text-white hover:bg-teal-600 disabled:opacity-50">{saving ? 'Saving...' : 'Save'}</button>
        </div>

        {errors?._global && <div className="text-red-500 text-sm mt-2">{errors._global.join(', ')}</div>}
      </div>
    </form>
  );
}
