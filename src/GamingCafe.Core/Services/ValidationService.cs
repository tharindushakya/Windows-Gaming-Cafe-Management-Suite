using System.Text.RegularExpressions;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces.Services;

namespace GamingCafe.Core.Services;

public class ValidationService : IValidationService
{
    private const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    public ValidationResult ValidateUser(User user)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(user.Username))
            errors.Add("Username is required");
        else if (user.Username.Length < 3)
            errors.Add("Username must be at least 3 characters long");
        else if (user.Username.Length > 50)
            errors.Add("Username cannot exceed 50 characters");

        if (string.IsNullOrWhiteSpace(user.Email))
            errors.Add("Email is required");
        else if (!ValidateEmail(user.Email).IsValid)
            errors.Add("Invalid email format");

        if (string.IsNullOrWhiteSpace(user.FirstName))
            errors.Add("First name is required");

        if (string.IsNullOrWhiteSpace(user.LastName))
            errors.Add("Last name is required");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateGameSession(GameSession session)
    {
        var errors = new List<string>();

        if (session.UserId <= 0)
            errors.Add("User ID is required");

        if (session.StationId <= 0)
            errors.Add("Station ID is required");

        if (session.StartTime >= session.EndTime)
            errors.Add("End time must be after start time");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateReservation(Reservation reservation)
    {
        var errors = new List<string>();

        if (reservation.UserId <= 0)
            errors.Add("User ID is required");

        if (reservation.StationId <= 0)
            errors.Add("Station ID is required");

        if (reservation.StartTime <= DateTime.UtcNow)
            errors.Add("Reservation start time must be in the future");

        if (reservation.StartTime >= reservation.EndTime)
            errors.Add("End time must be after start time");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateTransaction(Transaction transaction)
    {
        var errors = new List<string>();

        if (transaction.UserId <= 0)
            errors.Add("User ID is required");

        if (transaction.Amount <= 0)
            errors.Add("Amount must be greater than zero");

        // Transaction type validation removed since it's an enum and can't be null

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
        else if (!Regex.IsMatch(email, EmailPattern))
        {
            errors.Add("Invalid email format");
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
        }
        else
        {
            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Password must contain at least one special character");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public Task<ValidationResult> ValidateUniqueUsernameAsync(string username, int? excludeUserId = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username))
            errors.Add("Username is required");

        return Task.FromResult(new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        });
    }

    public Task<ValidationResult> ValidateUniqueEmailAsync(string email, int? excludeUserId = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email is required");
        else if (!ValidateEmail(email).IsValid)
            errors.Add("Invalid email format");

        return Task.FromResult(new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        });
    }
}
