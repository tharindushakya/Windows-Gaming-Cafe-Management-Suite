import React, { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { useNavigate } from 'react-router-dom';

export default function Topbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [showUserMenu, setShowUserMenu] = useState(false);

  async function checkProfile() { navigate('/profile'); }
  function handleLogout() { logout(); setShowUserMenu(false); }

  return (
    <header className="flex items-center justify-between px-4 py-3 bg-white border-b border-gray-100">
      <div className="flex items-center gap-3">
        <button className="md:hidden p-2 rounded bg-gray-50" aria-label="Toggle sidebar" onClick={() => window.dispatchEvent(new CustomEvent('sidebar-toggle'))}>
          <svg className="w-6 h-6 text-gray-700" viewBox="0 0 24 24" fill="none"><path d="M4 6h16M4 12h16M4 18h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/></svg>
        </button>
        <h1 className="text-lg font-semibold text-gray-900">Gaming Café Management</h1>
      </div>

      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <span className="w-2 h-2 rounded-full bg-emerald-500 block" />
          <span>System Online</span>
        </div>

        <div className="relative">
          <button onClick={() => setShowUserMenu(!showUserMenu)} className="flex items-center gap-3 bg-white border border-gray-100 rounded-lg px-3 py-2 text-sm text-gray-800">
            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-sky-500 to-purple-600 flex items-center justify-center text-white font-semibold">{(user?.email || 'A')[0].toUpperCase()}</div>
            <span className="hidden sm:inline">{user?.email || 'Admin'}</span>
            <svg className="w-4 h-4 text-gray-500" viewBox="0 0 16 16" fill="currentColor"><path d="M7.247 11.14L2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"/></svg>
          </button>

          {showUserMenu && (
            <div className="absolute right-0 mt-2 w-52 bg-white border border-gray-100 rounded shadow-lg z-50">
              <div className="px-4 py-3 border-b border-gray-100">
                <div className="font-medium text-gray-900">{user?.username || 'Admin'}</div>
                <div className="text-xs text-gray-500">{user?.email}</div>
              </div>
              <button onClick={checkProfile} className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">View Profile</button>
              <button onClick={handleLogout} className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 border-t border-gray-100">Sign Out</button>
            </div>
          )}
        </div>
      </div>

      {showUserMenu && <div className="fixed inset-0 z-40" onClick={() => setShowUserMenu(false)} />}
    </header>
  );
}
