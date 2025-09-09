using System;
using System.Threading.Tasks;

namespace GamingCafe.API.Background
{
    public interface IOutboxDispatcher
    {
        Task DispatchAsync(Guid messageId);
    }
}
