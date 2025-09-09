using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace GamingCafe.Data.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context == null) return;
        var entries = context.ChangeTracker.Entries().Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || e.State == Microsoft.EntityFrameworkCore.EntityState.Modified);
        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            {
                if (entry.Metadata.FindProperty("CreatedAt") != null)
                    entry.Property("CreatedAt").CurrentValue = now;
            }
            else
            {
                if (entry.Metadata.FindProperty("UpdatedAt") != null)
                    entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
