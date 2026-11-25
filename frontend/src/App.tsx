import { lazy, Suspense } from 'react';
import { BrowserRouter, Link, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import Layout from './components/Layout';
import AccountLayout from './components/AccountLayout';
import { Empty, Loading, RequireAuth } from './components/ui';

// Trang đăng tin dùng bản đồ (Leaflet) nên tải riêng khi người dùng mở trang
const PostListingPage = lazy(() => import('./pages/PostListingPage'));
import { AdminPropertiesPage, AdminReportsPage, AdminUsersPage } from './pages/admin/AdminPages';
import { ForgotPasswordPage, LoginPage, RegisterPage } from './pages/auth/AuthPages';
import ProfilePage from './pages/auth/ProfilePage';
import { BookingDetailPage, BookingsPage } from './pages/BookingsPages';
import DashboardPage from './pages/DashboardPage';
import { AppointmentsPage, InvoiceDetailPage, InvoicesPage, PaymentsPage } from './pages/FinancePages';
import HomePage from './pages/HomePage';
import SearchPage from './pages/SearchPage';
import NewsFeedPage from './pages/NewsFeedPage';
import { FavoritesProvider } from './favorites';
import { MyPropertiesPage, PropertyFormPage } from './pages/landlord/LandlordPages';
import PropertyDetailPage from './pages/PropertyDetailPage';
import {
  FavoritesPage, MessagesPage, MyReportsPage, NotificationsPage, PublicProfilePage,
} from './pages/SocialPages';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <FavoritesProvider>
        <Routes>
          <Route element={<Layout />}>
            {/* Công khai */}
            <Route index element={<HomePage />} />
            <Route path="search" element={<SearchPage />} />
            <Route path="properties/:id" element={<PropertyDetailPage />} />
            <Route path="users/:id" element={<PublicProfilePage />} />
            <Route path="ban-tin" element={<NewsFeedPage />} />
            <Route path="dang-tin" element={
              <RequireAuth roles={['Landlord']}>
                <Suspense fallback={<Loading text="Đang mở trang đăng tin..." />}><PostListingPage /></Suspense>
              </RequireAuth>
            } />
            <Route path="login" element={<LoginPage />} />
            <Route path="register" element={<RegisterPage />} />
            <Route path="forgot-password" element={<ForgotPasswordPage />} />

            <Route element={<AccountLayout />}>
              {/* Đã đăng nhập */}
              <Route path="dashboard" element={<RequireAuth><DashboardPage /></RequireAuth>} />
              <Route path="profile" element={<RequireAuth><ProfilePage /></RequireAuth>} />
              <Route path="notifications" element={<RequireAuth><NotificationsPage /></RequireAuth>} />
              <Route path="messages" element={<RequireAuth><MessagesPage /></RequireAuth>} />
              <Route path="messages/:id" element={<RequireAuth><MessagesPage /></RequireAuth>} />
              <Route path="favorites" element={<RequireAuth><FavoritesPage /></RequireAuth>} />
              <Route path="my-reports" element={<RequireAuth><MyReportsPage /></RequireAuth>} />
              <Route path="bookings" element={<RequireAuth><BookingsPage /></RequireAuth>} />
              <Route path="bookings/:id" element={<RequireAuth><BookingDetailPage /></RequireAuth>} />
              <Route path="invoices/:id" element={<RequireAuth><InvoiceDetailPage /></RequireAuth>} />

              {/* Người thuê / chủ nhà */}
              <Route path="invoices" element={<RequireAuth roles={['Tenant', 'Landlord']}><InvoicesPage /></RequireAuth>} />
              <Route path="payments" element={<RequireAuth roles={['Tenant', 'Landlord']}><PaymentsPage /></RequireAuth>} />
              <Route path="appointments" element={<RequireAuth roles={['Tenant', 'Landlord']}><AppointmentsPage /></RequireAuth>} />

              {/* Chủ nhà */}
              <Route path="landlord/properties" element={<RequireAuth roles={['Landlord']}><MyPropertiesPage /></RequireAuth>} />
              <Route path="landlord/properties/new" element={<Navigate to="/dang-tin" replace />} />
              <Route path="landlord/properties/:id/edit" element={<RequireAuth roles={['Landlord', 'Admin']}><PropertyFormPage /></RequireAuth>} />

              {/* Quản trị */}
              <Route path="admin/users" element={<RequireAuth roles={['Admin']}><AdminUsersPage /></RequireAuth>} />
              <Route path="admin/properties" element={<RequireAuth roles={['Admin']}><AdminPropertiesPage /></RequireAuth>} />
              <Route path="admin/reports" element={<RequireAuth roles={['Admin']}><AdminReportsPage /></RequireAuth>} />
            </Route>

            <Route path="*" element={
              <div className="container"><Empty text="Không tìm thấy trang."><Link className="btn btn-primary" to="/">Về trang chủ</Link></Empty></div>
            } />
          </Route>
        </Routes>
        </FavoritesProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
