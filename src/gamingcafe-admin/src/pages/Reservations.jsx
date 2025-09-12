import React, { useEffect, useState, useCallback } from 'react';
import api from '../api';

function formatDate(d) {
  if (!d) return '';
  const dt = new Date(d);
  return dt.toLocaleString();
}

export default function Reservations() {
  const [reservations, setReservations] = useState([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [loading, setLoading] = useState(false);

  // form
  const [users, setUsers] = useState([]);
  const [stations, setStations] = useState([]);
  const [form, setForm] = useState({ userId: '', stationId: '', reservationDate: '', startTime: '', endTime: '', notes: '' });
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchLists();
  }, []);

  const fetchReservations = useCallback(async () => {
    setLoading(true);
    try {
      const resp = await api.get(`/api/v1.0/reservations?page=${page}&pageSize=${pageSize}`);
      // support both PagedResponse and direct list
      if (resp && resp.data) {
        setReservations(resp.data);
        // eslint-disable-next-line no-unused-vars
        // setTotal(resp.totalCount || 0);
      } else if (Array.isArray(resp)) {
        setReservations(resp);
        // eslint-disable-next-line no-unused-vars
        // setTotal(resp.length);
      } else {
        setReservations([]);
        // eslint-disable-next-line no-unused-vars
        // setTotal(0);
      }
    } catch (err) {
      setError(err?.message || 'Failed to load reservations');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize]);

  useEffect(() => {
    fetchReservations();
  }, [fetchReservations]);

  async function fetchLists() {
    try {
      const usersResp = await api.get('/api/v1.0/users?page=1&pageSize=100');
      setUsers(usersResp.data || usersResp);
    } catch (e) {
      // ignore
    }
    try {
      const stationsResp = await api.get('/api/v1.0/stations?page=1&pageSize=200');
      setStations(stationsResp.data || stationsResp);
    } catch (e) {
      // ignore
    }
  }

  function handleChange(e) {
    const { name, value } = e.target;
    setForm(f => ({ ...f, [name]: value }));
  }

  async function submit(e) {
    e.preventDefault();
    setError(null);
    try {
      // combine date + times into full Date objects in ISO
      const date = new Date(form.reservationDate);
      const [sH, sM] = form.startTime.split(':');
      const [eH, eM] = form.endTime.split(':');
      const start = new Date(date);
      start.setHours(Number(sH||0), Number(sM||0), 0, 0);
      const end = new Date(date);
      end.setHours(Number(eH||0), Number(eM||0), 0, 0);

      const body = {
        userId: Number(form.userId),
        stationId: Number(form.stationId),
        reservationDate: date.toISOString(),
        startTime: start.toISOString(),
        endTime: end.toISOString(),
        notes: form.notes
      };

  await api.post('/api/v1.0/reservations', body);
      // refresh
      setForm({ userId: '', stationId: '', reservationDate: '', startTime: '', endTime: '', notes: '' });
      fetchReservations();
      fetchLists();
    } catch (err) {
      setError(err?.data?.message || err?.message || 'Failed to create reservation');
    }
  }

  async function cancelReservation(id) {
    if (!window.confirm('Cancel this reservation?')) return;
    try {
      await api.post(`/api/v1.0/reservations/${id}/cancel`, { reason: 'Cancelled by admin' });
      fetchReservations();
    } catch (err) {
      setError(err?.message || 'Failed to cancel');
    }
  }

  return (
    <div>
      <h2>Reservations</h2>
      <p>Manage reservations here.</p>

      {error && <div style={{ color: 'red' }}>{error}</div>}

      <section style={{ marginTop: 12 }}>
        <h3>Create Reservation</h3>
        <form onSubmit={submit}>
          <div>
            <label>User</label>
            <select name="userId" value={form.userId} onChange={handleChange} required>
              <option value="">Select user</option>
              {users.map(u => <option key={u.userId || u.userId} value={u.userId}>{u.username || u.email}</option>)}
            </select>
          </div>
          <div>
            <label>Station</label>
            <select name="stationId" value={form.stationId} onChange={handleChange} required>
              <option value="">Select station</option>
              {stations.map(s => <option key={s.stationId} value={s.stationId}>{s.stationName} - ${s.hourlyRate}</option>)}
            </select>
          </div>
          <div>
            <label>Date</label>
            <input name="reservationDate" type="date" value={form.reservationDate} onChange={handleChange} required />
          </div>
          <div>
            <label>Start Time</label>
            <input name="startTime" type="time" value={form.startTime} onChange={handleChange} required />
          </div>
          <div>
            <label>End Time</label>
            <input name="endTime" type="time" value={form.endTime} onChange={handleChange} required />
          </div>
          <div>
            <label>Notes</label>
            <input name="notes" value={form.notes} onChange={handleChange} />
          </div>
          <div style={{ marginTop: 8 }}>
            <button type="submit">Create</button>
          </div>
        </form>
      </section>

      <section style={{ marginTop: 24 }}>
        <h3>Reservations List</h3>
        {loading ? <div>Loading...</div> : (
          <>
            <table border="1" cellPadding={6} cellSpacing={0} style={{ width: '100%' }}>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>User</th>
                  <th>Station</th>
                  <th>Date</th>
                  <th>Start</th>
                  <th>End</th>
                  <th>Cost</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {reservations.map(r => (
                  <tr key={r.reservationId}>
                    <td>{r.reservationId}</td>
                    <td>{r.username || r.userId}</td>
                    <td>{r.stationName || r.stationId}</td>
                    <td>{new Date(r.reservationDate).toLocaleDateString()}</td>
                    <td>{formatDate(r.startTime)}</td>
                    <td>{formatDate(r.endTime)}</td>
                    <td>${(r.estimatedCost || 0).toFixed(2)}</td>
                    <td>{r.status}</td>
                    <td>
                      {r.status !== 'Cancelled' && r.status !== 'Completed' && (
                        <button onClick={() => cancelReservation(r.reservationId)}>Cancel</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div style={{ marginTop: 8 }}>
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>Prev</button>
              <span style={{ margin: '0 8px' }}>Page {page}</span>
              <button onClick={() => setPage(p => p + 1)} disabled={reservations.length < pageSize}>Next</button>
            </div>
          </>
        )}
      </section>
    </div>
  );
}
