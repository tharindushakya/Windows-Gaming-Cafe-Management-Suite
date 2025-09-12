import React, { useState } from 'react';

export default function WalletForm({ mode = 'deposit', initial = {}, onCancel, onSubmit, errors = {} }) {
  const [amount, setAmount] = useState(initial.amount ?? 0);
  const [description, setDescription] = useState(initial.description ?? '');
  const [toUserId, setToUserId] = useState(initial.toUserId ?? '');
  const [saving, setSaving] = useState(false);

  async function submit(e) {
    e.preventDefault();
    try {
      setSaving(true);
      if (mode === 'transfer') {
        await onSubmit({ fromUserId: initial.fromUserId, toUserId: Number(toUserId), amount: Number(amount), description });
      } else {
        await onSubmit({ amount: Number(amount), description, paymentMethod: 'Cash' });
      }
    } finally { setSaving(false); }
  }

  return (
    <form onSubmit={submit} style={{ padding: 8 }}>
      {mode === 'transfer' ? (
        <>
          <label>To User ID<input name="toUserId" value={toUserId} onChange={e => setToUserId(e.target.value)} className="input" required /></label>
          {errors?.toUserId && <div style={{ color: '#ef4444' }}>{errors.toUserId.join(', ')}</div>}
          <label>Amount<input name="amount" value={amount} onChange={e => setAmount(e.target.value)} className="input" type="number" step="0.01" required /></label>
          {errors?.amount && <div style={{ color: '#ef4444' }}>{errors.amount.join(', ')}</div>}
          <label>Description<textarea value={description} onChange={e => setDescription(e.target.value)} className="input" /></label>
        </>
      ) : (
        <>
          <label>Amount<input name="amount" value={amount} onChange={e => setAmount(e.target.value)} className="input" type="number" step="0.01" required /></label>
          <label>Description<textarea value={description} onChange={e => setDescription(e.target.value)} className="input" /></label>
        </>
      )}
      <div style={{ display: 'flex', gap: 8 }}>
          <button type="button" onClick={onCancel} disabled={saving}>Cancel</button>
        <button type="submit" style={{ background: '#0ea5a9', color: '#fff' }} disabled={saving}>{saving ? 'Submitting...' : 'Submit'}</button>
      </div>
      {errors?._global && <div style={{ color: '#ef4444', marginTop: 8 }}>{errors._global.join(', ')}</div>}
    </form>
  );
}
