using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using Polly.Retry;
using ServiceA.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceA
{
    public static class Extensions
    {
        // https://stackoverflow.com/a/3173726/697126
        public static T Random<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            // note: creating a Random instance each call may not be correct for you,
            // consider a thread-safe static instance
            var r = new Random();
            var list = enumerable as IList<T> ?? enumerable.ToList();
            return list.Count == 0 ? default(T) : list[r.Next(0, list.Count)];
        }

        // for more info: https://github.com/App-vNext/Polly#richer-policy-consumption-patterns
        // https://www.stevejgordon.co.uk/passing-an-ilogger-to-polly-policies
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
                      if (!context.TryGetLogger(out var logger)) return;

                      if (result.Exception != null)
                      {
                          logger.LogError(result.Exception, "An exception occurred on retry {RetryAttempt} for {PolicyKey}", retryCount, context.PolicyKey);
                      }
                      else
                      {
                          logger.LogError("A non success code {StatusCode} was received on retry {RetryAttempt} for {PolicyKey}",
                              (int)result.Result.StatusCode, retryCount, context.PolicyKey);
                      }
                  }).WithPolicyKey(Constants.backoffpolicy);

            PolicyRegistry registry = new PolicyRegistry()
            {
                { Constants.backoffpolicy, backOffPolicy },
                //{ "ThirdPartyApiResilienceStrategy", /* example of have more policies */ },
            };

            services.AddSingleton<IReadOnlyPolicyRegistry<string>>(registry);
        }

        // https://gist.github.com/stevejgordon/9e99f3cfa0d41a75780009e8192026e8#file-pollycontextextensions-cs
        public static bool TryGetLogger(this Context context, out ILogger logger)
        {
            if (context.TryGetValue(Constants.logger, out var loggerObject) && loggerObject is ILogger theLogger)
            {
                logger = theLogger;
                return true;
            }

            logger = null;
            return false;
        }
    }
}
