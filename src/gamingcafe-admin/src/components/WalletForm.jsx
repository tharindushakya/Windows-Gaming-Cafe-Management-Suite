import React, { useState } from 'react';
import { useToast } from './ToastProvider';

export default function WalletForm({ mode = 'deposit', initial = {}, onCancel, onSubmit, errors = {} }) {
  const [amount, setAmount] = useState(initial.amount ?? 0);
  const [description, setDescription] = useState(initial.description ?? '');
  const [toUserId, setToUserId] = useState(initial.toUserId ?? '');
  const [saving, setSaving] = useState(false);
  const toast = useToast();

  async function submit(e) {
    e.preventDefault();
    try {
      setSaving(true);
      if (mode === 'transfer') {
        await onSubmit({ fromUserId: initial.fromUserId, toUserId: Number(toUserId), amount: Number(amount), description });
        toast.push('Transfer submitted', 'success');
      } else {
        await onSubmit({ amount: Number(amount), description, paymentMethod: 'Cash' });
        toast.push('Top-up submitted', 'success');
      }
    } catch (err) {
      toast.push(err?.message || 'Submission failed', 'error');
      throw err;
    } finally { setSaving(false); }
  }

  return (
    <form onSubmit={submit} className="p-2">
      {mode === 'transfer' ? (
        <>
          <label className="flex flex-col text-sm">
            <span>To User ID</span>
            <input name="toUserId" value={toUserId} onChange={e => setToUserId(e.target.value)} className="border rounded px-2 py-1 w-full" required />
          </label>
          {errors?.toUserId && <div className="text-red-500 text-sm">{errors.toUserId.join(', ')}</div>}
          <label className="flex flex-col text-sm">
            <span>Amount</span>
            <input name="amount" value={amount} onChange={e => setAmount(e.target.value)} className="border rounded px-2 py-1 w-full" type="number" step="0.01" required />
          </label>
          {errors?.amount && <div className="text-red-500 text-sm">{errors.amount.join(', ')}</div>}
          <label className="flex flex-col text-sm">
            <span>Description</span>
            <textarea value={description} onChange={e => setDescription(e.target.value)} className="border rounded px-2 py-1 w-full" />
          </label>
        </>
      ) : (
        <>
          <label className="flex flex-col text-sm">
            <span>Amount</span>
            <input name="amount" value={amount} onChange={e => setAmount(e.target.value)} className="border rounded px-2 py-1 w-full" type="number" step="0.01" required />
          </label>
          <label className="flex flex-col text-sm">
            <span>Description</span>
            <textarea value={description} onChange={e => setDescription(e.target.value)} className="border rounded px-2 py-1 w-full" />
          </label>
        </>
      )}
      <div className="flex gap-2 mt-2">
          <button type="button" onClick={onCancel} disabled={saving} className="px-3 py-1 rounded bg-gray-200 hover:bg-gray-300">Cancel</button>
        <button type="submit" className="px-3 py-1 rounded bg-teal-500 text-white hover:bg-teal-600 disabled:opacity-50" disabled={saving}>{saving ? 'Submitting...' : 'Submit'}</button>
      </div>
      {errors?._global && <div className="text-red-500 text-sm mt-2">{errors._global.join(', ')}</div>}
    </form>
  );
}
