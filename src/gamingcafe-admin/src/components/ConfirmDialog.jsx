import React from 'react';

export default function ConfirmDialog({ title = 'Confirm', message, onConfirm, onCancel }) {
  return ( 
    <div className="space-y-4"> 
      {title && <h3 className="text-lg font-semibold text-gray-900">{title}</h3>} 
      <p className="text-gray-600">{message}</p> 
      <div className="flex gap-3 justify-end pt-4"> 
        <button 
          onClick={onCancel} 
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 transition-colors duration-200"
        >
          Cancel
        </button> 
        <button 
          onClick={onConfirm} 
          className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-lg hover:bg-red-700 focus:ring-2 focus:ring-red-500 focus:ring-offset-2 transition-colors duration-200"
        >
          Confirm
        </button> 
      </div> 
    </div> 
  );
}
