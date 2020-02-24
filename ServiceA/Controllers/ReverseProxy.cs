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
using Microsoft.Extensions.Http;

namespace ServiceA.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReverseProxy : ControllerBase
    {
        private readonly ILogger<ReverseProxy> _logger;
 
        public ReverseProxy(ILogger<ReverseProxy> logger)
        {
            _logger = logger;
        }

        [HttpGet("bad")]
        public async Task<WeatherResults> GetbadhttpAsync()
        {
            List<WeatherForecast> w1;
            List<WeatherForecast> w2;

            using (var httpClient = new HttpClient { BaseAddress = new Uri(Constants.reverseProxy) })
            {
                var result = await httpClient.GetStringAsync($"{Constants.serviceB}/WeatherForecast").ConfigureAwait(false);
                w1 = JsonConvert.DeserializeObject<List<WeatherForecast>>(result);
            }

            using (var httpClient = new HttpClient { BaseAddress = new Uri(Constants.reverseProxy) })
            {
                var result = await httpClient.GetStringAsync($"{Constants.serviceC}/WeatherForecast").ConfigureAwait(false);
                w2 = JsonConvert.DeserializeObject<List<WeatherForecast>>(result);
            }

            return new WeatherResults { WeatherForecast1 = w1, WeatherForecast2 = w2 };
        }

        [HttpGet("good")]
        public async Task<WeatherResults> GetAsync([FromServices] WeatherClientTyped weatherClient)
        {
            var w1 = await weatherClient.GetWeather($"{Constants.serviceB}/WeatherForecast");
            var w2 = await weatherClient.GetWeather($"{Constants.serviceC}/WeatherForecast");

            return new WeatherResults { WeatherForecast1 = w1, WeatherForecast2 = w2 };
        }
    }
    
}
