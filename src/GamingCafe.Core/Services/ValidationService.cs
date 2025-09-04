using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Data.Interfaces;

namespace GamingCafe.Core.Services;

public class ValidationService : IValidationService
{
    private readonly IUnitOfWork _unitOfWork;
    private const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    private const string PhonePattern = @"^[\+]?[1-9][\d]{0,15}$";

    public ValidationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public ValidationResult ValidateUser(User user)
    {
        var errors = new List<string>();

        // Required fields validation
        if (string.IsNullOrWhiteSpace(user.Username))
            errors.Add("Username is required");
        else if (user.Username.Length < 3)
            errors.Add("Username must be at least 3 characters long");
        else if (user.Username.Length > 50)
            errors.Add("Username cannot exceed 50 characters");
        else if (!IsValidUsername(user.Username))
            errors.Add("Username can only contain letters, numbers, and underscores");

        if (string.IsNullOrWhiteSpace(user.Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(user.Email))
            errors.Add("Invalid email format");

        if (string.IsNullOrWhiteSpace(user.FirstName))
            errors.Add("First name is required");
        else if (user.FirstName.Length > 100)
            errors.Add("First name cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(user.LastName))
            errors.Add("Last name is required");
        else if (user.LastName.Length > 100)
            errors.Add("Last name cannot exceed 100 characters");

        // Phone validation (if provided)
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber) && !IsValidPhone(user.PhoneNumber))
            errors.Add("Invalid phone number format");

        // Date of birth validation
        if (user.DateOfBirth.HasValue)
        {
            if (user.DateOfBirth.Value > DateTime.Today)
                errors.Add("Date of birth cannot be in the future");
            
            var age = DateTime.Today.Year - user.DateOfBirth.Value.Year;
            if (user.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
                age--;
                
            if (age < 13)
                errors.Add("User must be at least 13 years old");
        }

        // Wallet balance validation
        if (user.WalletBalance < 0)
            errors.Add("Wallet balance cannot be negative");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateGameSession(GameSession session)
    {
        var errors = new List<string>();

        // User validation
        if (session.UserId <= 0)
            errors.Add("Valid user ID is required");

        // Station validation
        if (session.StationId <= 0)
            errors.Add("Valid station ID is required");

        // Time validation
        if (session.StartTime == default)
            errors.Add("Start time is required");

        if (session.EndTime.HasValue && session.EndTime.Value <= session.StartTime)
            errors.Add("End time must be after start time");

        // Rate validation
        if (session.HourlyRate < 0)
            errors.Add("Hourly rate cannot be negative");

        // Status validation
        if (!Enum.IsDefined(typeof(SessionStatus), session.Status))
            errors.Add("Invalid session status");

        // Business rule: Session cannot be longer than 24 hours
        if (session.EndTime.HasValue)
        {
            var duration = session.EndTime.Value - session.StartTime;
            if (duration.TotalHours > 24)
                errors.Add("Session duration cannot exceed 24 hours");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateReservation(Reservation reservation)
    {
        var errors = new List<string>();

        // User validation
        if (reservation.UserId <= 0)
            errors.Add("Valid user ID is required");

        // Station validation
        if (reservation.StationId <= 0)
            errors.Add("Valid station ID is required");

        // Date validation
        if (reservation.ReservationDate.Date < DateTime.Today)
            errors.Add("Reservation date cannot be in the past");

        // Time validation
        if (reservation.StartTime >= reservation.EndTime)
            errors.Add("Start time must be before end time");

        // Duration validation
        var duration = reservation.EndTime - reservation.StartTime;
        if (duration.TotalMinutes < 30)
            errors.Add("Minimum reservation duration is 30 minutes");
        
        if (duration.TotalHours > 12)
            errors.Add("Maximum reservation duration is 12 hours");

        // Advance booking validation (max 30 days in advance)
        if (reservation.ReservationDate.Date > DateTime.Today.AddDays(30))
            errors.Add("Reservations cannot be made more than 30 days in advance");

        // Cost validation
        if (reservation.EstimatedCost < 0)
            errors.Add("Estimated cost cannot be negative");

        // Status validation
        if (!Enum.IsDefined(typeof(ReservationStatus), reservation.Status))
            errors.Add("Invalid reservation status");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateTransaction(Transaction transaction)
    {
        var errors = new List<string>();

        // User validation
        if (transaction.UserId <= 0)
            errors.Add("Valid user ID is required");

        // Amount validation
        if (transaction.Amount == 0)
            errors.Add("Transaction amount cannot be zero");

        // Type validation
        if (!Enum.IsDefined(typeof(TransactionType), transaction.Type))
            errors.Add("Invalid transaction type");

        // Description validation
        if (string.IsNullOrWhiteSpace(transaction.Description))
            errors.Add("Transaction description is required");
        else if (transaction.Description.Length > 500)
            errors.Add("Transaction description cannot exceed 500 characters");

        // Payment method validation (if provided)
        if (!string.IsNullOrWhiteSpace(transaction.PaymentMethod))
        {
            var validPaymentMethods = new[] { "Cash", "Card", "Wallet", "Bank Transfer", "Mobile Payment" };
            if (!validPaymentMethods.Contains(transaction.PaymentMethod))
                errors.Add("Invalid payment method");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return new ValidationResult { IsValid = false, Errors = errors };
        }

        // Length validation
        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters long");

        if (password.Length > 128)
            errors.Add("Password cannot exceed 128 characters");

        // Complexity validation
        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
            errors.Add("Password must contain at least one special character");

        // Common password validation
        var commonPasswords = new[]
        {
            "password", "123456", "123456789", "qwerty", "abc123",
            "password123", "admin", "administrator", "user", "guest"
        };

        if (commonPasswords.Contains(password.ToLower()))
            errors.Add("Password is too common. Please choose a more secure password");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateEmail(string email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required");
        }
        else
        {
            if (!IsValidEmail(email))
                errors.Add("Invalid email format");

            if (email.Length > 255)
                errors.Add("Email cannot exceed 255 characters");

            // Check for disposable email domains
            var disposableDomains = new[]
            {
                "10minutemail.com", "tempmail.org", "guerrillamail.com",
                "mailinator.com", "throwaway.email"
            };

            var domain = email.Split('@').LastOrDefault()?.ToLower();
            if (domain != null && disposableDomains.Contains(domain))
                errors.Add("Disposable email addresses are not allowed");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public async Task<ValidationResult> ValidateUniqueUsernameAsync(string username, int? excludeUserId = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Username is required");
        }
        else
        {
            var existingUser = await _unitOfWork.Repository<User>()
                .FindFirstAsync(u => u.Username.ToLower() == username.ToLower());

            if (existingUser != null && (!excludeUserId.HasValue || existingUser.UserId != excludeUserId.Value))
                errors.Add("Username is already taken");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public async Task<ValidationResult> ValidateUniqueEmailAsync(string email, int? excludeUserId = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required");
        }
        else
        {
            var existingUser = await _unitOfWork.Repository<User>()
                .FindFirstAsync(u => u.Email.ToLower() == email.ToLower());

            if (existingUser != null && (!excludeUserId.HasValue || existingUser.UserId != excludeUserId.Value))
                errors.Add("Email is already registered");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    // Helper methods
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email && Regex.IsMatch(email, EmailPattern);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove spaces, dashes, and parentheses
        var cleanPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");
        
        return Regex.IsMatch(cleanPhone, PhonePattern);
    }

    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        // Username can contain letters, numbers, and underscores only
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
    }
}

// Custom validation result class
public class ValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();

    public void AddError(string error)
    {
        var errorList = Errors.ToList();
        errorList.Add(error);
        Errors = errorList;
        IsValid = false;
    }

    public void AddErrors(IEnumerable<string> errors)
    {
        var errorList = Errors.ToList();
        errorList.AddRange(errors);
        Errors = errorList;
        IsValid = !errorList.Any();
    }
}
