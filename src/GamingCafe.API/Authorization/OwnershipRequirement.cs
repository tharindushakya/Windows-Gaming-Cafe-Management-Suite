using Microsoft.AspNetCore.Authorization;

namespace GamingCafe.API.Authorization;

public class OwnershipRequirement : IAuthorizationRequirement
{
    // marker requirement - handler will validate ownership or admin
}
