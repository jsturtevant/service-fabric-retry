using System;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Net.Http;
using System.Net.Sockets;
using System.Fabric;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ServiceA.Services
{
    public class WeatherExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ServiceFabricWeatherClientFactory> logger;

        public WeatherExceptionHandler(ILogger<ServiceFabricWeatherClientFactory> logger)
        {
            this.logger = logger;
        }

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            this.logger.LogError("retry {RetryAttempt} for {replica}: {exception}", retrySettings.RetryPolicy.TotalNumberOfRetries, exceptionInformation.TargetReplica.ToString(), exceptionInformation.Exception.Message);

            if (IsNotTransient(exceptionInformation))
            {
                this.logger.LogError(exceptionInformation.Exception, "A known non-transient exception occurred on retry {RetryAttempt} for {replica}: {exception}", retrySettings.RetryPolicy.TotalNumberOfRetries, exceptionInformation.TargetReplica.ToString(), exceptionInformation.Exception.Message);
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, TransientException.IsNotTransient, TimeSpan.FromSeconds(1), retrySettings.DefaultMaxRetryCountForNonTransientErrors);
                return true;
            }

            // if exceptionInformation.Exception is known and is transient (can be retried without re-resolving)
            if (IsTransient(exceptionInformation))
            {
                this.logger.LogError(exceptionInformation.Exception, "A known transient exception occurred on retry {RetryAttempt} for {replica}: {exception}", retrySettings.RetryPolicy.TotalNumberOfRetries, exceptionInformation.TargetReplica.ToString(), exceptionInformation.Exception.Message);
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, TransientException.IsTransient, TimeSpan.FromSeconds(1), retrySettings.DefaultMaxRetryCountForTransientErrors);
                return true;
            }
            

            // if exceptionInformation.Exception is unknown (let the next IExceptionHandler attempt to handle it)
            this.logger.LogError(exceptionInformation.Exception, "A unknown exception occurred on retry {RetryAttempt} for {replica}: {exception}", retrySettings.RetryPolicy.TotalNumberOfRetries, exceptionInformation.TargetReplica.ToString(), exceptionInformation.Exception.Message);
            result = null;
            return false;
        }

        // https://docs.microsoft.com/en-us/azure/architecture/best-practices/transient-faults
        private bool IsTransient(ExceptionInformation exceptionInformation)
        {
            if (exceptionInformation.Exception is HttpRequestException || exceptionInformation.Exception.InnerException is HttpRequestException)
            {
                this.logger.LogError(exceptionInformation.Exception, "HttpRequestException");
                return true;
            }

            if (exceptionInformation.Exception.Message.Contains("500"))
            {
                this.logger.LogError(exceptionInformation.Exception, "500");
                return true;
            }

            if (exceptionInformation.Exception is FabricTransientException || exceptionInformation.Exception.InnerException is FabricTransientException)
            {
                this.logger.LogError(exceptionInformation.Exception, "FabricTransientException");
                return true;
            }

            return false;
        }

        private bool IsNotTransient(ExceptionInformation exceptionInformation)
        {
            if (exceptionInformation.Exception is SocketException || exceptionInformation.Exception.InnerException is SocketException)
            {
                this.logger.LogError(exceptionInformation.Exception, "SocketException");
                return true;
            }
           
            if (exceptionInformation.Exception is TimeoutException || exceptionInformation.Exception.InnerException is TimeoutException)
            {
                this.logger.LogError(exceptionInformation.Exception, "TimeoutException");
                return true;
            }

            if (exceptionInformation.Exception is OperationCanceledException || exceptionInformation.Exception.InnerException is OperationCanceledException)
            {
                this.logger.LogError(exceptionInformation.Exception, "OperationCanceledException");
                return true;
            }

            if (exceptionInformation.Exception is ProtocolViolationException || exceptionInformation.Exception.InnerException is ProtocolViolationException)
            {
                this.logger.LogError(exceptionInformation.Exception, "ProtocolViolationException");
                return true;
            }

            if (exceptionInformation.Exception is HttpRequestException || exceptionInformation.Exception.InnerException is HttpRequestException)
            {
             
                if (exceptionInformation.Exception.Message.Contains("404"))
                {
                    // possible address resolved by fabric client is 0stale 
                    this.logger.LogError(exceptionInformation.Exception, "404 ");
                    return true;
                }

                return false;
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