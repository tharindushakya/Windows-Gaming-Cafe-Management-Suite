using GamingCafe.Core.Models;

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
    Task<IEnumerable<string>> GetPermissionsAsync(int userId);

    // Wallet management
    Task<decimal> GetWalletBalanceAsync(int userId);
    Task<bool> AddFundsAsync(int userId, decimal amount, string description = "");
    Task<bool> DeductFundsAsync(int userId, decimal amount, string description = "");
}

public interface IAuthenticationService
{
    // Authentication
    Task<string?> AuthenticateAsync(string username, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<User?> GetUserFromTokenAsync(string token);
    Task LogoutAsync(string token);
    Task<bool> RefreshTokenAsync(string refreshToken);

    // Session management
    Task<string> CreateSessionAsync(int userId);
    Task<bool> ValidateSessionAsync(string sessionId);
    Task EndSessionAsync(string sessionId);
    Task<TimeSpan> GetSessionDurationAsync(string sessionId);
}

public interface IGameSessionService
{
    // Session management
    Task<GameSession> StartSessionAsync(int userId, int stationId);
    Task<GameSession> EndSessionAsync(int sessionId);
    Task<GameSession?> GetActiveSessionAsync(int userId);
    Task<GameSession?> GetSessionByIdAsync(int sessionId);
    Task<IEnumerable<GameSession>> GetUserSessionsAsync(int userId, int page, int pageSize);
    Task<bool> PauseSessionAsync(int sessionId);
    Task<bool> ResumeSessionAsync(int sessionId);

    // Session monitoring
    Task<TimeSpan> GetCurrentSessionDurationAsync(int sessionId);
    Task<decimal> CalculateSessionCostAsync(int sessionId);
    Task<bool> ExtendSessionAsync(int sessionId, TimeSpan additionalTime);
    Task<IEnumerable<GameSession>> GetActiveSessionsAsync();
}

public interface IGameStationService
{
    // Station management
    Task<GameStation?> GetStationByIdAsync(int stationId);
    Task<IEnumerable<GameStation>> GetAllStationsAsync();
    Task<IEnumerable<GameStation>> GetAvailableStationsAsync();
    Task<GameStation> CreateStationAsync(GameStation station);
    Task<GameStation> UpdateStationAsync(GameStation station);
    Task DeleteStationAsync(int stationId);

    // Station status
    Task<bool> SetStationStatusAsync(int stationId, string status);
    Task<string> GetStationStatusAsync(int stationId);
    Task<bool> IsStationAvailableAsync(int stationId);
    Task<bool> AssignUserToStationAsync(int stationId, int userId);
    Task<bool> ReleaseStationAsync(int stationId);
}

public interface IReservationService
{
    // Reservation management
    Task<Reservation> CreateReservationAsync(Reservation reservation);
    Task<Reservation?> GetReservationByIdAsync(int reservationId);
    Task<IEnumerable<Reservation>> GetUserReservationsAsync(int userId);
    Task<Reservation> UpdateReservationAsync(Reservation reservation);
    Task CancelReservationAsync(int reservationId);

    // Availability checking
    Task<bool> IsTimeSlotAvailableAsync(int stationId, DateTime startTime, DateTime endTime);
    Task<IEnumerable<DateTime>> GetAvailableTimeSlotsAsync(int stationId, DateTime date);
    Task<bool> CheckReservationConflictAsync(int stationId, DateTime startTime, DateTime endTime, int? excludeReservationId = null);
}

public interface IPaymentService
{
    // Payment processing
    Task<bool> ProcessPaymentAsync(int userId, decimal amount, string description);
    Task<bool> RefundPaymentAsync(int transactionId, decimal? refundAmount = null);
    Task<Transaction?> GetTransactionByIdAsync(int transactionId);
    Task<IEnumerable<Transaction>> GetUserTransactionsAsync(int userId, int page, int pageSize);

    // Wallet operations
    Task<bool> AddPaymentMethodAsync(int userId, string paymentMethodData);
    Task<bool> RemovePaymentMethodAsync(int userId, string paymentMethodId);
    Task<IEnumerable<string>> GetUserPaymentMethodsAsync(int userId);
}

public interface IInventoryService
{
    // Product management
    Task<Product?> GetProductByIdAsync(int productId);
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task DeleteProductAsync(int productId);

    // Stock management
    Task<int> GetStockQuantityAsync(int productId);
    Task<bool> UpdateStockAsync(int productId, int quantity);
    Task<bool> IsProductAvailableAsync(int productId, int requestedQuantity = 1);
    Task<bool> ReserveStockAsync(int productId, int quantity);
    Task<bool> ReleaseStockAsync(int productId, int quantity);
}

public interface ILoyaltyProgramService
{
    // Loyalty program management
    Task<LoyaltyProgram?> GetLoyaltyProgramAsync(int userId);
    Task<int> GetUserPointsAsync(int userId);
    Task<bool> AddPointsAsync(int userId, int points, string reason);
    Task<bool> RedeemPointsAsync(int userId, int points, string description);
    Task<IEnumerable<object>> GetAvailableRewardsAsync();
    Task<bool> ClaimRewardAsync(int userId, int rewardId);
}

public interface IReportingService
{
    // Revenue reports
    Task<decimal> GetDailyRevenueAsync(DateTime date);
    Task<decimal> GetMonthlyRevenueAsync(int year, int month);
    Task<object> GetRevenueReportAsync(DateTime startDate, DateTime endDate);

    // Usage reports
    Task<object> GetStationUsageReportAsync(DateTime startDate, DateTime endDate);
    Task<object> GetUserActivityReportAsync(DateTime startDate, DateTime endDate);
    Task<object> GetPopularGamesReportAsync(DateTime startDate, DateTime endDate);

    // Performance metrics
    Task<object> GetSystemPerformanceMetricsAsync();
    Task<object> GetCustomerSatisfactionMetricsAsync();
}
