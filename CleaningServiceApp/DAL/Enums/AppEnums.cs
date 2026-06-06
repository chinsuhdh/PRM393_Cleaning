namespace Cleaning.DAL.Enums
{
    public enum UserRole { Client, Worker, Admin }
    public enum AccountStatus { Active, Banned, PendingVerification }
    public enum ServiceUnitType { Hour, SquareMeter, Package }
    public enum BookingStatus { Pending, Accepted, InProgress, Completed, Cancelled }
    public enum PaymentMethod { Cash, MoMo, VNPay, ZaloPay, BankTransfer }
    public enum PaymentStatus { Pending, Success, Failed, Refunded }
    public enum AiSenderType { User, Ai }
    public enum LogLevelType { Info, Warning, Error, Critical }
    public enum DeployStatusType { Success, Failed, InProgress }
}