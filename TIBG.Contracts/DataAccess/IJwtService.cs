namespace TIBG.Contracts.DataAccess
{
    /// <summary>
    /// Interface for JWT token service
    /// </summary>
    public interface IJwtService
    {
        string GenerateToken(int userId, string email, string username);
        int? ValidateToken(string token);
    }
}
