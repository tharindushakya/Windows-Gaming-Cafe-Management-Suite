import React from 'react';

export default function SimpleModal({ title, children, onClose }) {
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-md w-[600px] max-w-[95%] p-4 shadow-lg">
        <div className="flex justify-between items-center">
          <h3 className="text-lg font-medium m-0">{title}</h3>
          <button onClick={onClose} className="text-gray-600 hover:text-gray-800 px-2">✕</button>
        </div>
        <div className="mt-3">{children}</div>
      </div>
    </div>
  );
}
