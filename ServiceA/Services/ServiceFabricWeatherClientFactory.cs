using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Threading;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ServiceA.Services
{
    public class ServiceFabricWeatherClientFactory : CommunicationClientFactoryBase<ServiceFabricWeatherClient>
    {
        private readonly IHttpClientFactory clientFactory;

        public ServiceFabricWeatherClientFactory(IHttpClientFactory clientFactory) : base(null, CreateHandlers())
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

        private static IEnumerable<WeatherExceptionHandler> CreateHandlers()
        {
            return new Collection<WeatherExceptionHandler>()
            {
                new WeatherExceptionHandler()
            };
        }
        
    }

    public class WeatherExceptionHandler : IExceptionHandler
    {
        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            // if exceptionInformation.Exception is known and is transient (can be retried without re-resolving)
            if (IsTransient(exceptionInformation))
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, TransientException.IsTransient, TimeSpan.FromSeconds(3), retrySettings.DefaultMaxRetryCountForTransientErrors);
                return true;
            }
            
            if (IsNotTransient(exceptionInformation))
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, TransientException.IsNotTransient, TimeSpan.FromSeconds(3), retrySettings.DefaultMaxRetryCountForNonTransientErrors);
                return true;
            }

            // if exceptionInformation.Exception is unknown (let the next IExceptionHandler attempt to handle it)
            result = null;
            return false;
        }

        // https://docs.microsoft.com/en-us/azure/architecture/best-practices/transient-faults
        private bool IsTransient(ExceptionInformation exceptionInformation)
        {
            if (exceptionInformation.Exception.Message.Contains("500"))
            {
                return true;
            }
            
            return false;
        }

        private bool IsNotTransient(ExceptionInformation exceptionInformation)
        {
            if (exceptionInformation.Exception.Message == "known")
            {
                return true;
            }

            return false;
        }
        static class TransientException
        {
            public static bool IsTransient = true;
            public static bool IsNotTransient = false;

        }
    }

    
}