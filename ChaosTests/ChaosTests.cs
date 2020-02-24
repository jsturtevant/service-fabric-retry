using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using NUnit.Framework;
using System;
using System.Fabric.Chaos.DataStructures;
using System.Linq;
using System.Threading.Tasks;

namespace ChaosTests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void ChaosWithFaultsRetry()
        {
            var assertions = new[] {
               Assertion.ForStep("simple step", stats => stats.FailCount < 100, "FailCount > 2"),
               Assertion.ForStep("simple step", stats => stats.RPS > 8, "RPS > 8"),
               Assertion.ForStep("simple step", stats => stats.Percent75 >= 102, "Percent75 >= 1000"),
            };

            int timeToRun = 5;
            var step = HttpStep.Create("simple step", context =>
                Http.CreateRequest("GET", "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/good/retrys")
                    .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
            );

            var scenario = ScenarioBuilder.CreateScenario("ChaosWithFaultsRetry", new[] { step })         
                            .WithConcurrentCopies(20)
                            .WithWarmUpDuration(TimeSpan.FromSeconds(30))
                            .WithDuration(TimeSpan.FromMinutes(timeToRun))
                            .WithAssertions(assertions);

            var finishedScenario = Task.Run(() => NBomberRunner.RegisterScenarios(scenario)
                                                               .WithReportFileName($"{nameof(ChaosWithFaultsRetry)}{DateTime.Now.ToString("yyyyMMddHHmmss")}")
                                                               .RunTest());
            var chaosReport = ChaosRunner.RunTest(timeToRun);

            Task.WaitAll(finishedScenario);
        }

        [Test]
        public void ChaosWithNoFaultsNoRetry()
        {
            var assertions = new[] {
               Assertion.ForStep("simple step", stats => stats.FailCount < 10, "FailCount > 2"),
               Assertion.ForStep("simple step", stats => stats.RPS > 100, "RPS > 8"),
            };

            int timeToRun = 5;
            var step = HttpStep.Create("simple step", context =>
                Http.CreateRequest("GET", "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/good")
                    .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
            );

            var scenario = ScenarioBuilder.CreateScenario($"{nameof(ChaosWithNoFaultsNoRetry)}", new[] { step })
                            .WithConcurrentCopies(20)
                            .WithWarmUpDuration(TimeSpan.FromSeconds(30))
                            .WithDuration(TimeSpan.FromMinutes(timeToRun))
                            .WithAssertions(assertions);

            var finishedScenario = Task.Run(() => NBomberRunner.RegisterScenarios(scenario)
                                                            .WithReportFileName($"{nameof(ChaosWithNoFaultsNoRetry)}{DateTime.Now.ToString("yyyyMMddHHmmss")}")
                                                            .RunTest());
            var chaosReport = ChaosRunner.RunTest(timeToRun);

            foreach (var chaosEvent in chaosReport.History)
            {
                 Console.WriteLine(chaosEvent);
            }

            Task.WaitAll(finishedScenario);
        }

        [Test]
        public void ChaosWithNoFaultsRetry()
        {
            var assertions = new[] {
               Assertion.ForStep("simple step", stats => stats.FailCount < 2, "FailCount < 2"),
               Assertion.ForStep("simple step", stats => stats.RPS > 8, "RPS > 8"),
            };

            int timeToRun = 5;
            var step = HttpStep.Create("simple step", context =>
                Http.CreateRequest("GET", "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/good/retrys")
                    .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
            );

            var scenario = ScenarioBuilder.CreateScenario("ChaosWithFaultsRetry", new[] { step })
                            .WithConcurrentCopies(20)
                            .WithWarmUpDuration(TimeSpan.FromSeconds(30))
                            .WithDuration(TimeSpan.FromMinutes(timeToRun))
                            .WithAssertions(assertions);

            var finishedScenario = Task.Run(() => NBomberRunner.RegisterScenarios(scenario)
                                                        .WithReportFileName($"{nameof(ChaosWithNoFaultsRetry)}{DateTime.Now.ToString("yyyyMMddHHmmss")}")
                                                        .RunTest());
            var chaosReport = ChaosRunner.RunTest(timeToRun);

            foreach (var chaosEvent in chaosReport.History)
            {
                Console.WriteLine(chaosEvent);
            }

            Task.WaitAll(finishedScenario);
        }

        [Test]
        public void NoChaosWithFaultsNoRetry()
        {
            var assertions = new[] {
               Assertion.ForStep("simple step", stats => stats.FailCount < 100, "FailCount > 2"),
               Assertion.ForStep("simple step", stats => stats.RPS > 8, "RPS > 8"),
               Assertion.ForStep("simple step", stats => stats.Percent75 >= 102, "Percent75 >= 1000"),
            };

            int timeToRun = 1;
            var step = HttpStep.Create("simple step", context =>
                Http.CreateRequest("GET", "http://sf-memoryleak.eastus.cloudapp.azure.com:8080/nameservice/good")
                    .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
            );

            var scenario = ScenarioBuilder.CreateScenario("NoChaosWithFaultsNoRetry", new[] { step })
                            .WithConcurrentCopies(20)
                            .WithWarmUpDuration(TimeSpan.FromSeconds(30))
                            .WithDuration(TimeSpan.FromMinutes(timeToRun))
                            .WithAssertions(assertions);

            NBomberRunner.RegisterScenarios(scenario).WithReportFileName($"{nameof(NoChaosWithFaultsNoRetry)}{DateTime.Now.ToString("yyyyMMddHHmmss")}").RunTest();
        }
    }
}