import React, { useEffect, useState } from 'react';
import api from '../api';

export default function StationsList() {
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => { fetchStations(); }, []);

  async function fetchStations() {
    setLoading(true); setError(null);
    try {
      const res = await api.get('/api/v1.0/stations');
      setStations(res || []);
    } catch (err) {
      setError(err?.data?.message || err.message || 'Failed to load stations');
    } finally { setLoading(false); }
  }

  return (
    <div style={{ padding: 16 }}>
      <h2>Stations</h2>
      <p>View and manage gaming stations.</p>
      {loading && <div>Loading stations...</div>}
      {error && <div style={{ color: 'red' }}>{error}</div>}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(220px,1fr))', gap: 12, marginTop: 12 }}>
        {stations.map(s => (
          <div key={s.stationId} style={{ padding: 12, borderRadius: 8, boxShadow: '0 6px 18px rgba(2,6,23,0.06)', background: '#fff' }}>
            <div style={{ fontWeight: 700 }}>{s.stationName}</div>
            <div style={{ color: '#555', marginTop: 6 }}>{s.stationType} • ${s.hourlyRate?.toFixed?.(2)}</div>
            <div style={{ marginTop: 8, color: s.isAvailable ? '#10b981' : '#ef4444' }}>{s.isAvailable ? 'Available' : 'In use'}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
