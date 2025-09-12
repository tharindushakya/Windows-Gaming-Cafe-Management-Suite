import React, { useState, useEffect } from 'react';

export default function StationForm({ initial = {}, onCancel, onSubmit, errors = {} }) {
  const [form, setForm] = useState({
    stationName: '',
    stationType: 'PC',
    hourlyRate: 0,
    isAvailable: true,
    notes: ''
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => { setForm(f => ({ ...f, ...initial })); }, [initial]);

  function change(e) {
    const { name, value, type, checked } = e.target;
    setForm(p => ({ ...p, [name]: type === 'checkbox' ? checked : (name === 'hourlyRate' ? Number(value) : value) }));
  }

  async function submit(e) {
    e.preventDefault();
    try {
      setSaving(true);
      await onSubmit(form);
    } finally { setSaving(false); }
  }

  const inputCls = 'w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-400';
  const labelCls = 'text-sm text-gray-300';

  return (
    <form onSubmit={submit} className="p-2">
      <div className="grid gap-3">
        <label className={labelCls}>
          <div className="mb-1">Station name</div>
          <input name="stationName" value={form.stationName} onChange={change} className={inputCls} required />
        </label>
        {errors?.stationName && <div className="text-red-400 text-sm">{errors.stationName.join(', ')}</div>}

        <label className={labelCls}>
          <div className="mb-1">Type</div>
          <select name="stationType" value={form.stationType} onChange={change} className={inputCls}>
            <option value="PC">PC</option>
            <option value="Console">Console</option>
            <option value="PS5">PS5</option>
            <option value="Xbox">Xbox</option>
          </select>
        </label>

        <label className={labelCls}>
          <div className="mb-1">Hourly rate</div>
          <input name="hourlyRate" type="number" step="0.01" value={form.hourlyRate} onChange={change} className={inputCls} />
        </label>
        {errors?.hourlyRate && <div className="text-red-400 text-sm">{errors.hourlyRate.join(', ')}</div>}

        <label className="flex items-center gap-3 text-sm text-gray-300">
          <input name="isAvailable" type="checkbox" checked={form.isAvailable} onChange={change} className="h-4 w-4 text-teal-400 bg-gray-800 border-gray-700 rounded" />
          Available
        </label>

        <label className={labelCls}>
          <div className="mb-1">Notes</div>
          <textarea name="notes" value={form.notes} onChange={change} className={inputCls} />
        </label>

        <div className="flex gap-3">
          <button type="button" onClick={onCancel} disabled={saving} className="px-3 py-1.5 rounded bg-transparent border border-gray-700 text-sm text-gray-200">Cancel</button>
          <button type="submit" disabled={saving} className="px-3 py-1.5 rounded bg-teal-500 hover:bg-teal-600 text-white text-sm">{saving ? 'Saving...' : 'Save'}</button>
        </div>
        {errors?._global && <div className="text-red-400 text-sm mt-2">{errors._global.join(', ')}</div>}
      </div>
    </form>
  );
}
