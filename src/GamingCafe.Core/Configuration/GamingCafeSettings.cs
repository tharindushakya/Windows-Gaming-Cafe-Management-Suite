namespace GamingCafe.Core.Configuration;

public class GamingCafeSettings
{
    public const string SectionName = "GamingCafe";

    public DatabaseSettings Database { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
    public CacheSettings Cache { get; set; } = new();
    public PaymentSettings Payment { get; set; } = new();
    public SessionSettings Session { get; set; } = new();
    public LoyaltySettings Loyalty { get; set; } = new();
    public FileStorageSettings FileStorage { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public RateLimitingSettings RateLimiting { get; set; } = new();
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool AutoMigrate { get; set; } = false;
    public bool SeedData { get; set; } = true;
}

public class SecuritySettings
{
    public JwtSettings Jwt { get; set; } = new();
    public PasswordSettings Password { get; set; } = new();
    public TwoFactorSettings TwoFactor { get; set; } = new();
    public CorsSettings Cors { get; set; } = new();
    public bool RequireHttps { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool EnableApiKeyAuthentication { get; set; } = false;
    public string[]? AllowedApiKeys { get; set; }
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GamingCafe";
    public string Audience { get; set; } = "GamingCafe";
    public TimeSpan AccessTokenExpiry { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenExpiry { get; set; } = TimeSpan.FromDays(7);
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}

public class PasswordSettings
{
    public int MinLength { get; set; } = 8;
    public int MaxLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    public string[] CommonPasswords { get; set; } = Array.Empty<string>();
}

public class TwoFactorSettings
{
    public bool Enabled { get; set; } = false;
    public string ApplicationName { get; set; } = "Gaming Cafe";
    public int TokenValidityMinutes { get; set; } = 5;
    public bool RequireForAdmins { get; set; } = true;
}

public class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    public string[] AllowedHeaders { get; set; } = { "*" };
    public bool AllowCredentials { get; set; } = true;
    public int PreflightMaxAge { get; set; } = 86400; // 24 hours
}

public class EmailSettings
{
    public bool Enabled { get; set; } = true;
    public SmtpSettings Smtp { get; set; } = new();
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Gaming Cafe";
    public string[] AdminEmails { get; set; } = Array.Empty<string>();
    public bool EnableEmailVerification { get; set; } = true;
    public TimeSpan VerificationTokenExpiry { get; set; } = TimeSpan.FromHours(24);
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30000; // 30 seconds
}

public class CacheSettings
{
    public bool Enabled { get; set; } = true;
    public string Type { get; set; } = "Memory"; // Memory, Redis
    public RedisSettings Redis { get; set; } = new();
    public MemoryCacheSettings Memory { get; set; } = new();
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "GamingCafe";
    public int Database { get; set; } = 0;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int ConnectRetry { get; set; } = 3;
}

public class MemoryCacheSettings
{
    public long SizeLimit { get; set; } = 100; // MB
    public TimeSpan CompactionPercentage { get; set; } = TimeSpan.FromMinutes(5);
}

public class PaymentSettings
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Mock"; // Stripe, PayPal, Square, Mock
    public StripeSettings Stripe { get; set; } = new();
    public PayPalSettings PayPal { get; set; } = new();
    public string[] AcceptedPaymentMethods { get; set; } = { "Cash", "Card", "Wallet" };
    public decimal MinimumWalletTopup { get; set; } = 5.00m;
    public decimal MaximumWalletBalance { get; set; } = 1000.00m;
    public bool EnableRefunds { get; set; } = true;
    public TimeSpan RefundProcessingTime { get; set; } = TimeSpan.FromMinutes(5);
}

public class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
}

public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox"; // sandbox, live
}

public class SessionSettings
{
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromHours(2);
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan MinDuration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ExtensionIncrement { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan WarningThreshold { get; set; } = TimeSpan.FromMinutes(10);
    public bool AutoEndOnTimeExpiry { get; set; } = true;
    public bool AllowSessionPause { get; set; } = true;
    public TimeSpan MaxPauseDuration { get; set; } = TimeSpan.FromMinutes(15);
    public decimal DefaultHourlyRate { get; set; } = 5.00m;
}

public class LoyaltySettings
{
    public bool Enabled { get; set; } = true;
    public decimal DefaultPointsPerDollar { get; set; } = 1.0m;
    public decimal DefaultRedemptionRate { get; set; } = 0.01m; // $0.01 per point
    public int MinimumPointsForRedemption { get; set; } = 100;
    public TimeSpan PointsExpiryDuration { get; set; } = TimeSpan.FromDays(365);
    public bool EnableTiers { get; set; } = true;
    public TierSettings[] Tiers { get; set; } = Array.Empty<TierSettings>();
}

public class TierSettings
{
    public string Name { get; set; } = string.Empty;
    public decimal MinimumSpend { get; set; }
    public decimal PointsMultiplier { get; set; } = 1.0m;
    public decimal RedemptionBonus { get; set; } = 0.0m;
}

public class FileStorageSettings
{
    public string Provider { get; set; } = "Local"; // Local, Azure, AWS, GCP
    public string BasePath { get; set; } = "wwwroot/uploads";
    public long MaxFileSize { get; set; } = 10485760; // 10MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt" };
    public string[] AllowedImageExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif" };
    public int ImageMaxWidth { get; set; } = 1920;
    public int ImageMaxHeight { get; set; } = 1080;
    public bool EnableImageCompression { get; set; } = true;
    public int ImageCompressionQuality { get; set; } = 85;
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;
    public SignalRSettings SignalR { get; set; } = new();
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnablePushNotifications { get; set; } = false;
    public bool EnableSmsNotifications { get; set; } = false;
    public TimeSpan NotificationRetention { get; set; } = TimeSpan.FromDays(30);
}

public class SignalRSettings
{
    public bool Enabled { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ClientTimeoutInterval { get; set; } = TimeSpan.FromSeconds(30);
}

public class RateLimitingSettings
{
    public bool Enabled { get; set; } = true;
    public GlobalRateLimit Global { get; set; } = new();
    public ApiRateLimit[] EndpointLimits { get; set; } = Array.Empty<ApiRateLimit>();
    public string[] WhitelistedIPs { get; set; } = Array.Empty<string>();
    public bool EnableApiKeyBypass { get; set; } = true;
}

public class GlobalRateLimit
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
}

public class ApiRateLimit
{
    public string Endpoint { get; set; } = string.Empty;
    public int RequestsPerMinute { get; set; } = 30;
    public int RequestsPerHour { get; set; } = 500;
    public string[] Methods { get; set; } = Array.Empty<string>();
}
