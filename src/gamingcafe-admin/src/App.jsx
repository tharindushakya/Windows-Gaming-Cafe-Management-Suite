import './App.css';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import ToastProvider from './components/ToastProvider';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import ProtectedRoute from './components/ProtectedRoute';
import DashboardLayout from './components/DashboardLayout';
import Stations from './pages/Stations';
import StationsList from './pages/StationsList';
import Reservations from './pages/Reservations';
import Inventory from './pages/Inventory';
import POS from './pages/POS';
import Users from './pages/Users';
import Wallet from './pages/Wallet';
import Payments from './pages/Payments';
import Reports from './pages/Reports';
import Profile from './pages/Profile';
import Products from './pages/Products';
import Transactions from './pages/Transactions';
import Loyalty from './pages/Loyalty';

function App() {
  return (
    <div className="App">
      <BrowserRouter>
        <AuthProvider>
          <ToastProvider>
            <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Dashboard />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/stations" element={
              <ProtectedRoute>
                <DashboardLayout>
                    <Stations />
                </DashboardLayout>
              </ProtectedRoute>
            } />
              <Route path="/stations-list" element={
                <ProtectedRoute>
                  <DashboardLayout>
                    <StationsList />
                  </DashboardLayout>
                </ProtectedRoute>
              } />
            <Route path="/reservations" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Reservations />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/inventory" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Inventory />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/pos" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <POS />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/users" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Users />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/wallet" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Wallet />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/profile" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Profile />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/payments" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Payments />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/reports" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Reports />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/products" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Products />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/transactions" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Transactions />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            <Route path="/loyalty" element={
              <ProtectedRoute>
                <DashboardLayout>
                  <Loyalty />
                </DashboardLayout>
              </ProtectedRoute>
            } />
            </Routes>
          </ToastProvider>
        </AuthProvider>
      </BrowserRouter>
    </div>
  );
}

export default App;
