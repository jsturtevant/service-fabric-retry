using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServiceB.Controllers
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

        private static void RunRandomFaults()
        {
            var random = new Random();
            // x% chance to be true
            if (random.NextBool(5))
            {
                // cause retry
                throw new Exception();
            }

            if (random.NextBool(5))
            {
                // cause cpu consumption
                int number = random.Next(1, 5);
                RecSpin(number);
            }

            if (random.NextBool(20))
            {
                // simulate call external
                int number = random.Next(1, 5);
                Thread.Sleep(number);
            }
        }

        // consumes cpu. borrowed from https://github.com/microsoft/perfview/blob/master/src/PerfView/SupportFiles/Tutorial.cs
        public static int aStatic = 0;
        // Spin for 'timeSec' seconds.   We do only 1 second in this
        // method, doing the rest in the helper.   
        static void RecSpin(int timeSec)
        {
            if (timeSec <= 0)
                return;
            --timeSec;
            SpinForASecond();
            RecSpinHelper(timeSec);
        }

        // RecSpinHelper is a clone of RecSpin.   It is repeated 
        // to simulate mutual recursion (more interesting example)
        static void RecSpinHelper(int timeSec)
        {
            if (timeSec <= 0)
                return;
            --timeSec;
            SpinForASecond();
            RecSpin(timeSec);
        }

        // SpingForASecond repeatedly calls DateTime.Now until for
        // 1 second.  It also does some work of its own in this
        // methods so we get some exclusive time to look at.  
        static void SpinForASecond()
        {
            DateTime start = DateTime.Now;
            for (; ; )
            {
                if ((DateTime.Now - start).TotalSeconds > 1)
                    break;

                // Do some work in this routine as well.   
                for (int i = 0; i < 10; i++)
                    aStatic += i;
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
