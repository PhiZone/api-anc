namespace PhiZoneApi.Utils;

public static class HttpClientUtil
{
    public static async Task<bool> TestConnect(this HttpClient client, string uri, double timeoutInSeconds = 5)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutInSeconds));
        try
        {
            return (await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), cts.Token)).IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}