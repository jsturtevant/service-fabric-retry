using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServiceC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get(string faults = "false")
        {
            bool.TryParse(faults, out bool runFaults);
            if (runFaults)
            {
                RunRandomFaults();
            }

            var random = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = random.Next(-20, 55),
                Summary = Summaries[random.Next(Summaries.Length)]
            })
            .ToArray();
        }

        private void RunRandomFaults()
        {
            var random = new Random();

            // x% chance to be true
            if (random.NextBool(5))
            {
                // simulate calling externally and cascading failure
                int number = random.Next(1, 5);
                Thread.Sleep(number);
                throw new Exception();
            }


            if (random.NextBool(20))
            {
                // simulate call external
                int number = random.Next(1, 5);
                Thread.Sleep(number);
            }
        }
    }

    public static class Extensions
    {
        //https://stackoverflow.com/a/28793411
        public static bool NextBool(this Random r, int truePercentage = 50)
        {
            return r.NextDouble() < truePercentage / 100.0;
        }
    }

}
