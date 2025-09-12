import React, { useState, useEffect } from 'react';
import { useToast } from '../components/ToastProvider';
import SimpleModal from '../components/SimpleModal';
import ConfirmDialog from '../components/ConfirmDialog';
import api from '../api';

export default function Dashboard() {
  const [stats, setStats] = useState({
    activeStations: 0,
    totalRevenue: 0,
    onlineUsers: 0,
    occupancyRate: 0
  });
  const [activeSessions, setActiveSessions] = useState([]);
  const [systemAlerts, setSystemAlerts] = useState([]);
  const [loading, setLoading] = useState(true);
  const toast = useToast();
  const [showSessionModal, setShowSessionModal] = useState(false);
  const [selectedSession, setSelectedSession] = useState(null);
  const [showConfirm, setShowConfirm] = useState(false);
  const [confirmAction, setConfirmAction] = useState(null);

  useEffect(() => {
    loadDashboardData();
    const interval = setInterval(loadDashboardData, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, []);

  async function loadDashboardData() {
    try {
      // Try to fetch live dashboard stats and active sessions
      const [statsData, activeSessionsData] = await Promise.all([
        api.get('/api/v1.0/reports/dashboard').catch(() => null),
        api.get('/api/v1.0/gamesessions/active').catch(() => [])
      ]);

      if (statsData) {
        setStats({
          activeStations: statsData.activeStations || 0,
          totalRevenue: statsData.totalRevenue || 0,
          onlineUsers: statsData.activeUsers || 0,
          occupancyRate: statsData.stationUtilization ? Math.round(statsData.stationUtilization) : 0
        });
      }

      setActiveSessions(Array.isArray(activeSessionsData) ? activeSessionsData.map(s => ({
        id: s.sessionId,
        station: s.stationName,
        user: s.username,
        timeElapsed: s.duration ? s.duration : '00:00:00',
        game: s.notes || '—'
      })) : []);

      // Attempt to fetch alerts if endpoint exists, fallback to small mock
      const alerts = await api.get('/api/v1.0/alerts').catch(() => ([
        { id: 1, type: 'warning', message: 'PC-005 high temperature detected', time: '5 min ago' },
        { id: 2, type: 'info', message: 'New customer registration: alex_new', time: '12 min ago' }
      ]));
      setSystemAlerts(Array.isArray(alerts) ? alerts.slice(0,6) : []);

      setLoading(false);
    } catch (error) {
      console.error('Failed to load dashboard data:', error);
      setLoading(false);
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto mb-4"></div>
            <div className="text-gray-500 text-lg">Loading dashboard...</div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-6 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">Dashboard</h1>
            <p className="text-gray-600">Real-time overview of your gaming café</p>
          </div>
          <div className="mt-4 sm:mt-0">
            <button 
              className="inline-flex items-center px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg shadow-sm transition-colors duration-200 gap-2"
              onClick={() => { setSelectedSession(null); setShowSessionModal(true); }}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              New Session
            </button>
          </div>
        </div>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Active Stations</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">{stats.activeStations}<span className="text-lg text-gray-500">/16</span></p>
                <p className="text-sm text-emerald-600 mt-1 flex items-center">
                  <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M5.293 9.707a1 1 0 010-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 01-1.414 1.414L11 7.414V15a1 1 0 11-2 0V7.414L6.707 9.707a1 1 0 01-1.414 0z" clipRule="evenodd" />
                  </svg>
                  +2 from yesterday
                </p>
              </div>
              <div className="p-3 bg-indigo-100 rounded-lg">
                <svg className="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>
            </div>
          </div>
          
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Today's Revenue</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">${stats.totalRevenue.toFixed(2)}</p>
                <p className="text-sm text-emerald-600 mt-1 flex items-center">
                  <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M5.293 9.707a1 1 0 010-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 01-1.414 1.414L11 7.414V15a1 1 0 11-2 0V7.414L6.707 9.707a1 1 0 01-1.414 0z" clipRule="evenodd" />
                  </svg>
                  +15.3% from yesterday
                </p>
              </div>
              <div className="p-3 bg-emerald-100 rounded-lg">
                <svg className="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
                </svg>
              </div>
            </div>
          </div>
          
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Online Users</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">{stats.onlineUsers}</p>
                <p className="text-sm text-emerald-600 mt-1 flex items-center">
                  <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M5.293 9.707a1 1 0 010-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 01-1.414 1.414L11 7.414V15a1 1 0 11-2 0V7.414L6.707 9.707a1 1 0 01-1.414 0z" clipRule="evenodd" />
                  </svg>
                  +3 in last hour
                </p>
              </div>
              <div className="p-3 bg-blue-100 rounded-lg">
                <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
                </svg>
              </div>
            </div>
          </div>
          
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow duration-200">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Occupancy Rate</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">{stats.occupancyRate}%</p>
                <p className="text-sm text-amber-600 mt-1 flex items-center">
                  <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
                  </svg>
                  Peak hours
                </p>
              </div>
              <div className="p-3 bg-amber-100 rounded-lg">
                <svg className="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
              </div>
            </div>
          </div>
        </div>

        {/* Main Content Grid */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          {/* Active Sessions */}
          <div className="xl:col-span-2 bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Active Sessions</h3>
              <button className="text-sm text-indigo-600 hover:text-indigo-700 font-medium">
                View All
              </button>
            </div>
            
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Station</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Game</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Duration</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {activeSessions.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                        <div className="flex flex-col items-center">
                          <svg className="w-12 h-12 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                          </svg>
                          <p className="text-lg font-medium text-gray-900 mb-1">No active sessions</p>
                          <p className="text-gray-500">Start a new session to see it here</p>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    activeSessions.map(session => (
                      <tr key={session.id} className="hover:bg-gray-50 transition-colors duration-200">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="flex items-center">
                            <div className="flex-shrink-0 w-2 h-2 bg-emerald-400 rounded-full mr-3"></div>
                            <span className="text-sm font-medium text-gray-900">{session.station}</span>
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{session.user}</td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{session.game}</td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-900">{session.timeElapsed}</td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm">
                          <button 
                            className="text-indigo-600 hover:text-indigo-700 font-medium"
                            onClick={() => { setSelectedSession(session); setShowSessionModal(true); }}
                          >
                            Manage
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* System Alerts */}
          <div className="bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="p-6 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">System Alerts</h3>
            </div>
            
            <div className="p-6">
              <div className="space-y-4">
                {systemAlerts.length === 0 ? (
                  <div className="text-center py-8">
                    <svg className="w-12 h-12 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5-5-5h5v-12a3 3 0 015.196-2.196l.707-.707A1 1 0 0117.071.929l-.707.707A5 5 0 009 7v10z" />
                    </svg>
                    <p className="text-gray-500">All systems running smoothly</p>
                  </div>
                ) : (
                  systemAlerts.map(alert => (
                    <div key={alert.id} className={`p-4 rounded-lg border-l-4 ${
                      alert.type === 'warning' ? 'bg-amber-50 border-amber-400' : 
                      alert.type === 'success' ? 'bg-emerald-50 border-emerald-400' : 
                      'bg-blue-50 border-blue-400'
                    }`}>
                      <div className="flex items-start">
                        <div className={`flex-shrink-0 w-5 h-5 rounded-full mt-0.5 mr-3 ${
                          alert.type === 'warning' ? 'bg-amber-400' : 
                          alert.type === 'success' ? 'bg-emerald-400' : 
                          'bg-blue-400'
                        }`}></div>
                        <div className="flex-1">
                          <p className="text-sm font-medium text-gray-900">{alert.message}</p>
                          <p className="text-xs text-gray-500 mt-1">{alert.time}</p>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
              
              <button 
                className="w-full mt-6 px-4 py-2 text-sm font-medium text-gray-700 bg-gray-50 hover:bg-gray-100 border border-gray-300 rounded-lg transition-colors duration-200"
                onClick={() => toast.push('Opening alerts panel (mock)', 'info')}
              >
                View All Alerts
              </button>
            </div>
        </div>
      </div>

      {/* Session Modal (create or manage) */}
      {showSessionModal && (
        <SimpleModal 
          title={selectedSession ? `Manage ${selectedSession.station}` : 'Create Session'} 
          onClose={() => { setShowSessionModal(false); setSelectedSession(null); }}
        >
          <div className="p-6">
            {selectedSession ? (
              <div className="space-y-4">
                <div className="bg-gray-50 rounded-lg p-4">
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <span className="font-medium text-gray-500">Station:</span>
                      <div className="text-gray-900 font-medium">{selectedSession.station}</div>
                    </div>
                    <div>
                      <span className="font-medium text-gray-500">User:</span>
                      <div className="text-gray-900 font-medium">{selectedSession.user}</div>
                    </div>
                  </div>
                </div>
                <div className="flex gap-3">
                  <button 
                    className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg transition-colors duration-200"
                    onClick={() => { setConfirmAction({ type: 'pause', label: 'Pause session', session: selectedSession }); setShowConfirm(true); }}
                  >
                    Pause
                  </button>
                  <button 
                    className="px-4 py-2 bg-emerald-100 hover:bg-emerald-200 text-emerald-700 text-sm font-medium rounded-lg transition-colors duration-200"
                    onClick={() => { setConfirmAction({ type: 'resume', label: 'Resume session', session: selectedSession }); setShowConfirm(true); }}
                  >
                    Resume
                  </button>
                  <button 
                    className="px-4 py-2 bg-red-100 hover:bg-red-200 text-red-700 text-sm font-medium rounded-lg transition-colors duration-200"
                    onClick={() => { setConfirmAction({ type: 'end', label: 'End session', session: selectedSession }); setShowConfirm(true); }}
                  >
                    End Session
                  </button>
                </div>
              </div>
            ) : (
              <CreateSessionForm onStarted={() => { setShowSessionModal(false); loadDashboardData(); toast.push('Session started', 'success'); }} />
            )}
          </div>
        </SimpleModal>
      )}

      {/* Confirm Modal for destructive actions */}
      {showConfirm && confirmAction && (
        <SimpleModal title={confirmAction.label} onClose={() => { setShowConfirm(false); setConfirmAction(null); }}>
          <ConfirmDialog
            message={`Are you sure you want to ${confirmAction.label.toLowerCase()} for ${confirmAction.session.station}?`}
            onCancel={() => { setShowConfirm(false); setConfirmAction(null); }}
            onConfirm={async () => {
              try {
                const id = confirmAction.session.id;
                if (confirmAction.type === 'pause') await api.post(`/api/v1.0/gamesessions/${id}/pause`);
                if (confirmAction.type === 'resume') await api.post(`/api/v1.0/gamesessions/${id}/resume`);
                if (confirmAction.type === 'end') await api.post(`/api/v1.0/gamesessions/${id}/end`);
                toast.push(`${confirmAction.label} successful`, 'success');
                setShowConfirm(false);
                setConfirmAction(null);
                setShowSessionModal(false);
                setSelectedSession(null);
                await loadDashboardData();
              } catch (err) {
                toast.push(err.message || `${confirmAction.label} failed`, 'error');
              }
            }}
          />
        </SimpleModal>
      )}
      </div>
    </div>
  );
}

// Session create/manage form used by the modal
function CreateSessionForm({ onStarted }) {
  const [stationId, setStationId] = useState('');
  const [userId, setUserId] = useState('');
  const [stations, setStations] = useState([]);
  const [users, setUsers] = useState([]);
  const [loadingChoices, setLoadingChoices] = useState(true);
  const toast = useToast();

  useEffect(() => {
    let mounted = true;
    async function loadChoices() {
      setLoadingChoices(true);
      try {
        // Prefer available stations, fallback to full stations list
        let stationsResp = await api.get('/api/v1.0/stations/available').catch(() => null);
        if (!stationsResp || (Array.isArray(stationsResp) && stationsResp.length === 0)) {
          stationsResp = await api.get('/api/v1.0/stations').catch(() => []);
        }

        // Normalize stations response: support array or { data: [...] }
        if (stationsResp && !Array.isArray(stationsResp)) {
          stationsResp = stationsResp.data ?? stationsResp.items ?? stationsResp.results ?? stationsResp;
        }

        // Users endpoint may return { data: [...] } or { items: [...] } or an array
        let usersResp = await api.get('/api/v1.0/users?pageSize=50').catch(() => null);
        if (usersResp && !Array.isArray(usersResp)) {
          usersResp = usersResp.data ?? usersResp.items ?? usersResp.results ?? usersResp;
        }
        if (!usersResp || !Array.isArray(usersResp) || usersResp.length === 0) {
          const fallback = await api.get('/api/v1.0/users').catch(() => []);
          usersResp = Array.isArray(fallback) ? fallback : (fallback?.data ?? fallback?.items ?? fallback ?? []);
        }

        if (!mounted) return;
        setStations(Array.isArray(stationsResp) ? stationsResp : []);
        setUsers(Array.isArray(usersResp) ? usersResp : []);
      } catch (err) {
        console.error('Failed to load stations/users', err);
        toast.push('Failed to load stations or users', 'error');
      } finally {
        if (mounted) setLoadingChoices(false);
      }
    }

    loadChoices();
    return () => { mounted = false; };
  }, [toast]);

  async function start() {
    try {
      if (!stationId || !userId) {
        toast.push('Select station and user', 'error');
        return;
      }
      await api.post('/api/v1.0/gamesessions/start', { stationId: parseInt(stationId, 10), userId: parseInt(userId, 10) });
      onStarted?.();
    } catch (err) {
      toast.push(err.message || 'Failed to start session', 'error');
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Station</label>
        <select 
          value={stationId} 
          onChange={e => setStationId(e.target.value)} 
          disabled={loadingChoices}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
        >
          <option value="">-- select station --</option>
          {stations.map(s => {
            // id field may vary: stationId, id, StationId
            const id = s?.stationId ?? s?.id ?? s?.StationId ?? s?.stationID ?? s?.stationId;
            const name = s?.stationName ?? s?.name ?? s?.station_name ?? s?.station ?? s?.displayName;
            const label = name || (id ? `Station ${id}` : 'Unnamed station');
            return <option key={id ?? label} value={id ?? ''}>{label}</option>;
          })}
        </select>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">User</label>
        <select 
          value={userId} 
          onChange={e => setUserId(e.target.value)} 
          disabled={loadingChoices}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
        >
          <option value="">-- select user --</option>
          {users.map(u => {
            const id = u?.userId ?? u?.id ?? u?.UserId ?? u?.userID ?? u?.userId;
            const name = u?.username ?? u?.userName ?? u?.name ?? u?.displayName ?? u?.email;
            const label = name || (id ? `User ${id}` : 'Unknown user');
            return <option key={id ?? label} value={id ?? ''}>{label}</option>;
          })}
        </select>
      </div>

      <div className="flex gap-3 pt-4">
        <button 
          className="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          onClick={start} 
          disabled={loadingChoices || !stationId || !userId}
        >
          {loadingChoices ? 'Loading...' : 'Start Session'}
        </button>
        <button 
          className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg transition-colors duration-200"
          onClick={() => { setStationId(''); setUserId(''); }}
        >
          Clear
        </button>
      </div>
    </div>
  );
}