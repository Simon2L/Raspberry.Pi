using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;

namespace Raspberry.Pi.Dashboard.Integration;

public interface ISLApiService
{
    Task<DeparturesResponse> GetDeparturesAsync(Sites site, CancellationToken cancellationToken = default);
}

public enum Sites
{
    Zinken = 9296,
    Kista = 9302,
    Telefonplan = 9263
}

public class SLApiService(HttpClient httpClient, ILogger<SLApiService> logger, IMemoryCache cache) : ISLApiService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<SLApiService> _logger = logger;
    private readonly IMemoryCache _cache = cache;

    public async Task<DeparturesResponse> GetDeparturesAsync(Sites site, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(site, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await FetchDeparturesAsync(site, cancellationToken);
        }) ?? new();
    }

    // 9302 ZINKEN
    // 9296 KISTA
    // 9263 TELEFONPLAN

    // https://www.trafiklab.se/sv/api/our-apis/sl/transport/#/default/Departures
    private async Task<DeparturesResponse> FetchDeparturesAsync(Sites site, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<DeparturesResponse>(
                $"sites/{(int)site}/departures?forecast=30",
                cancellationToken
            );
            return result ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API error fetching departures");
            return new();
        }
    }
}

public class DeparturesResponse
{
    [JsonPropertyName("departures")]
    public List<Departure> Departures { get; set; } = new();
    
    [JsonIgnore]
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

public class Departure
{
    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("direction_code")]
    public int DirectionCode { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public string Display { get; set; } = string.Empty;

    [JsonPropertyName("scheduled")]
    public DateTime Scheduled { get; set; }

    [JsonPropertyName("expected")]
    public DateTime Expected { get; set; }

    [JsonPropertyName("journey")]
    public Journey Journey { get; set; } = new();

    [JsonPropertyName("stop_area")]
    public StopArea StopArea { get; set; } = new();

    [JsonPropertyName("stop_point")]
    public StopPoint StopPoint { get; set; } = new();

    [JsonPropertyName("line")]
    public Line Line { get; set; } = new();

    [JsonPropertyName("deviations")]
    public List<object> Deviations { get; set; } = new(); // unknown type, so keep object
}

public class Journey
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("prediction_state")]
    public string PredictionState { get; set; } = string.Empty;
}

public class StopArea
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class StopPoint
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("designation")]
    public string Designation { get; set; } = string.Empty;
}

public class Line
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("designation")]
    public string Designation { get; set; } = string.Empty;

    [JsonPropertyName("transport_authority_id")]
    public int TransportAuthorityId { get; set; }

    [JsonPropertyName("transport_mode")]
    public string TransportMode { get; set; } = string.Empty;

    [JsonPropertyName("group_of_lines")]
    public string GroupOfLines { get; set; } = string.Empty;
}
