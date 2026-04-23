namespace Eatah.Client.Services;

public static class ApiClientOptions
{
    public static Uri GetBaseAddress()
    {
        return new Uri("https://eatah.onrender.com/");
    }
}
