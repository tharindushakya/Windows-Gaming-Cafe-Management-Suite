import React from 'react';

export default function ConfirmDialog({ title = 'Confirm', message, onConfirm, onCancel }) {
  return ( 
    <div className="p-4"> 
      <h3 className="text-lg font-semibold mb-2">{title}</h3> 
      <p className="text-sm text-gray-600 mb-4">{message}</p> 
      <div className="flex gap-3"> 
        <button onClick={onCancel} className="px-3 py-2 rounded bg-gray-100 text-sm">Cancel</button> 
        <button onClick={onConfirm} className="px-3 py-2 rounded bg-red-600 text-white text-sm">Confirm</button> 
      </div> 
    </div> 
  );
}
