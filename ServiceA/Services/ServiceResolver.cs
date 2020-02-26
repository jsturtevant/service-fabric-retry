using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceA.Services
{
    public interface IServiceResolver
    {
        public Task<string> ResolveService(string serviceName);
    }

    public class ServiceResolver : IServiceResolver
    {
        public async Task<string> ResolveService(string serviceName)
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
