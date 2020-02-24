using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using System.Text.Json;
using ServiceA.Services;

namespace ServiceA.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReverseProxyController : ControllerBase
    {
        private readonly ILogger<ReverseProxyController> _logger;
 
        public ReverseProxyController(ILogger<ReverseProxyController> logger)
        {
            _logger = logger;
        }

        [HttpGet("bad")]
        public async Task<IEnumerable<WeatherForecast>> GetbadhttpAsync()
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri(Constants.reverseProxy) })
            {
                var result = await httpClient.GetStringAsync($"{Constants.serviceB}/WeatherForecast").ConfigureAwait(false);
                var value = JsonConvert.DeserializeObject<List<WeatherForecast>>(result);
            }

            using (var httpClient = new HttpClient { BaseAddress = new Uri(Constants.reverseProxy) })
            {
                var result = await httpClient.GetStringAsync($"{Constants.serviceC}/WeatherForecast").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<List<WeatherForecast>>(result);
            }
        }

        [HttpGet("good")]
        public async Task<IEnumerable<WeatherForecast>> GetAsync([FromServices] WeatherClient weatherClient)
        {
            await weatherClient.GetWeather($"{Constants.serviceB}/WeatherForecast");
            return await weatherClient.GetWeather($"{Constants.serviceC}/WeatherForecast");
        }

   
    }

    
}
