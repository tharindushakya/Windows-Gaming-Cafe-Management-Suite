namespace GamingCafe.Core.Authorization
{
    /// <summary>
    /// Centralized custom claim type names used across the application.
    /// </summary>
    public static class CustomClaimTypes
    {
        // Application-level permission claim (e.g. inv:write, txn:refund)
        public const string Permission = "permission";
    }
}
