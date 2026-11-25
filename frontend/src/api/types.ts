// Ánh xạ DTO / enum từ backend HouseRentalAPI (enum được serialize dạng chuỗi)

export type UserRole = 'Admin' | 'Landlord' | 'Tenant';
export type PropertyType = 'Room' | 'Apartment' | 'House' | 'Villa' | 'Office';
export type PropertyStatus = 'PendingApproval' | 'Available' | 'Rented' | 'Hidden' | 'Rejected';
export type BookingStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled' | 'Completed';
export type PaymentType = 'Deposit' | 'MonthlyRent';
export type PaymentMethod = 'Cash' | 'BankTransfer' | 'Momo' | 'VnPay';
export type PaymentStatus = 'Pending' | 'Success' | 'Failed' | 'Refunded';
export type InvoiceStatus = 'Unpaid' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled';
export type AppointmentStatus = 'Pending' | 'Confirmed' | 'Rejected' | 'Cancelled' | 'Completed';
export type ReportReason =
  | 'FakeInformation' | 'WrongPrice' | 'AlreadyRented' | 'Scam'
  | 'Duplicate' | 'InappropriateContent' | 'Other';
export type ReportStatus = 'Pending' | 'Resolved' | 'Dismissed';
export type NotificationType =
  | 'System' | 'Property' | 'Booking' | 'Payment' | 'Invoice' | 'Appointment' | 'Review' | 'Report';

export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserDto {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string;
  avatarUrl?: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export interface PublicProfileDto {
  id: number;
  fullName: string;
  avatarUrl?: string;
  role: UserRole;
  joinedAt: string;
  activeListings: number;
  averageRating: number;
  totalReviews: number;
}

export interface AuthResponseDto {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserDto;
}

export interface ForgotPasswordResultDto {
  resetToken?: string;
  expiresAt?: string;
}

export interface PropertySummaryDto {
  id: number;
  title: string;
  address: string;
  district: string;
  city: string;
  type: PropertyType;
  area: number;
  bedrooms: number;
  bathrooms: number;
  pricePerMonth: number;
  status: PropertyStatus;
  thumbnailUrl?: string;
  averageRating: number;
  reviewCount: number;
  viewCount: number;
  landlordId: number;
  landlordName: string;
  createdAt: string;
  ward?: string | null;
  imageCount?: number;
  landlordAvatarUrl?: string | null;
  latitude?: number | null;
  longitude?: number | null;
}

/** Một bài trong "Bản tin" trên trang cá nhân (GET /api/users/{id}/posts) */
export interface PropertyPostDto {
  id: number;
  title: string;
  excerpt: string;
  type: PropertyType;
  pricePerMonth: number;
  area: number;
  bedrooms: number;
  bathrooms: number;
  address: string;
  ward?: string | null;
  district: string;
  city: string;
  status: PropertyStatus;
  rejectReason?: string | null;
  imageUrls: string[];
  imageCount: number;
  amenities: string[];
  viewCount: number;
  averageRating: number;
  reviewCount: number;
  createdAt: string;
  publishedAt?: string | null;
  landlordId: number;
  landlordName: string;
  landlordAvatarUrl?: string | null;
}

export interface CountItemDto { key: string; count: number }

/** GET /api/properties/stats */
export interface PropertyStatsDto {
  total: number;
  byType: CountItemDto[];
  byDistrict: CountItemDto[];
}

export interface PropertyImageDto {
  id: number;
  url: string;
  isThumbnail: boolean;
}

export interface PropertyDetailDto {
  id: number;
  title: string;
  description: string;
  address: string;
  ward?: string;
  district: string;
  city: string;
  latitude?: number;
  longitude?: number;
  type: PropertyType;
  area: number;
  bedrooms: number;
  bathrooms: number;
  pricePerMonth: number;
  deposit: number;
  amenities: string[];
  status: PropertyStatus;
  rejectReason?: string;
  averageRating: number;
  reviewCount: number;
  viewCount: number;
  isFavorited: boolean;
  landlord: { id: number; fullName: string; email?: string; phoneNumber?: string; avatarUrl?: string };
  images: PropertyImageDto[];
  createdAt: string;
  updatedAt?: string;
}

export interface PropertyForm {
  title: string;
  description: string;
  address: string;
  ward?: string;
  district: string;
  city: string;
  latitude?: number | null;
  longitude?: number | null;
  type: PropertyType;
  area: number;
  bedrooms: number;
  bathrooms: number;
  pricePerMonth: number;
  deposit: number;
  amenities: string[];
  imageUrls?: string[];
}

export interface BookingDto {
  id: number;
  propertyId: number;
  propertyTitle: string;
  propertyAddress: string;
  landlordId: number;
  tenantId: number;
  tenantName: string;
  tenantPhone?: string;
  startDate: string;
  endDate: string;
  durationMonths: number;
  monthlyPrice: number;
  deposit: number;
  totalAmount: number;
  status: BookingStatus;
  note?: string;
  rejectReason?: string;
  createdAt: string;
}

export interface InvoiceDto {
  id: number;
  bookingId: number;
  propertyId: number;
  propertyTitle: string;
  tenantId: number;
  tenantName: string;
  month: number;
  year: number;
  rentAmount: number;
  electricityOldIndex: number;
  electricityNewIndex: number;
  electricityUnitPrice: number;
  electricityAmount: number;
  waterOldIndex: number;
  waterNewIndex: number;
  waterUnitPrice: number;
  waterAmount: number;
  serviceFee: number;
  otherFee: number;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  dueDate: string;
  status: InvoiceStatus;
  note?: string;
  createdAt: string;
}

export interface PaymentDto {
  id: number;
  bookingId: number;
  invoiceId?: number;
  propertyTitle?: string;
  amount: number;
  type: PaymentType;
  method: PaymentMethod;
  status: PaymentStatus;
  transactionCode: string;
  note?: string;
  paidAt?: string;
  createdAt: string;
}

export interface AppointmentDto {
  id: number;
  propertyId: number;
  propertyTitle: string;
  propertyAddress: string;
  landlordId: number;
  tenantId: number;
  tenantName: string;
  tenantPhone?: string;
  scheduledAt: string;
  status: AppointmentStatus;
  note?: string;
  landlordNote?: string;
  createdAt: string;
}

export interface ReviewDto {
  id: number;
  propertyId: number;
  tenantId: number;
  tenantName: string;
  tenantAvatarUrl?: string;
  rating: number;
  comment?: string;
  landlordReply?: string;
  repliedAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ReviewEligibilityDto {
  canReview: boolean;
  hasRented: boolean;
  myReview?: ReviewDto;
}

export interface ConversationDto {
  id: number;
  otherParticipant: { id: number; fullName: string; avatarUrl?: string; role?: UserRole };
  propertyId?: number;
  propertyTitle?: string;
  lastMessage?: string;
  lastMessageAt: string;
  unreadCount: number;
}

export interface MessageDto {
  id: number;
  conversationId: number;
  senderId: number;
  senderName: string;
  content: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export interface NotificationDto {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
  link?: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export interface ReportDto {
  id: number;
  propertyId: number;
  propertyTitle: string;
  reporterId: number;
  reporterName: string;
  reason: ReportReason;
  description?: string;
  status: ReportStatus;
  adminNote?: string;
  handledAt?: string;
  createdAt: string;
}

export interface MonthlyRevenueDto {
  year: number;
  month: number;
  revenue: number;
}

export interface AdminStatisticsDto {
  totalUsers: number;
  usersByRole: Record<string, number>;
  lockedUsers: number;
  totalProperties: number;
  propertiesByStatus: Record<string, number>;
  totalBookings: number;
  bookingsByStatus: Record<string, number>;
  pendingReports: number;
  totalRevenue: number;
  revenueByMonth: MonthlyRevenueDto[];
}

export interface LandlordStatisticsDto {
  totalProperties: number;
  propertiesByStatus: Record<string, number>;
  totalViews: number;
  averageRating: number;
  totalReviews: number;
  pendingBookings: number;
  activeContracts: number;
  pendingAppointments: number;
  unpaidInvoices: number;
  outstandingAmount: number;
  totalRevenue: number;
  revenueByMonth: MonthlyRevenueDto[];
}

export interface TenantStatisticsDto {
  activeContracts: number;
  pendingBookings: number;
  upcomingAppointments: number;
  favorites: number;
  unpaidInvoices: number;
  outstandingAmount: number;
  totalPaid: number;
}

export interface UploadResultDto {
  url: string;
  fileName: string;
  size: number;
  contentType: string;
}

export interface UnreadCount {
  unreadCount: number;
}
