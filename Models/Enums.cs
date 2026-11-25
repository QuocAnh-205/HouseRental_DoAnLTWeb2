namespace HouseRentalAPI.Models;

public enum UserRole
{
    Admin = 1,
    Landlord = 2,
    Tenant = 3
}

public enum PropertyType
{
    Room = 1,
    Apartment = 2,
    House = 3,
    Villa = 4,
    Office = 5
}

public enum PropertyStatus
{
    PendingApproval = 0,
    Available = 1,
    Rented = 2,
    Hidden = 3,
    Rejected = 4
}

public enum BookingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Completed = 4
}

public enum PaymentType
{
    Deposit = 1,
    MonthlyRent = 2
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Momo = 3,
    VnPay = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Refunded = 3
}

public enum InvoiceStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Rejected = 2,
    Cancelled = 3,
    Completed = 4
}

public enum ReportReason
{
    FakeInformation = 1,
    WrongPrice = 2,
    AlreadyRented = 3,
    Scam = 4,
    Duplicate = 5,
    InappropriateContent = 6,
    Other = 99
}

public enum ReportStatus
{
    Pending = 0,
    Resolved = 1,
    Dismissed = 2
}

public enum NotificationType
{
    System = 0,
    Property = 1,
    Booking = 2,
    Payment = 3,
    Invoice = 4,
    Appointment = 5,
    Review = 6,
    Report = 7
}
