import React from 'react';

export default function PagedList({ 
  data, 
  page, 
  pageSize, 
  totalCount, 
  onPageChange, 
  renderRow, 
  loading,
  emptyTitle = "No items found",
  emptySubtitle = "Create your first item to get started",
  emptyIcon = "M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
}) {
  const totalPages = Math.ceil((totalCount || data.length) / pageSize);
  
  return (
    <div>
      {loading && (
        <div className="py-8 text-center">
          <div className="inline-flex items-center">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-indigo-600 mr-3"></div>
            <span className="text-gray-500">Loading...</span>
          </div>
        </div>
      )}
      
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {data.length === 0 && !loading ? (
              <tr>
                <td colSpan={7} className="px-6 py-12 text-center text-gray-500">
                  <div className="flex flex-col items-center">
                    <svg className="w-12 h-12 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={emptyIcon} />
                    </svg>
                    <p className="text-lg font-medium text-gray-900 mb-1">{emptyTitle}</p>
                    <p className="text-gray-500">{emptySubtitle}</p>
                  </div>
                </td>
              </tr>
            ) : (
              data.map((item, idx) => (
                <React.Fragment key={item.id ?? item.productId ?? item.transactionId ?? idx}>
                  {renderRow(item)}
                </React.Fragment>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="bg-white px-6 py-4 border-t border-gray-200 flex items-center justify-between">
        <div className="flex items-center text-sm text-gray-700">
          <span>Showing </span>
          <span className="font-medium">{Math.min((page - 1) * pageSize + 1, totalCount || data.length)}</span>
          <span> to </span>
          <span className="font-medium">{Math.min(page * pageSize, totalCount || data.length)}</span>
          <span> of </span>
          <span className="font-medium">{totalCount ?? data.length}</span>
          <span> results</span>
        </div>
        
        <div className="flex items-center space-x-2">
          <button 
            onClick={() => onPageChange(Math.max(1, page - 1))} 
            disabled={page <= 1}
            className="relative inline-flex items-center px-4 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
          >
            <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Previous
          </button>
          
          <div className="flex items-center space-x-1">
            {/* Page numbers */}
            {totalPages <= 7 ? (
              // Show all pages if 7 or fewer
              Array.from({ length: totalPages }, (_, i) => i + 1).map(pageNum => (
                <button
                  key={pageNum}
                  onClick={() => onPageChange(pageNum)}
                  className={`relative inline-flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors duration-200 ${
                    pageNum === page
                      ? 'bg-indigo-600 text-white'
                      : 'text-gray-500 bg-white border border-gray-300 hover:bg-gray-50'
                  }`}
                >
                  {pageNum}
                </button>
              ))
            ) : (
              // Show truncated pagination for more than 7 pages
              <>
                <button
                  onClick={() => onPageChange(1)}
                  className={`relative inline-flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors duration-200 ${
                    1 === page
                      ? 'bg-indigo-600 text-white'
                      : 'text-gray-500 bg-white border border-gray-300 hover:bg-gray-50'
                  }`}
                >
                  1
                </button>
                {page > 3 && <span className="px-3 py-2 text-gray-500">...</span>}
                {page > 2 && page < totalPages - 1 && (
                  <button
                    onClick={() => onPageChange(page)}
                    className="relative inline-flex items-center px-3 py-2 text-sm font-medium bg-indigo-600 text-white rounded-lg"
                  >
                    {page}
                  </button>
                )}
                {page < totalPages - 2 && <span className="px-3 py-2 text-gray-500">...</span>}
                <button
                  onClick={() => onPageChange(totalPages)}
                  className={`relative inline-flex items-center px-3 py-2 text-sm font-medium rounded-lg transition-colors duration-200 ${
                    totalPages === page
                      ? 'bg-indigo-600 text-white'
                      : 'text-gray-500 bg-white border border-gray-300 hover:bg-gray-50'
                  }`}
                >
                  {totalPages}
                </button>
              </>
            )}
          </div>
          
          <button 
            onClick={() => onPageChange(Math.min(totalPages || 1, page + 1))} 
            disabled={page >= totalPages}
            className="relative inline-flex items-center px-4 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
          >
            Next
            <svg className="w-4 h-4 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
}
