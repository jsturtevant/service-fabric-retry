using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Threading;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ServiceA.Services
{
    public class ServiceFabricWeatherClientFactory : CommunicationClientFactoryBase<ServiceFabricWeatherClient>
    {
        private readonly IHttpClientFactory clientFactory;

        public ServiceFabricWeatherClientFactory(IHttpClientFactory clientFactory, IEnumerable<WeatherExceptionHandler>  exceptionHandlers) : base(null, exceptionHandlers)
        {
            this.clientFactory = clientFactory;
        }

        protected override void AbortClient(ServiceFabricWeatherClient client)
        {
        }

        protected override Task<ServiceFabricWeatherClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ServiceFabricWeatherClient(clientFactory, endpoint));
        }

        protected override bool ValidateClient(ServiceFabricWeatherClient clientChannel)
        {
            return true;
        }

        protected override bool ValidateClient(string endpoint, ServiceFabricWeatherClient client)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<WeatherExceptionHandler> CreateHandlers(ILogger<ServiceFabricWeatherClientFactory> logger)
        {
            return new Collection<WeatherExceptionHandler>()
            {
                new WeatherExceptionHandler(logger)
            };
        }
        
    }

    
}