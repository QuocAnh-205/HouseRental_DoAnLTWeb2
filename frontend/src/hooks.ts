import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { errorMessage } from './api/client';

/**
 * Gọi API khi deps thay đổi. Trả về data, loading, error và reload().
 */
export function useApi<T>(fn: () => Promise<T>, deps: unknown[] = []) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const fnRef = useRef(fn);
  fnRef.current = fn;
  const callId = useRef(0);

  const reload = useCallback(async () => {
    const id = ++callId.current;
    setLoading(true);
    setError(null);
    try {
      const result = await fnRef.current();
      if (id === callId.current) setData(result);
    } catch (err) {
      if (id === callId.current) setError(errorMessage(err));
    } finally {
      if (id === callId.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return { data, loading, error, reload, setData };
}

/** Chạy một thao tác (submit form, đổi trạng thái...) và theo dõi trạng thái busy */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const run = useCallback(async <T,>(fn: () => Promise<T>, onError?: (msg: string) => void): Promise<T | undefined> => {
    setBusy(true);
    try {
      return await fn();
    } catch (err) {
      const msg = errorMessage(err);
      if (onError) onError(msg);
      else window.dispatchEvent(new CustomEvent('toast', { detail: { type: 'error', text: msg } }));
      return undefined;
    } finally {
      setBusy(false);
    }
  }, []);
  return { busy, run };
}

/**
 * Mục cần tô sáng khi mở trang từ một thông báo (?highlight=<id>).
 * Gắn data-highlight={id} lên phần tử tương ứng; hook tự cuộn tới khi danh sách đã hiển thị (ready đổi).
 */
export function useHighlight(ready: unknown): number | null {
  const [params] = useSearchParams();
  const id = Number(params.get('highlight')) || null;
  useEffect(() => {
    if (!id || !ready) return;
    const t = setTimeout(() => {
      document.querySelector(`[data-highlight="${id}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 150);
    return () => clearTimeout(t);
  }, [id, ready]);
  return id;
}

export function toast(text: string, type: 'success' | 'error' | 'info' = 'success') {
  window.dispatchEvent(new CustomEvent('toast', { detail: { type, text } }));
}
