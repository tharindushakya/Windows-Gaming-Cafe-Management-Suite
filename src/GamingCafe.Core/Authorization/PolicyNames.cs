namespace GamingCafe.Core.Authorization;

public static class PolicyNames
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManagerOrAdmin = "RequireManagerOrAdmin";
    public const string RequireStationScope = "RequireStationScope";
    public const string RequireOwnerOrAdmin = "RequireOwnerOrAdmin";
}
