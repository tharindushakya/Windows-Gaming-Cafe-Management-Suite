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
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">Stations</h1>
            <p className="text-gray-600">Manage PC/Console stations, pricing, and availability</p>
          </div>
          <div className="mt-4 sm:mt-0">
            <button 
              className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg shadow-sm transition-colors duration-200 gap-2"
              onClick={openCreate}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Create Station
            </button>
          </div>
        </div>

        {loading && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
            <div className="inline-flex items-center gap-3 text-gray-500">
              <svg className="animate-spin h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Loading stations...
            </div>
          </div>
        )}
        
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
            <div className="flex items-center gap-2 text-red-700">
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {error}
            </div>
          </div>
        )}

        {/* Stations Table */}
        {!loading && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[720px]">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Station</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Hourly Rate</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {stations.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-6 py-12 text-center text-gray-500">
                        <div className="flex flex-col items-center">
                          <svg className="w-12 h-12 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                          </svg>
                          <p className="text-lg font-medium text-gray-900 mb-1">No stations found</p>
                          <p className="text-gray-500">Create your first station to get started</p>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    stations.map(s => (
                      <tr key={s.stationId ?? s.StationId} className="hover:bg-gray-50 transition-colors duration-200">
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {s.stationId ?? s.StationId}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center">
                            <div className="flex-shrink-0 h-8 w-8">
                              <div className="h-8 w-8 rounded-full bg-indigo-100 flex items-center justify-center">
                                <svg className="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                                </svg>
                              </div>
                            </div>
                            <div className="ml-3">
                              <div className="text-sm font-medium text-gray-900">{s.stationName ?? s.Name}</div>
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                            (s.stationType ?? s.Type) === 'PC' ? 'bg-blue-100 text-blue-800' : 
                            (s.stationType ?? s.Type) === 'Console' ? 'bg-purple-100 text-purple-800' : 
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {s.stationType ?? s.Type ?? 'PC'}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          ${((s.hourlyRate ?? s.HourlyRate ?? 0).toFixed ? (s.hourlyRate ?? s.HourlyRate ?? 0).toFixed(2) : s.hourlyRate ?? s.HourlyRate ?? 0)}/hr
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            s.isAvailable ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                          }`}>
                            <span className={`w-1.5 h-1.5 mr-1.5 rounded-full ${
                              s.isAvailable ? 'bg-green-400' : 'bg-red-400'
                            }`}></span>
                            {s.isAvailable ? 'Available' : 'In Use'}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          <div className="flex items-center justify-end gap-2">
                            <button 
                              className="text-indigo-600 hover:text-indigo-700 font-medium transition-colors duration-200"
                              onClick={() => openEdit(s)}
                            >
                              Edit
                            </button>
                            <button 
                              className={`font-medium transition-colors duration-200 ${
                                s.isAvailable ? 'text-amber-600 hover:text-amber-700' : 'text-green-600 hover:text-green-700'
                              }`}
                              onClick={() => toggleAvailability(s)}
                            >
                              {s.isAvailable ? 'Mark In Use' : 'Mark Available'}
                            </button>
                            <button 
                              className="text-red-600 hover:text-red-700 font-medium transition-colors duration-200"
                              onClick={() => askDelete(s)}
                            >
                              Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Pagination */}
        {!loading && stations.length > 0 && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 px-6 py-4 mt-6">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700">
                Page <span className="font-medium">{page}</span> — <span className="font-medium">{totalCount}</span> stations total
              </div>
              <div className="flex items-center gap-2">
                <button 
                  className="px-3 py-1 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  onClick={prevPage} 
                  disabled={page === 1}
                >
                  Previous
                </button>
                <button 
                  className="px-3 py-1 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  onClick={nextPage} 
                  disabled={page * pageSize >= totalCount}
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Modals */}
        {showModal && (
          <SimpleModal title={editing ? 'Edit Station' : 'Create Station'} onClose={() => setShowModal(false)}>
            <StationForm initial={editing ?? {}} onCancel={() => setShowModal(false)} onSubmit={handleSave} />
          </SimpleModal>
        )}
        
        {confirm && (
          <SimpleModal title="Confirm Delete" onClose={() => setConfirm(null)}>
            <ConfirmDialog 
              message={`Are you sure you want to delete station "${confirm.stationName || confirm.Name || confirm.stationId || confirm.StationId}"? This action cannot be undone.`} 
              onConfirm={() => doDelete(confirm)} 
              onCancel={() => setConfirm(null)} 
            />
          </SimpleModal>
        )}
      </div>
    </div>
  );
}
