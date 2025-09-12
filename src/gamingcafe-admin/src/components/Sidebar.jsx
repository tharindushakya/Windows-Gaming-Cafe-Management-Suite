import React from 'react';
import { NavLink } from 'react-router-dom';

export default function Sidebar() {
  return (
    <aside className="dc-sidebar">
      <h3 className="dc-brand">GamingCafe</h3>
      <nav>
        <ul>
          <li><NavLink to="/" end>Dashboard</NavLink></li>
          <li><NavLink to="/stations">Stations</NavLink></li>
          <li><NavLink to="/reservations">Reservations</NavLink></li>
          <li><NavLink to="/inventory">Inventory</NavLink></li>
          <li><NavLink to="/pos">POS</NavLink></li>
          <li><NavLink to="/users">Users</NavLink></li>
          <li><NavLink to="/wallet">Wallet</NavLink></li>
          <li><NavLink to="/payments">Payments</NavLink></li>
          <li><NavLink to="/reports">Reports</NavLink></li>
        </ul>
      </nav>
    </aside>
  );
}
