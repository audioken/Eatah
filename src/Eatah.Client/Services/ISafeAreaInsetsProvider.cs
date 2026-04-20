namespace Eatah.Client.Services;

public interface ISafeAreaInsetsProvider
{
    Task<(double Top, double Bottom)> GetInsetsAsync();
}
