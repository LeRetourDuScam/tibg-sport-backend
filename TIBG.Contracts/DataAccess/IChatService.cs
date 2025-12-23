using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(ChatRequest request);
    }
}
