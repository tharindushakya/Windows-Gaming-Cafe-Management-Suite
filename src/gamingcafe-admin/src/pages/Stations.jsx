import React, { useEffect, useState, useCallback } from 'react';
import api from '../api';
import { useToast } from '../components/ToastProvider';
import SimpleModal from '../components/SimpleModal';
import StationForm from '../components/StationForm';
import ConfirmDialog from '../components/ConfirmDialog';

export default function Stations() {
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [confirm, setConfirm] = useState(null);
  const toast = useToast();

  const fetchStations = useCallback(async (p = 1) => {
    setLoading(true); setError(null);
    try {
      const res = await api.get(`/api/v1.0/stations?page=${p}&pageSize=${pageSize}`);
      const list = res?.data || res?.Data || res || [];
      setStations(list);
      setTotalCount(res?.totalCount ?? res?.TotalCount ?? list.length);
    } catch (err) {
      setError(err?.data?.message || err.message || 'Failed to load stations');
    } finally { setLoading(false); }
  }, [pageSize]);

  useEffect(() => { fetchStations(page); }, [fetchStations, page]);

  function openCreate() { setEditing(null); setShowModal(true); }
  function openEdit(s) { setEditing(s); setShowModal(true); }

  async function handleSave(payload) {
    try {
      if (editing && (editing.stationId || editing.StationId)) {
        const id = editing.stationId ?? editing.StationId;
        await api.put(`/api/v1.0/stations/${id}`, payload);
        toast.push('Station updated', 'success');
      } else {
        await api.post('/api/v1.0/stations', payload);
        toast.push('Station created', 'success');
      }
      setShowModal(false);
      fetchStations(page);
    } catch (err) {
      toast.push(err?.data?.message || err.message || 'Failed to save station', 'error');
    }
  }

  function askDelete(s) { setConfirm(s); }

  async function doDelete(s) {
    try {
      const id = s.stationId ?? s.StationId;
      await api.del(`/api/v1.0/stations/${id}`);
      setConfirm(null);
      toast.push('Station deleted', 'success');
      fetchStations(page);
    } catch (err) {
      toast.push(err?.data?.message || err.message || 'Failed to delete station', 'error');
    }
  }

  async function toggleAvailability(s) {
    try {
      const id = s.stationId ?? s.StationId;
      // optimistic UI
      setStations(prev => prev.map(x => x.stationId === id || x.StationId === id ? { ...x, isAvailable: !x.isAvailable } : x));
      await api.post(`/api/v1.0/stations/${id}/toggle-availability`);
      toast.push('Station availability updated', 'success');
      fetchStations(page);
    } catch (err) {
      toast.push(err?.data?.message || err.message || 'Failed to update availability', 'error');
      fetchStations(page);
    }
  }

  function nextPage() { if (page * pageSize < totalCount) setPage(p => p + 1); }
  function prevPage() { if (page > 1) setPage(p => p - 1); }

  return (
    <div className="p-4">
      <h2 className="text-2xl font-semibold text-gray-100">Stations</h2>
      <p className="text-sm text-gray-300">Manage PC/Console stations; create, edit, delete and toggle availability.</p>

      {loading && <div className="mt-3 text-gray-300">Loading stations...</div>}
      {error && <div className="mt-3 text-red-400">{error}</div>}

      <div className="my-3">
        <button onClick={openCreate} className="px-3 py-1.5 rounded bg-teal-500 hover:bg-teal-600 text-white text-sm">Create station</button>
      </div>

      {!loading && !error && (
        <>
          <div className="overflow-x-auto bg-gray-900 border border-gray-800 rounded">
            <table className="min-w-full w-full divide-y divide-gray-800">
              <thead className="bg-gray-900">
                <tr>
                  <th className="text-left px-4 py-3 text-sm text-gray-300">ID</th>
                  <th className="text-left px-4 py-3 text-sm text-gray-300">Name</th>
                  <th className="text-left px-4 py-3 text-sm text-gray-300">Type</th>
                  <th className="text-right px-4 py-3 text-sm text-gray-300">Hourly</th>
                  <th className="text-left px-4 py-3 text-sm text-gray-300">Status</th>
                  <th className="text-right px-4 py-3 text-sm text-gray-300">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-gray-900 divide-y divide-gray-800">
                {stations.length === 0 && (
                  <tr><td colSpan={6} className="px-4 py-6 text-gray-400">No stations found.</td></tr>
                )}
                {stations.map(s => (
                  <tr key={s.stationId ?? s.StationId} className="hover:bg-gray-800">
                    <td className="px-4 py-3 text-sm text-gray-200">{s.stationId ?? s.StationId}</td>
                    <td className="px-4 py-3 text-sm text-gray-200">{s.stationName ?? s.Name}</td>
                    <td className="px-4 py-3 text-sm text-gray-200">{s.stationType ?? s.Type}</td>
                    <td className="px-4 py-3 text-sm text-gray-200 text-right">{(s.hourlyRate ?? s.HourlyRate ?? 0).toFixed ? (s.hourlyRate ?? s.HourlyRate ?? 0).toFixed(2) : s.hourlyRate ?? s.HourlyRate ?? 0}</td>
                    <td className={`px-4 py-3 text-sm ${s.isAvailable ? 'text-emerald-400' : 'text-red-400'}`}>{s.isAvailable ? 'Available' : 'In use'}</td>
                    <td className="px-4 py-3 text-sm text-right">
                      <button onClick={() => openEdit(s)} className="mr-2 px-2 py-1 text-sm rounded bg-gray-800 border border-gray-700 text-gray-200">Edit</button>
                      <button onClick={() => toggleAvailability(s)} className="mr-2 px-2 py-1 text-sm rounded bg-gray-800 border border-gray-700 text-gray-200">{s.isAvailable ? 'Mark in use' : 'Mark available'}</button>
                      <button onClick={() => askDelete(s)} className="px-2 py-1 text-sm rounded bg-red-700 hover:bg-red-600 text-white">Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-3 flex items-center justify-between text-sm text-gray-300">
            <div>Page {page} — {totalCount} stations</div>
            <div>
              <button onClick={prevPage} disabled={page === 1} className="mr-2 px-2 py-1 rounded bg-gray-800 border border-gray-700 text-gray-200 disabled:opacity-50">Prev</button>
              <button onClick={nextPage} disabled={page * pageSize >= totalCount} className="px-2 py-1 rounded bg-gray-800 border border-gray-700 text-gray-200 disabled:opacity-50">Next</button>
            </div>
          </div>
        </>
      )}

      {showModal && (
        <SimpleModal title={editing ? 'Edit station' : 'Create station'} onClose={() => setShowModal(false)}>
          <StationForm initial={editing ?? {}} onCancel={() => setShowModal(false)} onSubmit={handleSave} />
        </SimpleModal>
      )}

      {confirm && (
        <SimpleModal title="Confirm delete" onClose={() => setConfirm(null)}>
          <ConfirmDialog message={`Delete station ${confirm.stationName || confirm.Name || confirm.stationId || confirm.StationId}?`} onConfirm={() => doDelete(confirm)} onCancel={() => setConfirm(null)} />
        </SimpleModal>
      )}
    </div>
  );
}
