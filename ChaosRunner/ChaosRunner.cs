using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Chaos.DataStructures;
using System.Fabric.Health;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ChaosRunner
{
    public class ChaosRunner
    {
        public static async Task<ChaosReport> RunTest(int minsTorun)
        {
            string clientCertThumb = "87b906f84a251c015d44ea188e2eff322d1c16f8";
            string serverCertThumb = "87b906f84a251c015d44ea188e2eff322d1c16f8";
            string CommonName = "memoryleak";
            string connection = "sf-memoryleak.eastus.cloudapp.azure.com:19000";

            var xc = GetCredentials(clientCertThumb, serverCertThumb, CommonName);

            using (var client = new FabricClient(xc, connection))
            {
                var startTimeUtc = DateTime.UtcNow;
                var maxClusterStabilizationTimeout = TimeSpan.FromSeconds(30.0);
                var timeToRun = TimeSpan.FromMinutes(minsTorun);

                // The recommendation is to start with a value of 2 or 3 and to exercise caution while moving up.
                var maxConcurrentFaults = 3;

                var startContext = new Dictionary<string, string> { { "ReasonForStart", "Testing" } };

                // Time-separation (in seconds) between two consecutive iterations of Chaos. The larger the value, the
                // lower the fault injection rate.
                var waitTimeBetweenIterations = TimeSpan.FromSeconds(1);

                // Wait time (in seconds) between consecutive faults within a single iteration.
                // The larger the value, the lower the overlapping between faults and the simpler the sequence of
                // state transitions that the cluster goes through. 
                var waitTimeBetweenFaults = TimeSpan.FromSeconds(1);

                // Passed-in cluster health policy is used to validate health of the cluster in between Chaos iterations. 
                var clusterHealthPolicy = new ClusterHealthPolicy
                {
                    ConsiderWarningAsError = false,
                    MaxPercentUnhealthyApplications = 100,
                    MaxPercentUnhealthyNodes = 100
                };

                var nodetypeInclusionList = new List<string> { "nt2vm", "nt3vm" };
                var applicationInclusionList = new List<string> { "fabric:/RequestHandling" };

                // List of cluster entities to target for Chaos faults.
                var chaosTargetFilter = new ChaosTargetFilter
                {
                    NodeTypeInclusionList = nodetypeInclusionList,
                    //ApplicationInclusionList = applicationInclusionList,
                };

                var parameters = new ChaosParameters(
                    maxClusterStabilizationTimeout,
                    maxConcurrentFaults,
                    true, /* EnableMoveReplicaFault */
                    timeToRun,
                    startContext,
                    waitTimeBetweenIterations,
                    waitTimeBetweenFaults,
                    clusterHealthPolicy)
                { ChaosTargetFilter = chaosTargetFilter };

                try
                {
                    await client.TestManager.StartChaosAsync(parameters);
                }
                catch (FabricChaosAlreadyRunningException)
                {
                    Console.WriteLine("An instance of Chaos is already running in the cluster.");
                    await client.TestManager.StopChaosAsync();
                    throw new Exception("Chaos test already running");
                }

                var filter = new ChaosReportFilter(startTimeUtc, DateTime.MaxValue);

                var eventSet = new HashSet<ChaosEvent>(new ChaosEventComparer());

                string continuationToken = null;

                while (true)
                {
                    ChaosReport report;
                    try
                    {
                        report = string.IsNullOrEmpty(continuationToken)
                            ? await client.TestManager.GetChaosReportAsync(filter)
                            : await client.TestManager.GetChaosReportAsync(continuationToken);
                    }
                    catch (Exception e)
                    {
                        if (e is FabricTransientException)
                        {
                            Console.WriteLine("A transient exception happened: '{0}'", e);
                        }
                        else if (e is TimeoutException)
                        {
                            Console.WriteLine("A timeout exception happened: '{0}'", e);
                        }
                        else
                        {
                            throw;
                        }

                        Task.Delay(TimeSpan.FromSeconds(1.0)).GetAwaiter().GetResult();
                        continue;
                    }

                    continuationToken = report.ContinuationToken;

                    foreach (var chaosEvent in report.History)
                    {
                        eventSet.Add(chaosEvent);
                    }

                    // When Chaos stops, a StoppedEvent is created.
                    // If a StoppedEvent is found, exit the loop.
                    var lastEvent = report.History.LastOrDefault();

                    if (lastEvent is StoppedEvent)
                    {
                        return report;
                    }

                    Task.Delay(TimeSpan.FromSeconds(1.0)).GetAwaiter().GetResult();
                }
            }
        }

        private static X509Credentials GetCredentials(string clientCertThumb, string serverCertThumb, string name)
        {
            X509Credentials xc = new X509Credentials();
            xc.StoreLocation = StoreLocation.CurrentUser;
            xc.StoreName = "My";
            xc.FindType = X509FindType.FindByThumbprint;
            xc.FindValue = clientCertThumb;
            xc.RemoteCommonNames.Add(name);
            xc.RemoteCertThumbprints.Add(serverCertThumb);
            xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;
            return xc;
        }
    }
    

    public class ChaosEventComparer : IEqualityComparer<ChaosEvent>
    {
        public bool Equals(ChaosEvent x, ChaosEvent y)
        {
            return x.TimeStampUtc.Equals(y.TimeStampUtc);
        }
        public int GetHashCode(ChaosEvent obj)
        {
            return obj.TimeStampUtc.GetHashCode();
        }
    }
}
