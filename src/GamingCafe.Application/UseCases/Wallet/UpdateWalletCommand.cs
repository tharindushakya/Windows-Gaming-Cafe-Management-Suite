using System;

namespace GamingCafe.Application.UseCases.Wallet
{
    public record UpdateWalletCommand(int UserId, decimal Amount, string Reason);
}
