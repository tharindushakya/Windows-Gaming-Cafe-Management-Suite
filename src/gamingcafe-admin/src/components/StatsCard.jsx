import React from 'react';

export default function StatsCard({ title, value }) {
  return (
    <div className="dc-card">
      <div className="dc-card-title">{title}</div>
      <div className="dc-card-value">{value}</div>
    </div>
  );
}
