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

  return (
    <aside className={`${open ? 'translate-x-0' : '-translate-x-full'} md:translate-x-0 fixed md:static inset-y-0 left-0 z-40 w-64 bg-white border-r border-gray-100 shadow-sm transform transition-transform duration-200`}> 
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100">
        <h3 className="text-lg font-semibold text-gray-900">GamingCafe</h3>
        <button className="md:hidden text-gray-600" onClick={() => window.dispatchEvent(new CustomEvent('sidebar-toggle'))} aria-label="Close sidebar">✕</button>
      </div>

      <nav className="p-4">
        <ul className="space-y-2">
          <li>
            <NavLink to="/" end className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Dashboard</NavLink>
          </li>
          <li>
            <NavLink to="/stations" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Stations</NavLink>
          </li>
          <li>
            <NavLink to="/reservations" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Reservations</NavLink>
          </li>
          <li>
            <NavLink to="/inventory" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Inventory</NavLink>
          </li>
          <li>
            <NavLink to="/pos" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">POS</NavLink>
          </li>
          <li>
            <NavLink to="/users" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Users</NavLink>
          </li>
          <li>
            <NavLink to="/wallet" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Wallet</NavLink>
          </li>
          <li>
            <NavLink to="/payments" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Payments</NavLink>
          </li>
          <li>
            <NavLink to="/reports" className="block px-3 py-2 rounded hover:bg-gray-50" activeClassName="bg-gray-100 font-medium">Reports</NavLink>
          </li>
        </ul>
      </nav>
    </aside>
  );
}
