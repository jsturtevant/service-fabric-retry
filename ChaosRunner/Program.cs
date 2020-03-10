using System;
using System.Threading.Tasks;

namespace ChaosRunner
{

    class Program
    {
        public static string noretrysurl = "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/noretry?faults=false";
        public static string polyretryurl = "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/polly/retrys?faults=false";
        public static string servicepartion = "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/servicepartion/retrys?faults=false";
        public static string servicepartionwithfaults = "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/servicepartion/retrys?faults=true";

        static async Task Main(string[] args)
        {
            int Mins = 5;

            Console.WriteLine("##############################");
            Console.WriteLine("Running with no retrys enabled");
            await RunTestAsync(noretrysurl, Mins);
            await Task.Delay(TimeSpan.FromSeconds(60));

            Console.WriteLine("##############################");
            Console.WriteLine("Running with retrys enabled with polly");
            await RunTestAsync(polyretryurl, Mins);
            await Task.Delay(TimeSpan.FromSeconds(60));

            Console.WriteLine("##############################");
            Console.WriteLine("Running with retrys enabled with service partition");
            await RunTestAsync(servicepartion, Mins);
            await Task.Delay(TimeSpan.FromSeconds(60));

            Console.WriteLine("##############################");
            Console.WriteLine("Running with retrys enabled with service partition and faults enabled");
            await RunTestAsync(servicepartionwithfaults, Mins);
            await Task.Delay(TimeSpan.FromSeconds(60));
        }

        private static async Task RunTestAsync(string url, int minutes)
        {
            var runprocess = LoadRunner.RunLoad(url, minutes * 60);

            var chaosReport = await ChaosRunner.RunTest(minutes);
            foreach (var chaosEvent in chaosReport.History)
            {
                Console.WriteLine(chaosEvent);
            }

            await Task.WhenAll(runprocess);
        }
    }
}
