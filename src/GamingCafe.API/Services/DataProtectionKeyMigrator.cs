using System.Xml.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace GamingCafe.API.Services;

public class DataProtectionKeyMigrator : IHostedService
{
    private readonly ILogger<DataProtectionKeyMigrator> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DataProtectionKeyMigrator(ILogger<DataProtectionKeyMigrator> logger, IWebHostEnvironment env, IConfiguration config)
    {
        _logger = logger;
        _env = env;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var useRedis = _config.GetValue<bool?>("DataProtection:UseRedis") ?? true;
        if (!useRedis)
        {
            _logger.LogInformation("DataProtection:UseRedis is disabled; skipping migration.");
            return;
        }

        var redisConfig = _config.GetConnectionString("Redis") ?? "localhost:6379";

        var keysDir = Path.Combine(_env.ContentRootPath, "keys");
        if (!Directory.Exists(keysDir))
        {
            _logger.LogInformation("No local DataProtection keys directory found at {keysDir}; nothing to migrate.", keysDir);
            return;
        }

        _logger.LogInformation("Attempting to connect to Redis to migrate DataProtection keys...");

        try
        {
            using var conn = await ConnectionMultiplexer.ConnectAsync(redisConfig);

            // Load the assembly and type that implements RedisXmlRepository
            // Ensure the StackExchange Redis DataProtection assembly is loaded. Try explicit load first,
            // then fall back to scanning already-loaded assemblies.
            Assembly? asm = null;
            try
            {
                asm = Assembly.Load(new AssemblyName("Microsoft.AspNetCore.DataProtection.StackExchangeRedis"));
            }
            catch
            {
                // ignored, we'll try scanning loaded assemblies
            }

            if (asm == null)
            {
                asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Microsoft.AspNetCore.DataProtection.StackExchangeRedis");
            }

            if (asm == null)
            {
                // Try loading from the application base directory (where runtime assemblies live)
                try
                {
                    var candidate = Path.Combine(AppContext.BaseDirectory ?? _env.ContentRootPath, "Microsoft.AspNetCore.DataProtection.StackExchangeRedis.dll");
                    if (File.Exists(candidate))
                    {
                        asm = Assembly.LoadFrom(candidate);
                    }
                }
                catch (Exception loadEx)
                {
                    _logger.LogDebug(loadEx, "Assembly.LoadFrom failed for DataProtection StackExchangeRedis assembly");
                }
            }

            if (asm == null)
            {
                _logger.LogWarning("DataProtection Redis assembly not found on disk or loaded; migration cannot proceed.");
                return;
            }

            var repoType = asm.GetType("Microsoft.AspNetCore.DataProtection.StackExchangeRedis.RedisXmlRepository");
            if (repoType == null)
            {
                _logger.LogWarning("RedisXmlRepository type not found in assembly; dumping available types for diagnosis.");
                foreach (var t in asm.GetTypes().Where(t => t.Name.IndexOf("Redis", StringComparison.OrdinalIgnoreCase) >= 0 || t.Name.IndexOf("XmlRepository", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _logger.LogInformation("Found type: {type}", t.FullName);
                    foreach (var c in t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        _logger.LogInformation("  ctor: {ctor}", c.ToString());
                    }
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.Name.IndexOf("Store", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("Element", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name));
                            _logger.LogInformation("  method: {method} ( {params} )", m.Name, ps);
                        }
                    }
                }

                return;
            }

            // Try common constructor shapes
            var possibleCtors = new[] {
                new Type[] { typeof(IConnectionMultiplexer), typeof(string) },
                new Type[] { typeof(IDatabase), typeof(string) },
                new Type[] { typeof(Func<IDatabase>), typeof(RedisKey) },
                new Type[] { typeof(Func<IDatabase>), typeof(string) },
                new Type[] { typeof(object), typeof(string) }
            };

            ConstructorInfo? ctor = null;
            foreach (var shape in possibleCtors)
            {
                ctor = repoType.GetConstructor(shape);
                if (ctor != null) break;
            }

            if (ctor == null)
            {
                _logger.LogWarning("Could not find a known RedisXmlRepository constructor; available ctors:");
                foreach (var c in repoType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    _logger.LogWarning("  {ctor}", c.ToString());
                }
                return;
            }

            object repoInstance;
            var firstParam = ctor.GetParameters()[0].ParameterType;
            var secondParam = ctor.GetParameters().Length > 1 ? ctor.GetParameters()[1].ParameterType : typeof(string);

            if (firstParam == typeof(IConnectionMultiplexer))
            {
                // old shape: (ConnectionMultiplexer, string)
                repoInstance = ctor.Invoke(new object[] { conn, "GamingCafe-DataProtection-Keys" });
            }
            else if (firstParam.FullName == "StackExchange.Redis.IDatabase" || firstParam == typeof(IDatabase))
            {
                var db = conn.GetDatabase();
                // accept either RedisKey or string
                object secondArg = secondParam == typeof(RedisKey) ? (object)new RedisKey("GamingCafe-DataProtection-Keys") : "GamingCafe-DataProtection-Keys";
                repoInstance = ctor.Invoke(new object[] { db, secondArg });
            }
            else if (firstParam.IsGenericType && firstParam.GetGenericTypeDefinition() == typeof(Func<>))
            {
                var genArg = firstParam.GetGenericArguments()[0];
                if (genArg.FullName == "StackExchange.Redis.IDatabase" || genArg == typeof(IDatabase))
                {
                    Func<IDatabase> dbFactory = () => conn.GetDatabase();
                    object secondArg = secondParam == typeof(RedisKey) ? (object)new RedisKey("GamingCafe-DataProtection-Keys") : "GamingCafe-DataProtection-Keys";
                    repoInstance = ctor.Invoke(new object[] { dbFactory, secondArg });
                }
                else
                {
                    _logger.LogWarning("Func<> generic argument not recognized: {arg}", genArg.FullName);
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Constructor parameter type not recognized: {type}", firstParam.FullName);
                return;
            }

            var storeMethod = repoType.GetMethod("StoreElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (storeMethod == null)
            {
                _logger.LogWarning("StoreElement method not found on RedisXmlRepository; logging available methods for diagnosis.");
                foreach (var m in repoType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name.IndexOf("Store", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("Element", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var ps = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name));
                        _logger.LogInformation("  method: {method} ( {params} )", m.Name, ps);
                    }
                }
                return;
            }

            var files = Directory.GetFiles(keysDir, "*.xml");
            if (files.Length == 0)
            {
                _logger.LogInformation("No key XML files found in {keysDir}; nothing to migrate.", keysDir);
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var x = XElement.Load(file);
                    var friendly = Path.GetFileName(file);
                    storeMethod.Invoke(repoInstance, new object[] { x, friendly });
                    _logger.LogInformation("Migrated data-protection key file {file} into Redis.", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to migrate key file {file}; continuing.", file);
                }
            }

            _logger.LogInformation("DataProtection key migration completed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during DataProtection key migration.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
