namespace Eatah.Client.Services;

public static class ApiClientOptions
{
    public static Uri GetBaseAddress()
    {
#if ANDROID
        // Android emulator maps host's localhost to 10.0.2.2
        return new Uri("http://10.0.2.2:5092/");
#else
        return new Uri("http://localhost:5092/");
#endif
    }
}
