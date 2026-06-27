namespace Cleaning.DAL.Enums
{
    public enum UserRole { Client, Worker, Admin }
    public enum AccountStatus { Active, Banned, PendingVerification }
    public enum VerificationPurpose { EmailVerification, PhoneVerification, PasswordReset }
    public enum PropertyType { Apartment, House }
    public enum ServiceUnitType { Hour }
    public enum BookingType { Scheduled, Immediate }
    public enum BookingStatus { PendingPayment, PaidPendingWorker, Accepted, RescheduleRequested, InProgress, Completed, Cancelled, Refunded }
    public enum WorkerOnlineStatus { Offline, Online, Busy }
    public enum AvailabilityStatus { Available, Blocked, Booked }
    public enum PaymentMethod { Cash, MoMo, VNPay, ZaloPay, BankTransfer }
    public enum PaymentStatus { Pending, Success, Failed, Refunded, PartiallyRefunded }
    public enum PayoutStatus { Pending, Paid, Failed }
    public enum RescheduleStatus { Pending, Accepted, Rejected, Cancelled }
    public enum AiSenderType { User, Ai }
    public enum PhotoType { Before, After, Issue, AiReference }
    public enum CleanlinessLevel { Clean, Light, Medium, Heavy }
    public enum NotificationType { Booking, Payment, Schedule, System, Ai }
}
