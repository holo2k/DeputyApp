namespace Shared.Middleware;

public interface IBlackListService
{
    bool IsTokenBlacklisted(string token);
    void AddTokenToBlacklist(string token);
}