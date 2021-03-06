﻿using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceA.Services
{
    public interface IWeatherClient
    {
        Task<IReadOnlyCollection<WeatherForecast>> GetWeather(string endpoint);
    }

    public static class DefaultJsonSerializerOptions
    {
        public static JsonSerializerOptions Options => new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    //https://josefottosson.se/you-are-probably-still-using-httpclient-wrong-and-it-is-destabilizing-your-software/
    public class WeatherClientTyped : IWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeatherClientTyped> logger;
        private readonly IServiceResolver serviceResolver;

        public WeatherClientTyped(HttpClient httpClient, ILogger<WeatherClientTyped> logger, IServiceResolver serviceResolver)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger;
            this.serviceResolver = serviceResolver;
        }

        public async Task<IReadOnlyCollection<WeatherForecast>> GetWeather(string endpoint)
        {
            var request  = new HttpRequestMessage(HttpMethod.Get, endpoint);
            
            // pass relevant helpers
            var context = new Polly.Context().WithLogger<WeatherClientTyped>(logger);
            context.WithServiceResolver(this.serviceResolver);
            request.SetPolicyExecutionContext(context);

            var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            result.EnsureSuccessStatusCode();
            using (var contentStream = await result.Content.ReadAsStreamAsync())
            {
                return await System.Text.Json.JsonSerializer.DeserializeAsync<List<WeatherForecast>>(contentStream, DefaultJsonSerializerOptions.Options); ;
            }
        }
    }


}
