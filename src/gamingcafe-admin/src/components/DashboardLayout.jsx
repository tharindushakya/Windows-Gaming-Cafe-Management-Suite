import React from 'react';
import Sidebar from './Sidebar';
import Topbar from './Topbar';

export default function DashboardLayout({ children }) {
  return (
    <div className="dc-app">
      <Sidebar />
      <div className="dc-main">
        <Topbar />
        <div className="dc-content">{children}</div>
      </div>
    </div>
  );
}
