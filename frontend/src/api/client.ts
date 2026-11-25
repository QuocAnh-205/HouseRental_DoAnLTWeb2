import type { ApiResponse, AuthResponseDto } from './types';

// Mặc định gọi cùng origin, Vite proxy chuyển /api sang backend (xem vite.config.ts).
// Chỉ đặt VITE_API_URL khi frontend được host tách riêng khỏi backend.
export const API_URL = ((import.meta.env.VITE_API_URL as string | undefined) ?? '').replace(/\/$/, '');

const TOKEN_KEY = 'hr_access_token';
const REFRESH_KEY = 'hr_refresh_token';

export const tokenStore = {
  get access() { return localStorage.getItem(TOKEN_KEY); },
  get refresh() { return localStorage.getItem(REFRESH_KEY); },
  set(auth: AuthResponseDto) {
    localStorage.setItem(TOKEN_KEY, auth.accessToken);
    localStorage.setItem(REFRESH_KEY, auth.refreshToken);
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
  },
};

export class ApiError extends Error {
  status: number;
  errors: string[];
  constructor(message: string, status: number, errors: string[] = []) {
    super(message);
    this.status = status;
    this.errors = errors;
  }
  /** Gộp message + danh sách lỗi validate để hiển thị */
  get detail() {
    return this.errors.length ? `${this.message}: ${this.errors.join('; ')}` : this.message;
  }
}

/** Gọi khi refresh token thất bại → AuthContext đăng xuất */
let onAuthLost: () => void = () => {};
export const setOnAuthLost = (fn: () => void) => { onAuthLost = fn; };

type Query = Record<string, string | number | boolean | undefined | null | (string | number)[]>;

export function buildQuery(query?: Query) {
  if (!query) return '';
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') continue;
    if (Array.isArray(value)) value.forEach(v => params.append(key, String(v)));
    else params.append(key, String(value));
  }
  const s = params.toString();
  return s ? `?${s}` : '';
}

/**
 * 'ok': đã có token mới; 'invalid': refresh token hết hạn / bị thu hồi (phải đăng nhập lại);
 * 'error': lỗi tạm thời (mất mạng, máy chủ bận, 429) — giữ nguyên phiên đăng nhập.
 */
type RefreshResult = 'ok' | 'invalid' | 'error';
let refreshing: Promise<RefreshResult> | null = null;

async function tryRefresh(): Promise<RefreshResult> {
  const refreshToken = tokenStore.refresh;
  if (!refreshToken) return 'invalid';
  refreshing ??= (async (): Promise<RefreshResult> => {
    try {
      const res = await fetch(`${API_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });
      if (res.status === 401 || res.status === 403) return 'invalid';
      if (!res.ok) return 'error';
      const body = (await res.json()) as ApiResponse<AuthResponseDto>;
      if (!body.data) return 'error';
      tokenStore.set(body.data);
      return 'ok';
    } catch {
      return 'error';
    } finally {
      setTimeout(() => { refreshing = null; }, 0);
    }
  })();
  return refreshing;
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  query?: Query;
  /** true: không tự refresh khi 401 */
  noRetry?: boolean;
}

/** Trả về toàn bộ ApiResponse (dùng khi cần message) */
export async function requestRaw<T>(path: string, opts: RequestOptions = {}): Promise<ApiResponse<T>> {
  const headers: Record<string, string> = {};
  const isForm = opts.body instanceof FormData;
  if (opts.body !== undefined && !isForm) headers['Content-Type'] = 'application/json';
  const token = tokenStore.access;
  if (token) headers.Authorization = `Bearer ${token}`;

  let res: Response;
  try {
    res = await fetch(`${API_URL}${path}${buildQuery(opts.query)}`, {
      method: opts.method ?? 'GET',
      headers,
      body: opts.body === undefined ? undefined : isForm ? (opts.body as FormData) : JSON.stringify(opts.body),
    });
  } catch {
    throw new ApiError(`Không kết nối được tới máy chủ API${API_URL ? ` (${API_URL})` : ''}. Hãy kiểm tra backend đã chạy chưa.`, 0);
  }

  if (res.status === 401 && token && !opts.noRetry) {
    const refreshed = await tryRefresh();
    if (refreshed === 'ok') return requestRaw<T>(path, { ...opts, noRetry: true });
    if (refreshed === 'invalid') {
      tokenStore.clear();
      onAuthLost();
    }
  }

  let body: ApiResponse<T> | null = null;
  const text = await res.text();
  if (text) {
    try { body = JSON.parse(text); } catch { /* không phải JSON */ }
  }

  if (!res.ok || (body && body.success === false)) {
    const fallback = res.status >= 500 && !body
      ? 'Máy chủ API không phản hồi. Hãy kiểm tra backend đã chạy chưa (dotnet run --launch-profile http).'
      : `Lỗi ${res.status}`;
    throw new ApiError(body?.message ?? fallback, res.status, body?.errors ?? []);
  }
  return body ?? { success: true };
}

export async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const body = await requestRaw<T>(path, opts);
  // Response chỉ có message → trả null (khác undefined mà useAction dùng để báo lỗi)
  return (body.data ?? null) as T;
}

export const api = {
  get: <T>(path: string, query?: Query) => request<T>(path, { query }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  patch: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PATCH', body }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};

export function errorMessage(err: unknown) {
  if (err instanceof ApiError) return err.detail;
  if (err instanceof Error) return err.message;
  return 'Đã có lỗi xảy ra';
}
