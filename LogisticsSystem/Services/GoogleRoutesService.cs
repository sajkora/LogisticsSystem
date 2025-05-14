using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class GoogleRoutesService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GoogleRoutesService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _apiKey = config["GoogleAPI:Key"];
    }

    public async Task<string> GetRoute(string origin, string destination)
    {
        string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}&key={_apiKey}";
        return await _httpClient.GetStringAsync(url);
    }
}
