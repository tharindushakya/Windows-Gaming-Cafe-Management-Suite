import React from 'react';

export default function ConfirmDialog({ title = 'Confirm', message, onConfirm, onCancel }) {
  return (
    <div style={{ padding: 12 }}>
      <h3 style={{ marginTop: 0 }}>{title}</h3>
      <p>{message}</p>
      <div style={{ display: 'flex', gap: 8 }}>
        <button onClick={onCancel}>Cancel</button>
        <button onClick={onConfirm} style={{ background: '#ef4444', color: '#fff' }}>Confirm</button>
      </div>
    </div>
  );
}
