import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { api, ApiError, setOnAuthLost, tokenStore } from '../api/client';
import type { AuthResponseDto, UserDto, UserRole } from '../api/types';

interface AuthState {
  user: UserDto | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<UserDto>;
  register: (data: RegisterInput) => Promise<UserDto>;
  logout: () => Promise<void>;
  setUser: (u: UserDto) => void;
  hasRole: (...roles: UserRole[]) => boolean;
}

export interface RegisterInput {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
  role: 'Landlord' | 'Tenant';
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setOnAuthLost(() => setUser(null));
    if (!tokenStore.access) {
      setLoading(false);
      return;
    }
    api.get<UserDto>('/api/auth/me')
      .then(setUser)
      // Chỉ xoá phiên khi token thật sự không hợp lệ; lỗi mạng / máy chủ bận thì giữ để thử lại lần sau
      .catch(err => { if (err instanceof ApiError && (err.status === 401 || err.status === 403)) tokenStore.clear(); })
      .finally(() => setLoading(false));
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await api.post<AuthResponseDto>('/api/auth/login', { email, password });
    tokenStore.set(res);
    setUser(res.user);
    return res.user;
  }, []);

  const register = useCallback(async (data: RegisterInput) => {
    const res = await api.post<AuthResponseDto>('/api/auth/register', {
      ...data,
      phoneNumber: data.phoneNumber || undefined,
    });
    tokenStore.set(res);
    setUser(res.user);
    return res.user;
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.refresh;
    if (refreshToken) {
      try { await api.post('/api/auth/logout', { refreshToken }); } catch { /* bỏ qua */ }
    }
    tokenStore.clear();
    setUser(null);
  }, []);

  const hasRole = useCallback((...roles: UserRole[]) => !!user && roles.includes(user.role), [user]);

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout, setUser, hasRole }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth phải dùng trong AuthProvider');
  return ctx;
}
