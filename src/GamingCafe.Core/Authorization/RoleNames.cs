namespace GamingCafe.Core.Authorization
{
    /// <summary>
    /// Centralized role name constants to avoid scattered magic strings and typos.
    /// </summary>
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Administrator = "Administrator"; // sometimes used in legacy checks
    }
}
