import type {
  AppointmentStatus, BookingStatus, InvoiceStatus, NotificationType, PaymentMethod, PaymentStatus,
  PaymentType, PropertyStatus, PropertyType, ReportReason, ReportStatus, UserRole,
} from './types';

export const roleLabel: Record<UserRole, string> = {
  Admin: 'Quản trị viên',
  Landlord: 'Chủ nhà',
  Tenant: 'Người thuê',
};

export const propertyTypeLabel: Record<PropertyType, string> = {
  Room: 'Phòng trọ',
  Apartment: 'Căn hộ',
  House: 'Nhà nguyên căn',
  Villa: 'Biệt thự',
  Office: 'Văn phòng',
};

export const propertyStatusLabel: Record<PropertyStatus, string> = {
  PendingApproval: 'Chờ duyệt',
  Available: 'Đang cho thuê',
  Rented: 'Đã cho thuê',
  Hidden: 'Đang ẩn',
  Rejected: 'Bị từ chối',
};

export const bookingStatusLabel: Record<BookingStatus, string> = {
  Pending: 'Chờ duyệt',
  Approved: 'Đang thuê',
  Rejected: 'Bị từ chối',
  Cancelled: 'Đã hủy',
  Completed: 'Đã kết thúc',
};

export const paymentTypeLabel: Record<PaymentType, string> = {
  Deposit: 'Tiền cọc',
  MonthlyRent: 'Tiền thuê tháng',
};

export const paymentMethodLabel: Record<PaymentMethod, string> = {
  Cash: 'Tiền mặt',
  BankTransfer: 'Chuyển khoản',
  Momo: 'MoMo',
  VnPay: 'VNPay',
};

export const paymentStatusLabel: Record<PaymentStatus, string> = {
  Pending: 'Chờ xác nhận',
  Success: 'Thành công',
  Failed: 'Thất bại',
  Refunded: 'Đã hoàn tiền',
};

export const invoiceStatusLabel: Record<InvoiceStatus, string> = {
  Unpaid: 'Chưa thanh toán',
  PartiallyPaid: 'Thanh toán một phần',
  Paid: 'Đã thanh toán',
  Overdue: 'Quá hạn',
  Cancelled: 'Đã hủy',
};

export const appointmentStatusLabel: Record<AppointmentStatus, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Rejected: 'Bị từ chối',
  Cancelled: 'Đã hủy',
  Completed: 'Đã xem nhà',
};

export const reportReasonLabel: Record<ReportReason, string> = {
  FakeInformation: 'Thông tin sai sự thật',
  WrongPrice: 'Sai giá',
  AlreadyRented: 'Nhà đã cho thuê',
  Scam: 'Lừa đảo',
  Duplicate: 'Tin trùng lặp',
  InappropriateContent: 'Nội dung không phù hợp',
  Other: 'Khác',
};

export const reportStatusLabel: Record<ReportStatus, string> = {
  Pending: 'Chờ xử lý',
  Resolved: 'Đã xử lý',
  Dismissed: 'Bác bỏ',
};

export const notificationTypeLabel: Record<NotificationType, string> = {
  System: 'Hệ thống',
  Property: 'Tin đăng',
  Booking: 'Thuê nhà',
  Payment: 'Thanh toán',
  Invoice: 'Hóa đơn',
  Appointment: 'Lịch hẹn',
  Review: 'Đánh giá',
  Report: 'Báo cáo',
};

/** Màu badge theo trạng thái (dùng chung cho mọi loại trạng thái) */
export function statusTone(status: string): 'green' | 'yellow' | 'red' | 'gray' | 'blue' {
  switch (status) {
    case 'Available': case 'Approved': case 'Success': case 'Paid': case 'Confirmed': case 'Resolved':
      return 'green';
    case 'Pending': case 'PendingApproval': case 'Unpaid': case 'PartiallyPaid':
      return 'yellow';
    case 'Rejected': case 'Failed': case 'Overdue':
      return 'red';
    case 'Rented': case 'Completed': case 'Refunded':
      return 'blue';
    default:
      return 'gray';
  }
}

export const AMENITIES = [
  'Wifi', 'Điều hòa', 'Máy giặt', 'Nóng lạnh', 'Tủ lạnh', 'Bếp', 'Thang máy', 'Bãi đỗ xe',
  'Gara ô tô', 'Ban công', 'Sân thượng', 'Gác lửng', 'Nội thất', 'Bảo vệ 24/7', 'Hồ bơi',
];

export const SORT_OPTIONS = [
  { value: 'newest', label: 'Mới nhất' },
  { value: 'price_asc', label: 'Giá tăng dần' },
  { value: 'price_desc', label: 'Giá giảm dần' },
  { value: 'area_desc', label: 'Diện tích lớn nhất' },
  { value: 'rating_desc', label: 'Đánh giá cao' },
  { value: 'most_viewed', label: 'Xem nhiều' },
];

// ===== Định dạng =====

const vnd = new Intl.NumberFormat('vi-VN');
export const money = (v: number | undefined | null) => `${vnd.format(v ?? 0)} đ`;

/** 3500000 → "3,5 triệu" */
export function shortMoney(v: number) {
  if (v >= 1_000_000_000) return `${+(v / 1_000_000_000).toFixed(2)} tỷ`.replace('.', ',');
  if (v >= 1_000_000) return `${+(v / 1_000_000).toFixed(1)} triệu`.replace('.', ',');
  return money(v);
}

export function fmtDate(v?: string | null) {
  if (!v) return '—';
  // DateOnly "2026-10-28" → hiển thị trực tiếp, tránh lệch múi giờ
  if (/^\d{4}-\d{2}-\d{2}$/.test(v)) {
    const [y, m, d] = v.split('-');
    return `${d}/${m}/${y}`;
  }
  return new Date(ensureUtc(v)).toLocaleDateString('vi-VN');
}

export function fmtDateTime(v?: string | null) {
  if (!v) return '—';
  return new Date(ensureUtc(v)).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

/** Backend lưu UTC; nếu chuỗi thiếu hậu tố múi giờ thì coi là UTC */
export function ensureUtc(v: string) {
  return /[zZ]|[+-]\d{2}:\d{2}$/.test(v) ? v : `${v}Z`;
}

export function timeAgo(v: string) {
  const diff = (Date.now() - new Date(ensureUtc(v)).getTime()) / 1000;
  if (diff < 60) return 'vừa xong';
  if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
  if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
  if (diff < 86400 * 30) return `${Math.floor(diff / 86400)} ngày trước`;
  return fmtDate(v);
}

export const todayIso = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

/**
 * Link trong thông báo là đường dẫn API (/api/bookings/5...) → đổi sang route của web.
 */
/**
 * Trang cần mở khi bấm vào một thông báo.
 * Backend lưu đường dẫn API (vd "/api/bookings/5/payments"); hàm này đổi sang đúng trang + vị trí trên giao diện.
 * Thông báo không có đường dẫn vẫn mở trang tương ứng với loại thông báo, nên lúc nào cũng có chỗ để đi.
 */
export function notificationRoute(n: { link?: string | null; type: NotificationType }, role?: UserRole): string {
  const [rawPath, rawQuery = ''] = (n.link ?? '').replace(/^\/api/, '').split('?');
  const path = rawPath.replace(/\/+$/, '');
  const query = new URLSearchParams(rawQuery);
  let m: RegExpMatchArray | null;

  if (path) {
    // Tin đăng
    if ((m = path.match(/^\/properties\/(\d+)\/reviews$/))) return `/properties/${m[1]}#reviews`;
    if ((m = path.match(/^\/properties\/(\d+)$/))) {
      // Admin nhận "tin mới chờ duyệt / gửi duyệt lại" -> trang duyệt tin, tô sáng đúng tin đó
      if (role === 'Admin' && n.type === 'Property') return `/admin/properties?status=PendingApproval&highlight=${m[1]}`;
      return `/properties/${m[1]}`;
    }
    if (path === '/properties/manage') return `/admin/properties?status=${query.get('status') ?? 'PendingApproval'}`;
    if (path === '/properties/mine') return '/landlord/properties';
    if (path === '/properties') return '/search';

    // Hợp đồng, thanh toán, hóa đơn
    if ((m = path.match(/^\/bookings\/(\d+)\/payments$/))) return `/bookings/${m[1]}#payments`;
    if ((m = path.match(/^\/bookings\/(\d+)\/invoices$/))) return `/bookings/${m[1]}#invoices`;
    if ((m = path.match(/^\/bookings\/(\d+)$/))) return `/bookings/${m[1]}`;
    if (path.startsWith('/bookings')) return '/bookings';
    if (path.startsWith('/payments')) return '/payments';
    if ((m = path.match(/^\/invoices\/(\d+)$/))) return `/invoices/${m[1]}`;
    if (path.startsWith('/invoices')) return '/invoices';

    // Lịch hẹn: tô sáng đúng lịch hẹn trong danh sách
    if ((m = path.match(/^\/appointments\/(\d+)$/))) return `/appointments?highlight=${m[1]}`;
    if (path.startsWith('/appointments')) return '/appointments';

    // Báo cáo vi phạm
    if (path.startsWith('/reports/mine')) return '/my-reports';
    if (path.startsWith('/reports')) return role === 'Admin' ? '/admin/reports' : '/my-reports';

    // Tài khoản
    if (path.startsWith('/auth/me') || path.startsWith('/users/me')) return '/profile';
    if (path.startsWith('/conversations')) return '/messages';
  }

  // Không có đường dẫn (hoặc dạng chưa biết): mở trang theo loại thông báo
  switch (n.type) {
    case 'Booking': return '/bookings';
    case 'Payment': return '/payments';
    case 'Invoice': return '/invoices';
    case 'Appointment': return '/appointments';
    case 'Property': return role === 'Admin' ? '/admin/properties?status=PendingApproval' : role === 'Landlord' ? '/landlord/properties' : '/search';
    case 'Review': return role === 'Landlord' ? '/landlord/properties' : '/bookings';
    case 'Report': return role === 'Admin' ? '/admin/reports' : '/my-reports';
    default: return '/profile';
  }
}
