import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import {
  faBell, faBuildingUser, faCalendarCheck, faChartPie, faComments, faCreditCard, faFileContract, faFileInvoiceDollar,
  faFlag, faHeart, faHouseCircleCheck, faPlus, faUserGear, faUsers,
} from '@fortawesome/free-solid-svg-icons';
import type { UserRole } from './api/types';

export interface MenuItem {
  to: string;
  label: string;
  icon: IconDefinition;
  /** Hiện số chưa đọc */
  badge?: 'messages' | 'notifications';
}

export interface MenuGroup {
  title: string;
  items: MenuItem[];
}

const common: MenuGroup = {
  title: 'Cá nhân',
  items: [
    { to: '/messages', label: 'Tin nhắn', icon: faComments, badge: 'messages' },
    { to: '/notifications', label: 'Thông báo', icon: faBell, badge: 'notifications' },
    { to: '/profile', label: 'Hồ sơ & bảo mật', icon: faUserGear },
  ],
};

export const ACCOUNT_MENU: Record<UserRole, MenuGroup[]> = {
  Tenant: [
    {
      title: 'Thuê nhà',
      items: [
        { to: '/dashboard', label: 'Tổng quan', icon: faChartPie },
        { to: '/bookings', label: 'Hợp đồng thuê', icon: faFileContract },
        { to: '/appointments', label: 'Lịch xem nhà', icon: faCalendarCheck },
        { to: '/invoices', label: 'Hóa đơn', icon: faFileInvoiceDollar },
        { to: '/payments', label: 'Thanh toán', icon: faCreditCard },
        { to: '/favorites', label: 'Nhà đã lưu', icon: faHeart },
        { to: '/my-reports', label: 'Báo cáo đã gửi', icon: faFlag },
      ],
    },
    common,
  ],
  Landlord: [
    {
      title: 'Cho thuê',
      items: [
        { to: '/dashboard', label: 'Tổng quan', icon: faChartPie },
        { to: '/landlord/properties', label: 'Nhà của tôi', icon: faBuildingUser },
        { to: '/dang-tin', label: 'Đăng tin mới', icon: faPlus },
        { to: '/bookings', label: 'Yêu cầu thuê', icon: faFileContract },
        { to: '/appointments', label: 'Lịch xem nhà', icon: faCalendarCheck },
        { to: '/invoices', label: 'Hóa đơn', icon: faFileInvoiceDollar },
        { to: '/payments', label: 'Thanh toán', icon: faCreditCard },
        { to: '/favorites', label: 'Nhà đã lưu', icon: faHeart },
      ],
    },
    common,
  ],
  Admin: [
    {
      title: 'Quản trị',
      items: [
        { to: '/dashboard', label: 'Tổng quan', icon: faChartPie },
        { to: '/admin/properties', label: 'Duyệt tin đăng', icon: faHouseCircleCheck },
        { to: '/admin/users', label: 'Người dùng', icon: faUsers },
        { to: '/bookings', label: 'Hợp đồng', icon: faFileContract },
        { to: '/admin/reports', label: 'Báo cáo vi phạm', icon: faFlag },
      ],
    },
    common,
  ],
};
