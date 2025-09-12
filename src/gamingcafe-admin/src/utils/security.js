import { useAuth } from '../auth/AuthContext';

export function useHasRole() {
  const { user } = useAuth();
  return (role) => {
    if (!user) return false;
    const roles = (user.role || user.roles || '').toString().split(',').map(r => r.trim());
    return roles.includes(role) || roles.includes('Admin');
  };
}

export default useHasRole;
