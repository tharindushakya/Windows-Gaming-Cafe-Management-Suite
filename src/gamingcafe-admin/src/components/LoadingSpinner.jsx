import React from 'react';

export default function LoadingSpinner({ size = 24 }) {
  const s = typeof size === 'number' ? `${size}px` : size;
  return (
    <div style={{ width: s, height: s }} className="rounded-full border-4 border-gray-200 border-t-gray-500 animate-spin" />
  );

// CSS injection for animation (simple)
  }
