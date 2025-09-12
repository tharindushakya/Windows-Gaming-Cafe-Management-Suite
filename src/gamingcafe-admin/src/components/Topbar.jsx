import React, { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useNavigate } from 'react-router-dom';

export default function Topbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [showUserMenu, setShowUserMenu] = useState(false);

  async function checkProfile() {
  // Legacy helper - now navigate to profile page
  navigate('/profile');
  }

  function handleLogout() {
    logout();
    setShowUserMenu(false);
  }

  return (
    <header className="dc-topbar">
      <div className="dc-search">
        <h1 style={{ margin: 0, fontSize: '20px', fontWeight: 600, color: 'var(--text-primary)' }}>
          Gaming Café Management
        </h1>
      </div>
      
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
        {/* System Status Indicator */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <div className="status-indicator status-online">
            <div style={{ width: '6px', height: '6px', borderRadius: '50%', background: 'var(--accent-green)' }}></div>
            System Online
          </div>
        </div>

        {/* User Menu */}
        <div style={{ position: 'relative' }}>
          <button 
            onClick={() => setShowUserMenu(!showUserMenu)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
              background: 'var(--bg-elevated)',
              border: '1px solid var(--border-subtle)',
              borderRadius: '8px',
              padding: '8px 12px',
              color: 'var(--text-primary)',
              cursor: 'pointer',
              transition: 'all 0.2s ease'
            }}
            onMouseEnter={e => e.target.style.borderColor = 'var(--border-prominent)'}
            onMouseLeave={e => e.target.style.borderColor = 'var(--border-subtle)'}
          >
            <div style={{
              width: '32px',
              height: '32px',
              borderRadius: '50%',
              background: 'linear-gradient(135deg, var(--accent-blue), var(--accent-purple))',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'white',
              fontWeight: 600,
              fontSize: '14px'
            }}>
              {(user?.email || 'A')[0].toUpperCase()}
            </div>
            <span className="dc-user">{user?.email || 'Admin'}</span>
            <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M7.247 11.14L2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"/>
            </svg>
          </button>

          {showUserMenu && (
            <div style={{
              position: 'absolute',
              top: '100%',
              right: 0,
              marginTop: '8px',
              background: 'var(--bg-card)',
              border: '1px solid var(--border-subtle)',
              borderRadius: '8px',
              boxShadow: 'var(--shadow-lg)',
              minWidth: '200px',
              zIndex: 1000
            }}>
              <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border-subtle)' }}>
                <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{user?.username || 'Admin'}</div>
                <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{user?.email}</div>
              </div>
              
              <button 
                onClick={checkProfile}
                style={{
                  width: '100%',
                  textAlign: 'left',
                  padding: '12px 16px',
                  background: 'none',
                  border: 'none',
                  color: 'var(--text-secondary)',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease'
                }}
                onMouseEnter={e => {
                  e.target.style.background = 'var(--bg-elevated)';
                  e.target.style.color = 'var(--text-primary)';
                }}
                onMouseLeave={e => {
                  e.target.style.background = 'none';
                  e.target.style.color = 'var(--text-secondary)';
                }}
              >
                View Profile
              </button>
              
              <button 
                onClick={handleLogout}
                style={{
                  width: '100%',
                  textAlign: 'left',
                  padding: '12px 16px',
                  background: 'none',
                  border: 'none',
                  color: 'var(--accent-red)',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease',
                  borderTop: '1px solid var(--border-subtle)'
                }}
                onMouseEnter={e => e.target.style.background = 'rgba(239, 68, 68, 0.1)'}
                onMouseLeave={e => e.target.style.background = 'none'}
              >
                Sign Out
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Click outside to close menu */}
      {showUserMenu && (
        <div 
          style={{ position: 'fixed', inset: 0, zIndex: 999 }}
          onClick={() => setShowUserMenu(false)}
        />
      )}
    </header>
  );
}
