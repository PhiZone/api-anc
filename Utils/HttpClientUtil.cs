namespace PhiZoneApi.Utils;

public static class HttpClientUtil
{
    public static async Task<bool> TestConnect(this HttpClient client, string uri, double timeoutInSeconds = 5)
    {
        var timeout = client.Timeout;
        bool result;
        client.Timeout = TimeSpan.FromSeconds(timeoutInSeconds);
        try
        {
            result = (await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri))).IsSuccessStatusCode;
        }
        catch (Exception)
        {
            result = false;
        }

        client.Timeout = timeout;
        return result;
    }
}