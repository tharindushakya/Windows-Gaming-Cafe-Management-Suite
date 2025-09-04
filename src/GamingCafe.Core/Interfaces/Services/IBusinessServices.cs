using GamingCafe.Core.Models;
using GamingCafe.Core.Models.Common;

namespace GamingCafe.Core.Interfaces.Services;

public interface IUserService
{
    // User management
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByEmailAsync(string email);
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, string? searchTerm = null);
    Task<User> CreateUserAsync(User user, string password);
    Task<User> UpdateUserAsync(User user);
    Task DeleteUserAsync(int userId);
    Task<bool> SoftDeleteUserAsync(int userId);
    Task<bool> RestoreUserAsync(int userId);

    // Authentication
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(int userId);
    Task<bool> ResetPasswordAsync(int userId, string token, string newPassword);

    // User roles and permissions
    Task<bool> AssignRoleAsync(int userId, string role);
    Task<bool> RemoveRoleAsync(int userId, string role);
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, string permission);

    // User wallet operations
    Task<decimal> GetWalletBalanceAsync(int userId);
    Task<bool> AddFundsAsync(int userId, decimal amount, string description = "Funds added");
    Task<bool> DeductFundsAsync(int userId, decimal amount, string description = "Funds deducted");
    Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(int userId, int page = 1, int pageSize = 10);

    // User activity
    Task<DateTime?> GetLastLoginAsync(int userId);
    Task UpdateLastLoginAsync(int userId);
    Task<bool> IsUserActiveAsync(int userId);
    Task<UserStatistics> GetUserStatisticsAsync(int userId);
}

public interface IAuthenticationService
{
    // Authentication
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<bool> RevokeAllTokensAsync(int userId);

    // Registration
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);
    Task<bool> ConfirmEmailAsync(int userId, string token);
    Task<bool> ResendEmailConfirmationAsync(string email);

    // Two-factor authentication
    Task<bool> EnableTwoFactorAsync(int userId);
    Task<bool> DisableTwoFactorAsync(int userId);
    Task<string> GenerateTwoFactorTokenAsync(int userId);
    Task<bool> ValidateTwoFactorTokenAsync(int userId, string token);

    // Password management
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    // Session management
    Task<bool> InvalidateUserSessionsAsync(int userId);
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(int userId);
}

public interface IGameSessionService
{
    // Session management
    Task<GameSession?> GetSessionByIdAsync(int sessionId);
    Task<GameSession?> GetActiveSessionByUserAsync(int userId);
    Task<GameSession?> GetActiveSessionByStationAsync(int stationId);
    Task<(IEnumerable<GameSession> Sessions, int TotalCount)> GetSessionsAsync(int page, int pageSize, SessionFilter? filter = null);
    
    // Session operations
    Task<GameSession> StartSessionAsync(int userId, int stationId, decimal? hourlyRate = null);
    Task<GameSession> EndSessionAsync(int sessionId);
    Task<GameSession> PauseSessionAsync(int sessionId);
    Task<GameSession> ResumeSessionAsync(int sessionId);
    Task<GameSession> ExtendSessionAsync(int sessionId, TimeSpan extension);

    // Session billing
    Task<decimal> CalculateSessionCostAsync(int sessionId);
    Task<decimal> GetCurrentSessionCostAsync(int sessionId);
    Task<bool> ProcessSessionPaymentAsync(int sessionId, PaymentMethod paymentMethod);

    // Session statistics
    Task<SessionStatistics> GetSessionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<UserSessionSummary>> GetUserSessionSummaryAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
}

public interface IStationService
{
    // Station management
    Task<GameStation?> GetStationByIdAsync(int stationId);
    Task<(IEnumerable<GameStation> Stations, int TotalCount)> GetStationsAsync(int page, int pageSize, StationFilter? filter = null);
    Task<GameStation> CreateStationAsync(GameStation station);
    Task<GameStation> UpdateStationAsync(GameStation station);
    Task DeleteStationAsync(int stationId);

    // Station availability
    Task<bool> IsStationAvailableAsync(int stationId);
    Task<bool> SetStationAvailabilityAsync(int stationId, bool isAvailable, string? reason = null);
    Task<IEnumerable<GameStation>> GetAvailableStationsAsync();

    // Station status
    Task<StationStatus> GetStationStatusAsync(int stationId);
    Task<bool> UpdateStationStatusAsync(int stationId, StationStatus status);
    Task<IEnumerable<StationStatusSummary>> GetAllStationStatusesAsync();

    // Maintenance
    Task<bool> ScheduleMaintenanceAsync(int stationId, DateTime scheduledDate, string description);
    Task<bool> CompleteMaintenanceAsync(int stationId, string notes);
    Task<IEnumerable<MaintenanceRecord>> GetMaintenanceHistoryAsync(int stationId);
}

public interface IReservationService
{
    // Reservation management
    Task<Reservation?> GetReservationByIdAsync(int reservationId);
    Task<(IEnumerable<Reservation> Reservations, int TotalCount)> GetReservationsAsync(int page, int pageSize, ReservationFilter? filter = null);
    Task<Reservation> CreateReservationAsync(CreateReservationRequest request);
    Task<Reservation> UpdateReservationAsync(int reservationId, UpdateReservationRequest request);
    Task<bool> CancelReservationAsync(int reservationId, string? reason = null);

    // Reservation validation
    Task<bool> IsTimeSlotAvailableAsync(int stationId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeReservationId = null);
    Task<IEnumerable<TimeSlot>> GetAvailableTimeSlotsAsync(int stationId, DateTime date);
    Task<bool> ValidateReservationAsync(CreateReservationRequest request);

    // Reservation status
    Task<bool> CheckInReservationAsync(int reservationId);
    Task<bool> NoShowReservationAsync(int reservationId);
    Task<bool> CompleteReservationAsync(int reservationId);

    // Reservation statistics
    Task<ReservationStatistics> GetReservationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Reservation>> GetUpcomingReservationsAsync(int? userId = null);
}

public interface IPaymentService
{
    // Payment processing
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> ProcessRefundAsync(int transactionId, decimal amount, string reason);
    Task<PaymentResult> ProcessWalletPaymentAsync(int userId, decimal amount, string description);

    // Payment methods
    Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync(int userId);
    Task<PaymentMethod> AddPaymentMethodAsync(int userId, AddPaymentMethodRequest request);
    Task<bool> RemovePaymentMethodAsync(int paymentMethodId);
    Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId);

    // Transaction history
    Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetTransactionHistoryAsync(int userId, int page, int pageSize);
    Task<Transaction?> GetTransactionByIdAsync(int transactionId);
    Task<TransactionStatistics> GetTransactionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    // Payment gateway integration
    Task<bool> ValidatePaymentGatewayAsync();
    Task<PaymentGatewayStatus> GetPaymentGatewayStatusAsync();
}

// Supporting models and enums
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresTwoFactor { get; set; }
}

public class RegistrationResult
{
    public bool Success { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
}

public class UserStatistics
{
    public int TotalSessions { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public decimal TotalSpent { get; set; }
    public int LoyaltyPoints { get; set; }
    public DateTime LastActivity { get; set; }
}

public class SessionStatistics
{
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSessionDuration { get; set; }
}

public class ReservationStatistics
{
    public int TotalReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int CancelledReservations { get; set; }
    public int NoShowReservations { get; set; }
    public decimal CompletionRate { get; set; }
}

public class TransactionStatistics
{
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public int RefundCount { get; set; }
    public decimal RefundAmount { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Cancelled
}

public enum PaymentGatewayStatus
{
    Online,
    Offline,
    Maintenance,
    Error
}
