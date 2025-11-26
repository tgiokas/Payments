﻿using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Payments.Infrastructure.ApiClients;

public abstract class JccApiClient : ApiClientBase
{
    protected readonly IConfiguration? _configuration;

    protected string? _clientUuid;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected JccApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<JccApiClient> logger)
      : base(httpClient, logger)
    {
        
    }

    protected async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUrl, string token, HttpContent? content = null)
    {       
        var request = new HttpRequestMessage(method, requestUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}