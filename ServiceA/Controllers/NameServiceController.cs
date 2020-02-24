using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Polly.Registry;
using Polly;
using ServiceA.Services;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Threading;

namespace ServiceA.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class NameServiceController : ControllerBase
    {
        private readonly ILogger<NameServiceController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IReadOnlyPolicyRegistry<string> _registry;
        private readonly ICommunicationClientFactory<ServiceFabricWeatherClient> serviceFabricClientFactory;

        public NameServiceController(ILogger<NameServiceController> logger, 
                                        IHttpClientFactory clientFactory,
                                        IReadOnlyPolicyRegistry<string> registry)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _registry = registry;
            this.serviceFabricClientFactory = new ServiceFabricWeatherClientFactory(clientFactory);
        }

        [HttpGet("bad")]
        public async Task<WeatherResults> Get()
        {
            List<WeatherForecast> w1;
            List<WeatherForecast> w2;

            string serviceB = await ResolveService(Constants.serviceB);
            using (var httpClient = new HttpClient { BaseAddress = new Uri(serviceB) })
            {
                var result = await httpClient.GetStringAsync("WeatherForecast").ConfigureAwait(false);
                w1 = JsonConvert.DeserializeObject<List<WeatherForecast>>(result);
            }

            string serviceC = await ResolveService(Constants.serviceB);
            using (var httpClient = new HttpClient { BaseAddress = new Uri(serviceC) })
            {
                var result = await httpClient.GetStringAsync("WeatherForecast").ConfigureAwait(false);
                w2 = JsonConvert.DeserializeObject<List<WeatherForecast>>(result);
            }

            return new WeatherResults { WeatherForecast1 = w1, WeatherForecast2 = w2 };
        }

        [HttpGet("good")]
        public async Task<WeatherResults> GetGood(string faults = "false")
        {
            string serviceB = await ResolveService(Constants.serviceB);
            var bResult = await CallServiceAsync($"{serviceB}/WeatherForecast?faults={faults}");
            var w1 = await ProcessServiceCallAsync(bResult);

            string serviceC = await ResolveService(Constants.serviceC);
            var cResult = await CallServiceAsync($"{serviceC}/WeatherForecast?faults={faults}");
            var w2 = await ProcessServiceCallAsync(cResult);

            return new WeatherResults{ WeatherForecast1 = w1, WeatherForecast2 = w2 };
        }

        [HttpGet("good/retrys")]
        public async Task<WeatherResults> GetRetrys(string faults = "false")
        {
            IAsyncPolicy<HttpResponseMessage> retryPolicy = this._registry.Get<IAsyncPolicy<HttpResponseMessage>>(Constants.backoffpolicy);

            var context = new Context($"GetSomeData-{Guid.NewGuid()}", new Dictionary<string, object>
            {
                { Constants.logger, _logger }
            });

            // todo perform service resolution
            string serviceB = await ResolveService(Constants.serviceB);
            var bResult = await retryPolicy.ExecuteAsync(async (ctx) => await CallServiceAsync($"{serviceB}/WeatherForecast?faults={faults}"), context);
            var w1 = await ProcessServiceCallAsync(bResult);

            string serviceC = await ResolveService(Constants.serviceC);
            var cResult = await retryPolicy.ExecuteAsync((ctx) => CallServiceAsync($"{serviceC}/WeatherForecast?faults={faults}"), context);
            var w2 = await ProcessServiceCallAsync(cResult);

            return new WeatherResults { WeatherForecast1 = w1, WeatherForecast2 = w2 };
        }

        [HttpGet("servicepartion/retrys")]
        public async Task<WeatherResults> GetWithServicePartionClient(string faults = "false")
        {

            var serviceBClient = new ServicePartitionClient<ServiceFabricWeatherClient>(this.serviceFabricClientFactory, new Uri($"fabric:/{Constants.serviceB}"));
            var w1 = await serviceBClient.InvokeWithRetryAsync(async (client) =>
                                {
                                    return await client.CallServiceAsync($"WeatherForecast?faults={faults}");
                                }, CancellationToken.None);

            var serviceCClient = new ServicePartitionClient<ServiceFabricWeatherClient>(this.serviceFabricClientFactory, new Uri($"fabric:/{Constants.serviceC}"));
            var w2 = await serviceCClient.InvokeWithRetryAsync(async (client) =>
                                {
                                    return await client.CallServiceAsync($"WeatherForecast?faults={faults}");
                                }, CancellationToken.None);

            return new WeatherResults { WeatherForecast1 = w1, WeatherForecast2 = w2 };
        }

        private async Task<HttpResponseMessage> CallServiceAsync(string message)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, message);
            HttpClient client = this._clientFactory.CreateClient();
            return await client.SendAsync(req);
        }

        private async Task<IEnumerable<WeatherForecast>> ProcessServiceCallAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                return await System.Text.Json.JsonSerializer.DeserializeAsync<List<WeatherForecast>>(contentStream, DefaultJsonSerializerOptions.Options); ;
            }
        }

        private  async Task<string> ResolveService(string serviceName)
        {
            ServicePartitionResolver resolver = ServicePartitionResolver.GetDefault();

            System.Threading.CancellationToken cancellationToken = default;
            ResolvedServicePartition partition = await resolver.ResolveAsync(new Uri($"fabric:/{serviceName}"), new ServicePartitionKey(), cancellationToken);

            var endPoint = partition.Endpoints.Random();
            dynamic address = JsonConvert.DeserializeObject(endPoint.Address);
            string urlString = address.Endpoints[""];
            return urlString;
        }
    }
}