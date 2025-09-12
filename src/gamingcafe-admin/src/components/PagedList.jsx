import React from 'react';

export default function PagedList({ data, page, pageSize, totalCount, onPageChange, renderRow, loading }) {
  const totalPages = Math.ceil((totalCount || data.length) / pageSize);
  return (
    <div>
      {loading && <div>Loading...</div>}
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          {/** Caller provides header inside renderRow for first item or outside */}
        </thead>
        <tbody>
          {data.map((item, idx) => (
            <React.Fragment key={item.id ?? item.productId ?? item.transactionId ?? idx}>
              {renderRow(item)}
            </React.Fragment>
          ))}
        </tbody>
      </table>

      <div style={{ marginTop: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>Page {page} of {totalPages} — {totalCount ?? data.length} items</div>
        <div>
          <button onClick={() => onPageChange(Math.max(1, page - 1))} disabled={page <= 1} style={{ marginRight: 8 }}>Prev</button>
          <button onClick={() => onPageChange(Math.min(totalPages || 1, page + 1))} disabled={page >= totalPages}>Next</button>
        </div>
      </div>
    </div>
  );
}
