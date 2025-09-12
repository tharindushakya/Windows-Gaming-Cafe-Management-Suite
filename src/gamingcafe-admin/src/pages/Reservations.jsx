import React, { useEffect, useState, useCallback } from 'react';
import api from '../api';
import { useToast } from '../components/ToastProvider';

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
  const [submitting, setSubmitting] = useState(false);
  const toast = useToast();

  useEffect(() => {
    fetchLists();
    fetchReservations();
  }, []);

  const fetchReservations = useCallback(async () => {
    setLoading(true);
    try {
      const resp = await api.get(`/api/v1.0/reservations?page=${page}&pageSize=${pageSize}`);
      // support both PagedResponse and direct list
      if (resp && resp.data) {
        setReservations(resp.data);
      } else if (Array.isArray(resp)) {
        setReservations(resp);
      } else {
        setReservations([]);
      }
    } catch (err) {
      setError(err?.message || 'Failed to load reservations');
      toast?.push(err?.message || 'Failed to load reservations', 'error');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, toast]);

  useEffect(() => {
    fetchReservations();
  }, [fetchReservations]);

  async function fetchLists() {
    try {
      const [usersResp, stationsResp] = await Promise.all([
        api.get('/api/v1.0/users'),
        api.get('/api/v1.0/stations')
      ]);
      setUsers(Array.isArray(usersResp) ? usersResp : usersResp?.data || []);
      setStations(Array.isArray(stationsResp) ? stationsResp : stationsResp?.data || []);
    } catch (err) {
      console.error('Failed to load users/stations', err);
      toast?.push('Failed to load users or stations', 'error');
    }
  }

  function handleChange(e) {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
  }

  async function submit(e) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await api.post('/api/v1.0/reservations', {
        ...form,
        userId: Number(form.userId),
        stationId: Number(form.stationId)
      });
      setForm({ userId: '', stationId: '', reservationDate: '', startTime: '', endTime: '', notes: '' });
      fetchReservations();
      toast?.push('Reservation created successfully', 'success');
    } catch (err) {
      setError(err?.data?.message || err?.message || 'Failed to create reservation');
      toast?.push(err?.data?.message || err?.message || 'Failed to create reservation', 'error');
    } finally {
      setSubmitting(false);
    }
  }

  async function cancelReservation(id, reservationData) {
    if (!window.confirm(`Cancel reservation for ${reservationData?.username || 'this user'}?`)) return;
    try {
      await api.post(`/api/v1.0/reservations/${id}/cancel`, { reason: 'Cancelled by admin' });
      fetchReservations();
      toast?.push('Reservation cancelled successfully', 'success');
    } catch (err) {
      setError(err?.message || 'Failed to cancel');
      toast?.push(err?.message || 'Failed to cancel', 'error');
    }
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900">Reservations</h2>
        <p className="text-gray-600 mt-2">Manage station reservations and bookings.</p>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-700">
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
            {error}
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Create Reservation Form */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-lg shadow">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-medium text-gray-900">Create Reservation</h3>
            </div>
            <form onSubmit={submit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">User</label>
                <select 
                  name="userId" 
                  value={form.userId} 
                  onChange={handleChange} 
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                >
                  <option value="">Select user</option>
                  {users.map(u => (
                    <option key={u.userId || u.id} value={u.userId || u.id}>
                      {u.username || u.email || `User ${u.userId || u.id}`}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Station</label>
                <select 
                  name="stationId" 
                  value={form.stationId} 
                  onChange={handleChange} 
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                >
                  <option value="">Select station</option>
                  {stations.map(s => (
                    <option key={s.stationId || s.id} value={s.stationId || s.id}>
                      {s.stationName || s.name || `Station ${s.stationId || s.id}`} - ${(s.hourlyRate || 0).toFixed(2)}/hr
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Date</label>
                <input 
                  name="reservationDate" 
                  type="date" 
                  value={form.reservationDate} 
                  onChange={handleChange} 
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Start Time</label>
                <input 
                  name="startTime" 
                  type="time" 
                  value={form.startTime} 
                  onChange={handleChange} 
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">End Time</label>
                <input 
                  name="endTime" 
                  type="time" 
                  value={form.endTime} 
                  onChange={handleChange} 
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Notes</label>
                <textarea 
                  name="notes" 
                  value={form.notes} 
                  onChange={handleChange}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
                  placeholder="Optional notes or special requests..."
                />
              </div>

              <button 
                type="submit" 
                disabled={submitting}
                className="w-full inline-flex items-center justify-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
              >
                {submitting ? (
                  <>
                    <svg className="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    Creating...
                  </>
                ) : (
                  <>
                    <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                    </svg>
                    Create Reservation
                  </>
                )}
              </button>
            </form>
          </div>
        </div>

        {/* Reservations List */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-lg shadow">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-medium text-gray-900">Reservations List</h3>
            </div>
            <div className="overflow-hidden">
              {loading ? (
                <div className="text-center py-12">
                  <svg className="animate-spin mx-auto h-8 w-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  <p className="mt-2 text-sm text-gray-500">Loading reservations...</p>
                </div>
              ) : reservations.length === 0 ? (
                <div className="text-center py-12">
                  <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3a4 4 0 118 0v4m-4 8a1 1 0 11-2 0 1 1 0 012 0zm6 0a1 1 0 11-2 0 1 1 0 012 0z" />
                  </svg>
                  <p className="mt-2 text-sm text-gray-500">No reservations found</p>
                  <p className="text-xs text-gray-400">Create your first reservation to see it here</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Station</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date & Time</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Cost</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {reservations.map(r => (
                        <tr key={r.reservationId} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            #{r.reservationId}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm font-medium text-gray-900">
                              {r.username || `User ${r.userId}`}
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm text-gray-900">
                              {r.stationName || `Station ${r.stationId}`}
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div className="text-sm text-gray-900">
                              {new Date(r.reservationDate).toLocaleDateString()}
                            </div>
                            <div className="text-sm text-gray-500">
                              {formatDate(r.startTime)} - {formatDate(r.endTime)}
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            ${(r.estimatedCost || 0).toFixed(2)}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                              r.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                              r.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                              r.status === 'Completed' ? 'bg-blue-100 text-blue-800' :
                              'bg-yellow-100 text-yellow-800'
                            }`}>
                              {r.status}
                            </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                            {r.status !== 'Cancelled' && r.status !== 'Completed' && (
                              <button 
                                onClick={() => cancelReservation(r.reservationId, r)}
                                className="text-red-600 hover:text-red-900 transition-colors duration-200"
                              >
                                Cancel
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {/* Pagination */}
              {reservations.length > 0 && (
                <div className="bg-white px-4 py-3 border-t border-gray-200 sm:px-6">
                  <div className="flex items-center justify-between">
                    <div className="flex-1 flex justify-between sm:hidden">
                      <button 
                        onClick={() => setPage(p => Math.max(1, p - 1))} 
                        disabled={page === 1}
                        className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Previous
                      </button>
                      <button 
                        onClick={() => setPage(p => p + 1)} 
                        disabled={reservations.length < pageSize}
                        className="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Next
                      </button>
                    </div>
                    <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
                      <div>
                        <p className="text-sm text-gray-700">
                          Page <span className="font-medium">{page}</span>
                        </p>
                      </div>
                      <div>
                        <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px">
                          <button
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                            className="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                          >
                            <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                              <path fillRule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clipRule="evenodd" />
                            </svg>
                          </button>
                          <button
                            onClick={() => setPage(p => p + 1)}
                            disabled={reservations.length < pageSize}
                            className="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                          >
                            <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                              <path fillRule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clipRule="evenodd" />
                            </svg>
                          </button>
                        </nav>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
