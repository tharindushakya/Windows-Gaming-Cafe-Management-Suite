import React, { createContext, useContext, useState, useEffect } from 'react';
import api from '../api';

const AuthContext = createContext(null);

export function useAuth() {
  return useContext(AuthContext);
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try { return JSON.parse(localStorage.getItem('gc_user')); } catch { return null; }
  });

  useEffect(() => {
    if (user) localStorage.setItem('gc_user', JSON.stringify(user));
    else localStorage.removeItem('gc_user');
  }, [user]);

  async function login(email, password) {
    const payload = { email, password };
    const data = await api.post('/api/v1.0/auth/login', payload);
    // API expected to return { accessToken, user }
    if (data?.accessToken) {
      const u = { ...data.user, token: data.accessToken };
      setUser(u);
      // ensure api helper has token
      api.setToken(data.accessToken);
      return data;
    }
    throw new Error('Invalid login response');
  }

  function logout() {
    setUser(null);
    api.setToken(null);
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
