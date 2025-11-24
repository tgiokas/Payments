using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Payments.Infrastructure.ApiClients;

public abstract class JccApiClient : ApiClientBase
{
    protected readonly IConfiguration? _configuration;
    private readonly IDistributedCache _cache;    
    protected readonly string _keycloakServerUrl;
    protected readonly string _realm;
    protected readonly string _clientId;
    protected readonly string _clientSecret;
    protected readonly string _authority;
    protected readonly string _redirectUri;
    protected string? _clientUuid;    
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };

    protected JccApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<JccApiClient> logger, IDistributedCache cache)
      : base(httpClient, logger)
    {
        _keycloakServerUrl = configuration["KEYCLOAK_BASEURL"] ?? configuration["Keycloak:BaseUrl"]
            ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_BASEURL is null.");
        _realm = configuration["KEYCLOAK_REALM"] ?? configuration["Keycloak:Realm"]
            ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_REALM is null.");
        _clientId = configuration["KEYCLOAK_CLIENTID"] ?? configuration["Keycloak:ClientId"]
            ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_CLIENTID is null.");
        _clientSecret = configuration["KEYCLOAK_CLIENTSECRET"] ?? configuration["Keycloak:ClientSecret"]
            ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_CLIENTSECRET is null.");
        _authority = configuration["KEYCLOAK_AUTHORITY"] ?? configuration["Keycloak:Authority"]
            ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_AUTHORITY is null.");
        _redirectUri = configuration["KEYCLOAK_REDIRECTURI"] ?? configuration["Keycloak:RedirectURI"]
            ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_REDIRECTURI is null.");

        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    // Authenticate Backend Services (Microservices, APIs) using Client Credentials Flow (Return a JWT Admin Token).
    protected async Task<TokenDto?> GetAdminAccessTokenAsync()
    {      
        try
        {
            const string AdminTokenCacheKey = "keycloak:admin:token";

            // Try to read token from Distributed Cache
            var cached = await _cache.GetStringAsync(AdminTokenCacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                var token = JsonSerializer.Deserialize<TokenDto>(cached, _jsonOptions);                
                if (token != null)
                {
                    _logger.LogDebug("Using cached Keycloak admin token.");
                    return token;
                }
            }

            // No valid cache -> request new token
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
            });

            var requestUrl = $"{_keycloakServerUrl}/realms/{_realm}/protocol/openid-connect/token";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = content
            };

            var response = await SendRequestAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Keycloak admin token: {Status}", response.StatusCode);
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var newToken = JsonSerializer.Deserialize<TokenDto>(jsonResponse, _jsonOptions);
            if (newToken == null)
            {
                _logger.LogWarning("Failed to deserialize Keycloak admin token.");
                return null;
            }

            // Cache the new token
            var options = new DistributedCacheEntryOptions
            {
                // Subtract a safety buffer (30 seconds before actual expiration)
                AbsoluteExpirationRelativeToNow = newToken.Expires_in.HasValue
                                   ? TimeSpan.FromSeconds(newToken.Expires_in.Value - CacheConstants.SafetyBufferSec)
                                   : TimeSpan.FromSeconds(0),                
            };

            await _cache.SetStringAsync(AdminTokenCacheKey, JsonSerializer.Serialize(newToken, _jsonOptions), options);
            _logger.LogInformation("Cached new Keycloak admin token for {seconds} seconds.", newToken.Expires_in);

            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Keycloak admin token.");
            return null;
        }
    }

    protected async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUrl, HttpContent? content = null)
    {
        var adminToken = await GetAdminAccessTokenAsync();
        var request = new HttpRequestMessage(method, requestUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken?.Access_token);
        return request;
    }    
}