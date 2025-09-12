import React, { useEffect, useState } from 'react';
import { NavLink } from 'react-router-dom';

export default function Sidebar() {
  const [open, setOpen] = useState(() => {
    try { return localStorage.getItem('sidebarOpen') === '1'; } catch { return true; }
  });

  useEffect(() => {
    function onToggle() {
      setOpen(prev => {
        const next = !prev;
        try { localStorage.setItem('sidebarOpen', next ? '1' : '0'); } catch {}
        return next;
      });
    }

    window.addEventListener('sidebar-toggle', onToggle);
    return () => window.removeEventListener('sidebar-toggle', onToggle);
  }, []);

  const navItems = [
    { to: '/', label: 'Dashboard', icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z', end: true },
    { to: '/stations', label: 'Stations', icon: 'M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z' },
    { to: '/users', label: 'Users', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z' },
    { to: '/reservations', label: 'Reservations', icon: 'M8 7V3a2 2 0 012-2h6a2 2 0 012 2v4h3a1 1 0 011 1v9a2 2 0 01-2 2H2a2 2 0 01-2-2V8a1 1 0 011-1h3zm4-4v4h4V3h-4zM3 9v8h16V9H3z' },
    { to: '/payments', label: 'Payments', icon: 'M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z' },
    { to: '/inventory', label: 'Inventory', icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4' },
    { to: '/pos', label: 'POS', icon: 'M9 5H7a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2zm8 0h-2a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2z' },
    { to: '/wallet', label: 'Wallet', icon: 'M21 15.546c-.523 0-.969-.235-1.327-.47a1.249 1.249 0 00-1.346 0c-.358.235-.804.47-1.327.47s-.969-.235-1.327-.47a1.249 1.249 0 00-1.346 0c-.358.235-.804.47-1.327.47s-.969-.235-1.327-.47a1.249 1.249 0 00-1.346 0c-.358.235-.804.47-1.327.47s-.969-.235-1.327-.47a1.249 1.249 0 00-1.346 0C4.969 15.311 4.523 15.546 4 15.546V14c.523 0 .969.235 1.327.47a1.249 1.249 0 001.346 0c.358-.235.804-.47 1.327-.47s.969.235 1.327.47a1.249 1.249 0 001.346 0c.358-.235.804-.47 1.327-.47s.969.235 1.327.47a1.249 1.249 0 001.346 0c.358-.235.804-.47 1.327-.47s.969.235 1.327.47a1.249 1.249 0 001.346 0c.358-.235.804-.47 1.327-.47v1.546zM3 8l1.5 1.5L6 8l1.5 1.5L9 8l1.5 1.5L12 8l1.5 1.5L15 8l1.5 1.5L18 8l1.5 1.5L21 8v6c0 1.105-.895 2-2 2H5c-1.105 0-2-.895-2-2V8z' },
    { to: '/reports', label: 'Reports', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' }
  ];

  return (
    <aside className={`${open ? 'translate-x-0' : '-translate-x-full'} md:translate-x-0 fixed md:static inset-y-0 left-0 z-40 w-64 bg-white border-r border-gray-200 transform transition-transform duration-200 ease-in-out`}>
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center">
            <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <h3 className="text-lg font-bold text-gray-900">GamingCafe</h3>
        </div>
        <button 
          className="md:hidden p-2 rounded-lg text-gray-500 hover:text-gray-700 hover:bg-gray-100 transition-colors duration-200" 
          onClick={() => window.dispatchEvent(new CustomEvent('sidebar-toggle'))} 
          aria-label="Close sidebar"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      {/* Navigation */}
      <nav className="px-4 py-6">
        <ul className="space-y-1">
          {navItems.map((item) => (
            <li key={item.to}>
              <NavLink 
                to={item.to} 
                end={item.end}
                className={({ isActive }) => 
                  `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors duration-200 ${
                    isActive 
                      ? 'bg-indigo-50 text-indigo-700 border-r-2 border-indigo-700' 
                      : 'text-gray-700 hover:bg-gray-100 hover:text-gray-900'
                  }`
                }
              >
                <svg className="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={item.icon} />
                </svg>
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* Footer */}
      <div className="absolute bottom-4 left-4 right-4 p-3 bg-gray-50 rounded-lg">
        <div className="text-xs text-gray-500 text-center">
          Gaming Café Admin
          <br />
          <span className="text-gray-400">v1.0.0</span>
        </div>
      </div>
    </aside>
  );
}
