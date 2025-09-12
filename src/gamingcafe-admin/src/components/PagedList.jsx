import React from 'react';

export default function PagedList({ data, page, pageSize, totalCount, onPageChange, renderRow, loading }) {
  const totalPages = Math.ceil((totalCount || data.length) / pageSize);
  return (
    <div>
      {loading && <div className="py-4 text-center">Loading...</div>}
      <div className="overflow-x-auto">
        <table className="w-full table-auto border-collapse">
          <thead>{/* caller provides headers if needed */}</thead>
          <tbody>
            {data.map((item, idx) => (
              <React.Fragment key={item.id ?? item.productId ?? item.transactionId ?? idx}>
                {renderRow(item)}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mt-3 flex items-center justify-between text-sm">
        <div>Page {page} of {totalPages} — {totalCount ?? data.length} items</div>
        <div className="flex items-center">
          <button onClick={() => onPageChange(Math.max(1, page - 1))} disabled={page <= 1} className="px-3 py-1 mr-2 border rounded disabled:opacity-50">Prev</button>
          <button onClick={() => onPageChange(Math.min(totalPages || 1, page + 1))} disabled={page >= totalPages} className="px-3 py-1 border rounded disabled:opacity-50">Next</button>
        </div>
      </div>
    </div>
  );
}
