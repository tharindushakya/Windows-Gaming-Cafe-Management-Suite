import React, { useEffect, useState } from 'react';
import api from '../api';

export default function Loyalty() {
  const [programs, setPrograms] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => { fetchPrograms(); }, []);

  async function fetchPrograms() {
    setLoading(true); setError(null);
    try {
      const res = await api.get('/api/v1.0/loyalty?page=1&pageSize=50');
      const list = res?.data || res?.Data || res || [];
      setPrograms(list);
    } catch (err) {
      setError(err?.data?.message || err.message || 'Failed to load loyalty programs');
    } finally { setLoading(false); }
  }

  return (
    <div style={{ padding: 16 }}>
      <h2>Loyalty Programs</h2>
      <p>Manage loyalty programs and user points.</p>
      {loading && <div>Loading...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}
      <ul>
        {programs.map(p => (
          <li key={p.programId}>{p.name} — {p.pointsRequired} points</li>
        ))}
      </ul>
    </div>
  );
}
