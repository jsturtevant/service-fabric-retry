using System;
using System.Collections.Generic;

namespace ServiceA
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    public class WeatherResults
    {
        public IEnumerable<WeatherForecast> WeatherForecast1 { get; set; }
        public IEnumerable<WeatherForecast> WeatherForecast2 { get; set; }
    }
}
