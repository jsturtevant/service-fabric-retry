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

        public WeatherClientTyped(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IReadOnlyCollection<WeatherForecast>> GetWeather(string endpoint)
        {
            var request = CreateRequest(endpoint);
            var result = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            using (var contentStream = await result.Content.ReadAsStreamAsync())
            {
                return await System.Text.Json.JsonSerializer.DeserializeAsync<List<WeatherForecast>>(contentStream, DefaultJsonSerializerOptions.Options); ;
            }
        }

        private static HttpRequestMessage CreateRequest(string endpoint)
        {
            return new HttpRequestMessage(HttpMethod.Get, endpoint);
        }
    }


}
