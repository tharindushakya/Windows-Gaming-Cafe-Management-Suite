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

  return (
    <div className="p-6">
      <form onSubmit={submit} className="space-y-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Station Name</label>
          <input 
            name="stationName" 
            value={form.stationName} 
            onChange={change} 
            className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Enter station name"
            required 
          />
          {errors?.stationName && (
            <p className="mt-1 text-sm text-red-600">{errors.stationName.join(', ')}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Station Type</label>
          <select 
            name="stationType" 
            value={form.stationType} 
            onChange={change} 
            className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
          >
            <option value="PC">PC Gaming</option>
            <option value="Console">Console</option>
            <option value="PS5">PlayStation 5</option>
            <option value="Xbox">Xbox Series X/S</option>
            <option value="Nintendo">Nintendo Switch</option>
            <option value="VR">VR Station</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Hourly Rate ($)</label>
          <div className="relative">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <span className="text-gray-500 sm:text-sm">$</span>
            </div>
            <input 
              name="hourlyRate" 
              type="number" 
              step="0.01" 
              min="0"
              value={form.hourlyRate} 
              onChange={change} 
              className="w-full pl-7 pr-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="0.00"
            />
          </div>
          {errors?.hourlyRate && (
            <p className="mt-1 text-sm text-red-600">{errors.hourlyRate.join(', ')}</p>
          )}
        </div>

        <div className="flex items-center">
          <input 
            name="isAvailable" 
            type="checkbox" 
            checked={form.isAvailable} 
            onChange={change} 
            className="h-4 w-4 text-indigo-600 border-gray-300 rounded focus:ring-indigo-500"
          />
          <label className="ml-3 block text-sm font-medium text-gray-700">
            Station is available for booking
          </label>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Notes</label>
          <textarea 
            name="notes" 
            value={form.notes} 
            onChange={change} 
            rows={3}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            placeholder="Additional notes about this station..."
          />
        </div>

        {errors?._global && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-center gap-2 text-red-700">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {errors._global.join(', ')}
            </div>
          </div>
        )}

        <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
          <button 
            type="button" 
            onClick={onCancel} 
            disabled={saving} 
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
          >
            Cancel
          </button>
          <button 
            type="submit" 
            disabled={saving} 
            className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
          >
            {saving ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                Saving...
              </>
            ) : (
              'Save Station'
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
