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
      <div className="dc-dashboard">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '200px' }}>
          <div style={{ color: 'var(--text-secondary)' }}>Loading dashboard...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="dc-dashboard">
      <div className="dc-welcome">
        <div>
          <h1>Dashboard</h1>
          <p className="dc-subtitle">Real-time overview of your gaming café</p>
        </div>
        <div>
          <button className="btn btn-primary" onClick={() => { setSelectedSession(null); setShowSessionModal(true); }}>
            <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
              <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z"/>
            </svg>
            New Session
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="dc-stats">
        <div className="dc-card">
          <div className="dc-card-title">Active Stations</div>
          <div className="dc-card-value">{stats.activeStations}/16</div>
          <div className="dc-card-change positive">+2 from yesterday</div>
        </div>
        
        <div className="dc-card">
          <div className="dc-card-title">Today's Revenue</div>
          <div className="dc-card-value">${stats.totalRevenue.toFixed(2)}</div>
          <div className="dc-card-change positive">+15.3% from yesterday</div>
        </div>
        
        <div className="dc-card">
          <div className="dc-card-title">Online Users</div>
          <div className="dc-card-value">{stats.onlineUsers}</div>
          <div className="dc-card-change positive">+3 in last hour</div>
        </div>
        
        <div className="dc-card">
          <div className="dc-card-title">Occupancy Rate</div>
          <div className="dc-card-value">{stats.occupancyRate}%</div>
          <div className="dc-card-change positive">Peak hours</div>
        </div>
      </div>

      {/* Active Sessions and Alerts */}
      <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '24px', marginTop: '24px' }}>
        {/* Active Sessions */}
        <div className="dc-card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <h3 style={{ margin: 0, color: 'var(--text-primary)', fontSize: '18px', fontWeight: 600 }}>Active Sessions</h3>
            <button className="btn btn-secondary" style={{ fontSize: '12px', padding: '6px 12px' }}>View All</button>
          </div>
          
          <div className="data-table">
            <table style={{ width: '100%' }}>
              <thead>
                <tr>
                  <th>Station</th>
                  <th>User</th>
                  <th>Game</th>
                  <th>Duration</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {activeSessions.map(session => (
                  <tr key={session.id}>
                    <td>
                      <span className="status-indicator status-online">
                        {session.station}
                      </span>
                    </td>
                    <td style={{ fontWeight: 500 }}>{session.user}</td>
                    <td style={{ color: 'var(--text-secondary)' }}>{session.game}</td>
                    <td style={{ fontFamily: 'JetBrains Mono, monospace' }}>{session.timeElapsed}</td>
                    <td>
                      <button className="btn btn-secondary" style={{ fontSize: '12px', padding: '4px 8px' }} onClick={() => { setSelectedSession(session); setShowSessionModal(true); }}>
                        Manage
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Session modal (create or manage) */}
        {showSessionModal && (
          <SimpleModal title={selectedSession ? `Manage ${selectedSession.station}` : 'Create session'} onClose={() => { setShowSessionModal(false); setSelectedSession(null); }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {selectedSession ? (
                <div>
                  <div><strong>Station:</strong> {selectedSession.station}</div>
                  <div><strong>User:</strong> {selectedSession.user}</div>
                  <div style={{ marginTop: 12, display: 'flex', gap: 8 }}>
                    <button className="btn" onClick={() => { setConfirmAction({ type: 'pause', label: 'Pause session', session: selectedSession }); setShowConfirm(true); }}>Pause</button>
                    <button className="btn" onClick={() => { setConfirmAction({ type: 'resume', label: 'Resume session', session: selectedSession }); setShowConfirm(true); }}>Resume</button>
                    <button className="btn btn-danger" onClick={() => { setConfirmAction({ type: 'end', label: 'End session', session: selectedSession }); setShowConfirm(true); }}>End</button>
                  </div>
                </div>
              ) : (
                <CreateSessionForm onStarted={() => { setShowSessionModal(false); loadDashboardData(); toast.push('Session started', 'success'); }} />
              )}
            </div>
          </SimpleModal>
        )}

        {/* Confirm modal for destructive actions */}
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

        {/* System Alerts */}
        <div className="dc-card">
          <h3 style={{ margin: '0 0 20px 0', color: 'var(--text-primary)', fontSize: '18px', fontWeight: 600 }}>System Alerts</h3>
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {systemAlerts.map(alert => (
              <div key={alert.id} style={{
                padding: '12px',
                borderRadius: '8px',
                background: 'var(--bg-elevated)',
                border: `1px solid ${
                  alert.type === 'warning' ? 'var(--accent-amber)' : 
                  alert.type === 'success' ? 'var(--accent-green)' : 
                  'var(--accent-blue)'
                }`
              }}>
                <div style={{ 
                  display: 'flex', 
                  alignItems: 'flex-start', 
                  gap: '8px',
                  marginBottom: '4px'
                }}>
                  <div style={{
                    width: '6px',
                    height: '6px',
                    borderRadius: '50%',
                    background: alert.type === 'warning' ? 'var(--accent-amber)' : 
                               alert.type === 'success' ? 'var(--accent-green)' : 
                               'var(--accent-blue)',
                    marginTop: '6px',
                    flexShrink: 0
                  }}></div>
                  <div style={{ flex: 1 }}>
                    <div style={{ 
                      color: 'var(--text-primary)', 
                      fontSize: '14px',
                      fontWeight: 500,
                      lineHeight: 1.4
                    }}>
                      {alert.message}
                    </div>
                    <div style={{ 
                      color: 'var(--text-muted)', 
                      fontSize: '12px',
                      marginTop: '2px'
                    }}>
                      {alert.time}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
          
          <button className="btn btn-secondary" style={{ 
            width: '100%', 
            marginTop: '16px',
            fontSize: '12px',
            padding: '8px'
          }} onClick={() => toast.push('Opening alerts panel (mock)', 'info')}>
            View All Alerts
          </button>
        </div>
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
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      <label>Station</label>
      <select value={stationId} onChange={e => setStationId(e.target.value)} disabled={loadingChoices}>
        <option value="">-- select station --</option>
        {stations.map(s => {
          // id field may vary: stationId, id, StationId
          const id = s?.stationId ?? s?.id ?? s?.StationId ?? s?.stationID ?? s?.stationId;
          const name = s?.stationName ?? s?.name ?? s?.station_name ?? s?.station ?? s?.displayName;
          const label = name || (id ? `Station ${id}` : 'Unnamed station');
          return <option key={id ?? label} value={id ?? ''}>{label}</option>;
        })}
      </select>

      <label>User</label>
      <select value={userId} onChange={e => setUserId(e.target.value)} disabled={loadingChoices}>
        <option value="">-- select user --</option>
        {users.map(u => {
          const id = u?.userId ?? u?.id ?? u?.UserId ?? u?.userID ?? u?.userId;
          const name = u?.username ?? u?.userName ?? u?.name ?? u?.displayName ?? u?.email;
          const label = name || (id ? `User ${id}` : 'Unknown user');
          return <option key={id ?? label} value={id ?? ''}>{label}</option>;
        })}
      </select>

      <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
        <button className="btn" onClick={start} disabled={loadingChoices}>Start</button>
        <button className="btn" onClick={() => { setStationId(''); setUserId(''); }}>Clear</button>
      </div>
    </div>
  );
}
