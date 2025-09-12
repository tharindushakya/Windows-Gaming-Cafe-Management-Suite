import React, { useState, useEffect } from 'react';

export default function TransactionForm({ 
  initial = {}, 
  onCancel, 
  onSubmit, 
  errors = {},
  mode = 'create' // 'create', 'edit', 'refund', 'status'
}) {
  const [form, setForm] = useState({
    userId: '',
    amount: '',
    type: 'GameTime',
    paymentMethod: 'Cash',
    description: '',
    paymentReference: '',
    notes: '',
    status: 'Pending',
    refundAmount: '',
    reason: ''
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setForm(prevForm => ({ ...prevForm, ...initial }));
  }, [initial]);

  function handleChange(e) {
    const { name, value } = e.target;
    setForm(prev => ({
      ...prev,
      [name]: ['amount', 'refundAmount'].includes(name) ? value : value
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    try {
      setSaving(true);
      await onSubmit(form);
    } finally {
      setSaving(false);
    }
  }

  const transactionTypes = [
    { value: 'GameTime', label: 'Game Time' },
    { value: 'Product', label: 'Product' },
    { value: 'WalletTopup', label: 'Wallet Top-up' },
    { value: 'Refund', label: 'Refund' },
    { value: 'LoyaltyRedemption', label: 'Loyalty Redemption' }
  ];

  const paymentMethods = [
    { value: 'Cash', label: 'Cash' },
    { value: 'CreditCard', label: 'Credit Card' },
    { value: 'DebitCard', label: 'Debit Card' },
    { value: 'Wallet', label: 'Wallet' },
    { value: 'LoyaltyPoints', label: 'Loyalty Points' },
    { value: 'BankTransfer', label: 'Bank Transfer' }
  ];

  const statusOptions = [
    { value: 'Pending', label: 'Pending' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Failed', label: 'Failed' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  const formStyle = {
    display: 'grid',
    gap: '16px',
    padding: '16px',
    maxWidth: '600px'
  };

  const labelStyle = {
    display: 'block',
    fontSize: '14px',
    fontWeight: '600',
    color: '#374151',
    marginBottom: '4px'
  };

  const inputStyle = {
    width: '100%',
    padding: '8px 12px',
    border: '1px solid #d1d5db',
    borderRadius: '6px',
    fontSize: '14px'
  };

  const errorStyle = {
    color: '#ef4444',
    fontSize: '12px',
    marginTop: '4px'
  };

  const buttonStyle = {
    padding: '8px 16px',
    border: 'none',
    borderRadius: '6px',
    fontSize: '14px',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s'
  };

  return (
    <form onSubmit={handleSubmit} style={formStyle}>
      {mode === 'create' && (
        <>
          <div>
            <label style={labelStyle}>
              User ID *
            </label>
            <input
              type="number"
              name="userId"
              value={form.userId}
              onChange={handleChange}
              style={inputStyle}
              placeholder="Enter user ID"
              required
            />
            {errors?.userId && <div style={errorStyle}>{errors.userId.join(', ')}</div>}
          </div>

          <div>
            <label style={labelStyle}>
              Description *
            </label>
            <input
              type="text"
              name="description"
              value={form.description}
              onChange={handleChange}
              style={inputStyle}
              placeholder="Transaction description"
              required
            />
            {errors?.description && <div style={errorStyle}>{errors.description.join(', ')}</div>}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <div>
              <label style={labelStyle}>
                Amount *
              </label>
              <input
                type="number"
                step="0.01"
                min="0"
                name="amount"
                value={form.amount}
                onChange={handleChange}
                style={inputStyle}
                placeholder="0.00"
                required
              />
              {errors?.amount && <div style={errorStyle}>{errors.amount.join(', ')}</div>}
            </div>
            <div>
              <label style={labelStyle}>
                Type
              </label>
              <select
                name="type"
                value={form.type}
                onChange={handleChange}
                style={inputStyle}
              >
                {transactionTypes.map(type => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
              {errors?.type && <div style={errorStyle}>{errors.type.join(', ')}</div>}
            </div>
          </div>

          <div>
            <label style={labelStyle}>
              Payment Method
            </label>
            <select
              name="paymentMethod"
              value={form.paymentMethod}
              onChange={handleChange}
              style={inputStyle}
            >
              {paymentMethods.map(method => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
            {errors?.paymentMethod && <div style={errorStyle}>{errors.paymentMethod.join(', ')}</div>}
          </div>

          <div>
            <label style={labelStyle}>
              Payment Reference
            </label>
            <input
              type="text"
              name="paymentReference"
              value={form.paymentReference}
              onChange={handleChange}
              style={inputStyle}
              placeholder="Optional payment reference (e.g., check number, card last 4 digits)"
            />
            {errors?.paymentReference && <div style={errorStyle}>{errors.paymentReference.join(', ')}</div>}
          </div>

          <div>
            <label style={labelStyle}>
              Notes
            </label>
            <textarea
              name="notes"
              value={form.notes}
              onChange={handleChange}
              style={{ ...inputStyle, minHeight: '80px', resize: 'vertical' }}
              placeholder="Optional notes about this transaction"
            />
            {errors?.notes && <div style={errorStyle}>{errors.notes.join(', ')}</div>}
          </div>
        </>
      )}

      {mode === 'refund' && (
        <>
          <div style={{ padding: '12px', background: '#fef3c7', borderRadius: '6px', border: '1px solid #f59e0b', marginBottom: '16px' }}>
            <div style={{ fontWeight: '600', color: '#92400e' }}>Processing Refund</div>
            <div style={{ fontSize: '14px', color: '#92400e', marginTop: '4px' }}>
              Original Amount: ${(initial.amount || 0).toFixed(2)}
            </div>
          </div>

          <div>
            <label style={labelStyle}>
              Refund Amount *
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              max={initial.amount || 0}
              name="refundAmount"
              value={form.refundAmount}
              onChange={handleChange}
              style={inputStyle}
              placeholder="0.00"
              required
            />
            {errors?.refundAmount && <div style={errorStyle}>{errors.refundAmount.join(', ')}</div>}
          </div>

          <div>
            <label style={labelStyle}>
              Reason *
            </label>
            <textarea
              name="reason"
              value={form.reason}
              onChange={handleChange}
              style={{ ...inputStyle, minHeight: '80px', resize: 'vertical' }}
              placeholder="Reason for the refund"
              required
            />
            {errors?.reason && <div style={errorStyle}>{errors.reason.join(', ')}</div>}
          </div>
        </>
      )}

      {mode === 'status' && (
        <>
          <div style={{ padding: '12px', background: '#f0f9ff', borderRadius: '6px', border: '1px solid #0ea5e9', marginBottom: '16px' }}>
            <div style={{ fontWeight: '600', color: '#0c4a6e' }}>Update Transaction Status</div>
            <div style={{ fontSize: '14px', color: '#0c4a6e', marginTop: '4px' }}>
              Current Status: {initial.status || 'Unknown'}
            </div>
          </div>

          <div>
            <label style={labelStyle}>
              New Status *
            </label>
            <select
              name="status"
              value={form.status}
              onChange={handleChange}
              style={inputStyle}
              required
            >
              {statusOptions.map(status => (
                <option key={status.value} value={status.value}>
                  {status.label}
                </option>
              ))}
            </select>
            {errors?.status && <div style={errorStyle}>{errors.status.join(', ')}</div>}
          </div>

          <div>
            <label style={labelStyle}>
              Notes
            </label>
            <textarea
              name="notes"
              value={form.notes}
              onChange={handleChange}
              style={{ ...inputStyle, minHeight: '80px', resize: 'vertical' }}
              placeholder="Optional notes about the status change"
            />
            {errors?.notes && <div style={errorStyle}>{errors.notes.join(', ')}</div>}
          </div>
        </>
      )}

      <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
        <button
          type="button"
          onClick={onCancel}
          disabled={saving}
          style={{
            ...buttonStyle,
            background: '#6b7280',
            color: '#fff'
          }}
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={saving}
          style={{
            ...buttonStyle,
            background: mode === 'refund' ? '#ef4444' : mode === 'status' ? '#0ea5e9' : '#10b981',
            color: '#fff'
          }}
        >
          {saving ? 'Processing...' : 
           mode === 'refund' ? 'Process Refund' :
           mode === 'status' ? 'Update Status' :
           'Create Transaction'}
        </button>
      </div>

      {errors?._global && (
        <div style={errorStyle}>
          {Array.isArray(errors._global) ? errors._global.join(', ') : errors._global}
        </div>
      )}
    </form>
  );
}
