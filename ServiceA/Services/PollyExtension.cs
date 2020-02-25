using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceA.Services
{
    public static class PollyExtension
    {
        //https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory#using-any-policy-configured-via-the-traditional-polly-syntax
        public static void AddPollyPolicies(this IServiceCollection services)
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying = {
               HttpStatusCode.RequestTimeout, // 408
               HttpStatusCode.InternalServerError, // 500
               HttpStatusCode.BadGateway, // 502
               HttpStatusCode.ServiceUnavailable, // 503
               HttpStatusCode.GatewayTimeout // 504
            };

            var backOffPolicy = Policy
              .Handle<HttpRequestException>()
              .Or<OperationCanceledException>()
              .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
              .WaitAndRetryAsync(new[]
                  {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
                  }, (result, timeSpan, retryCount, context) =>
                  {
                      if (result.Exception != null)
                      {
                          context.GetLogger()?.LogError(result.Exception, "An exception occurred on retry {RetryAttempt} for {PolicyKey}", retryCount, context.PolicyKey);
                      }
                      else
                      {
                          context.GetLogger()?.LogError("A non success code {StatusCode} was received on retry {RetryAttempt} for {PolicyKey}",
                              (int)result.Result.StatusCode, retryCount, context.PolicyKey);
                      }
                  }).WithPolicyKey(Constants.backoffpolicy);

            PolicyRegistry registry = new PolicyRegistry()
            {
                { Constants.backoffpolicy, backOffPolicy },
                { Constants.noretrypolicy, Policy.NoOpAsync() }
                //{ "ThirdPartyApiResilienceStrategy", /* example of have more policies */ },
            };

            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);
        }

        //https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory#configuring-httpclientfactory-policies-to-use-an-iloggert-from-the-call-site
        public static Context WithLogger<T>(this Context context, ILogger logger)
        {
            context[Constants.logger] = logger;
            return context;
        }

        public static ILogger GetLogger(this Context context)
        {
            if (context.TryGetValue(Constants.logger, out object logger))
            {
                return logger as ILogger;
            }

            return null;
        }
    }
}
