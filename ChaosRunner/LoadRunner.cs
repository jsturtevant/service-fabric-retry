using CliWrap;
using CliWrap.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ChaosRunner
{
    public class LoadRunner
    {
        public static async Task<ExecutionResult> RunLoad(string url, int durationSeconds, int rate = 100, int workers = 1)
        {
            /*
             *  -c  Number of workers to run concurrently.
             *  -q  Rate limit, in queries per second (QPS) per worker. Default is no rate limit.
             */

            if (rate < workers)
            {
                throw new NotSupportedException("Total number of requests cannot be smaller than the concurrency level.");
            }

            var paramaters = $"-t 20 -z {durationSeconds}s -q {rate} -c {workers} {url}";
            Console.WriteLine($"paramaters: {paramaters}");

            return await Cli.Wrap("loadtest/hey.exe")
                    .SetArguments(paramaters)
                    .SetStandardOutputCallback(l => Console.WriteLine($"{l}")) 
                    .SetStandardErrorCallback(l => Console.WriteLine($"{l}"))
                    .ExecuteAsync();
        }


      
    }


}
