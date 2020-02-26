using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace ServiceA.Services
{
    public class ServiceFabricWeatherClient : ICommunicationClient
    {
        private readonly IHttpClientFactory clientFactory;

        public ServiceFabricWeatherClient(IHttpClientFactory client, string url)
        {
            this.clientFactory = client;
            Url = url;
        }

        public ResolvedServiceEndpoint Endpoint { get; set; }

        public string ListenerName { get; set; }

        public string Url { get; }
        
        public ResolvedServicePartition ResolvedServicePartition { get; set; }

        public async Task<IEnumerable<WeatherForecast>> CallServiceAsync(string message)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"{this.Url}/{message}");
            HttpClient client = clientFactory.CreateClient("sf");
            var result = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            result.EnsureSuccessStatusCode();
            using (var contentStream = await result.Content.ReadAsStreamAsync())
            {
                return await System.Text.Json.JsonSerializer.DeserializeAsync<List<WeatherForecast>>(contentStream, DefaultJsonSerializerOptions.Options); ;
            }
        }
    }
}