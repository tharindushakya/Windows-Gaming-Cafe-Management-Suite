import React from 'react';

export default function LoadingSpinner({ size = 24 }) {
  return (
    <div style={{ width: size, height: size, borderRadius: '50%', border: '3px solid rgba(0,0,0,0.08)', borderTop: '3px solid rgba(0,0,0,0.6)', animation: 'spin 1s linear infinite' }} />
  );
}

// CSS injection for animation (simple)
const style = document.createElement('style');
style.innerHTML = `@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }`;
document.head.appendChild(style);
