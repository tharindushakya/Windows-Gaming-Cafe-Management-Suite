
using System;
using System.Text.RegularExpressions;

namespace GamingCafe.Core.ValueObjects;

public sealed record EmailAddress
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Email is required", nameof(value));
        if (!EmailRegex.IsMatch(value)) throw new ArgumentException("Invalid email format", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string value) => new EmailAddress(value);
}
