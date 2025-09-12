import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';

const ToastContext = createContext(null);
export function useToast() { return useContext(ToastContext); }

const ICONS = {
  info: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><circle cx="12" cy="12" r="10" stroke="white" strokeOpacity="0.12"/><path d="M11 11h2v5h-2zM11 7h2v2h-2z" fill="white"/></svg>
  ),
  success: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M20 6L9 17l-5-5" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/></svg>
  ),
  error: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M18 6L6 18M6 6l12 12" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/></svg>
  ),
  warn: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" stroke="white" strokeWidth="0" fill="white" fillOpacity="0.12"/><path d="M12 9v4" stroke="white" strokeWidth="2" strokeLinecap="round"/><path d="M12 17h.01" stroke="white" strokeWidth="2" strokeLinecap="round"/></svg>
  )
};

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  // inject keyframes for progress bar once
  useEffect(() => {
    const id = 'toast-progress-keyframes';
    if (!document.getElementById(id)) {
      const style = document.createElement('style');
      style.id = id;
      style.innerHTML = `@keyframes toast-progress { from { width: 100%; } to { width: 0%; } }`;
      document.head.appendChild(style);
    }
  }, []);

  const remove = useCallback((id) => setToasts(t => t.filter(x => x.id !== id)), []);

  const push = useCallback((message, type = 'info', duration = 4000) => {
    const id = Math.random().toString(36).slice(2,9);
    // cap toasts to 5
    setToasts(prev => {
      const next = [...prev, { id, message, type, duration }];
      if (next.length > 5) next.shift();
      return next;
    });
    // auto-remove after duration
    setTimeout(() => remove(id), duration);
  }, [remove]);

  return (
    <ToastContext.Provider value={{ push, remove }}>
      {children}
      <div aria-live="polite" className="fixed right-4 top-4 z-50 flex flex-col gap-2">
        {toasts.map(t => {
          const bg = t.type === 'error' ? '#ef4444' : t.type === 'success' ? '#10b981' : t.type === 'warn' ? '#f59e0b' : '#374151';
          return (
            <div key={t.id} role="status" className="min-w-[260px] max-w-[360px] text-white shadow-xl rounded overflow-hidden flex flex-col">
              <div className="flex items-center px-3 py-2 gap-3" style={{ background: bg }}>
                <div className="w-7 h-7 flex items-center justify-center opacity-95">{ICONS[t.type] ?? ICONS.info}</div>
                <div className="flex-1 text-sm leading-5">{t.message}</div>
                <button aria-label="dismiss" onClick={() => remove(t.id)} className="ml-3 p-2 text-white/90">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none"><path d="M18 6L6 18M6 6l12 12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/></svg>
                </button>
              </div>
              <div className="h-1 bg-white/10">
                <div className="h-full bg-white" style={{ width: '100%', animation: `toast-progress ${t.duration}ms linear forwards` }} />
              </div>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export default ToastProvider;
