using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace GamingCafe.Data.Interceptors;

public class ConcurrencyLoggingInterceptor : SaveChangesInterceptor
{
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Exception is DbUpdateConcurrencyException dex)
        {
            // Append additional context if needed
            throw new InvalidOperationException("A concurrency conflict occurred while saving changes.", dex);
        }
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Exception is DbUpdateConcurrencyException dex)
        {
            throw new InvalidOperationException("A concurrency conflict occurred while saving changes.", dex);
        }
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
