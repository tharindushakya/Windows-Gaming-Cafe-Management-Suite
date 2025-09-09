using System;
using Microsoft.Extensions.DependencyInjection;

namespace GamingCafe.API.Background
{
    public static class StaticServiceProvider
    {
        public static IServiceProvider? ServiceProvider { get; set; }
    }
}
