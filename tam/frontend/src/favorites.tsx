import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, errorMessage } from './api/client';
import type { PagedResult, PropertySummaryDto } from './api/types';
import { useAuth } from './auth/AuthContext';
import { toast } from './hooks';

interface FavoritesState {
  isFavorite: (id: number) => boolean;
  toggle: (id: number) => Promise<void>;
  /** Đồng bộ trạng thái lấy từ chi tiết nhà (isFavorited) */
  sync: (id: number, value: boolean) => void;
}

const FavoritesContext = createContext<FavoritesState | null>(null);

/** Danh sách id nhà đã lưu của người dùng, để thả tim ngay trên thẻ nhà */
export function FavoritesProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [ids, setIds] = useState<Set<number>>(new Set());

  useEffect(() => {
    if (!user) { setIds(new Set()); return; }
    api.get<PagedResult<PropertySummaryDto>>('/api/favorites', { pageSize: 100 })
      .then(res => setIds(new Set(res.items.map(p => p.id))))
      .catch(() => { /* bỏ qua */ });
  }, [user]);

  const sync = useCallback((id: number, value: boolean) => {
    setIds(prev => {
      if (prev.has(id) === value) return prev;
      const next = new Set(prev);
      if (value) next.add(id); else next.delete(id);
      return next;
    });
  }, []);

  const toggle = useCallback(async (id: number) => {
    if (!user) {
      navigate('/login', { state: { from: window.location.pathname + window.location.search } });
      return;
    }
    const fav = ids.has(id);
    sync(id, !fav);
    try {
      if (fav) await api.del(`/api/favorites/${id}`);
      else await api.post(`/api/favorites/${id}`);
      toast(fav ? 'Đã bỏ lưu' : 'Đã lưu vào danh sách yêu thích');
    } catch (err) {
      sync(id, fav);
      toast(errorMessage(err), 'error');
    }
  }, [user, ids, navigate, sync]);

  const isFavorite = useCallback((id: number) => ids.has(id), [ids]);

  return <FavoritesContext.Provider value={{ isFavorite, toggle, sync }}>{children}</FavoritesContext.Provider>;
}

export function useFavorites() {
  const ctx = useContext(FavoritesContext);
  if (!ctx) throw new Error('useFavorites phải dùng trong FavoritesProvider');
  return ctx;
}
