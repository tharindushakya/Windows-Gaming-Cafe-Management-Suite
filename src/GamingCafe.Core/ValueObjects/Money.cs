using System;

namespace GamingCafe.Core.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency = "USD")
{
    public Money Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot add Money with different currencies");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot subtract Money with different currencies");
        return new Money(Amount - other.Amount, Currency);
    }
}
