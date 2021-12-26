using System.Reflection;
using k8s;
using Kubernetes.OperatorSdk;
using Kubernetes.OperatorSdk.Logging;
using Jitesoft.MariaDBOperator.Controllers;
using Jitesoft.MariaDBOperator.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

var assembly = Assembly.GetExecutingAssembly().GetName();

if (!Enum.TryParse(Environment.GetEnvironmentVariable("LOG_LEVEL"), true, out LogLevel logLevel))
{
    logLevel = LogLevel.Debug;
}

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddJsonConsole(opts =>
    {
        opts.UseUtcTimestamp = true;
        opts.IncludeScopes = false;
        opts.TimestampFormat = "R";
    }).SetMinimumLevel(logLevel);
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("MariaDB Operator version {Version}", assembly.Version?.ToString(3) ?? "Unversioned");
logger.LogInformation("Loading configuration ({Type})", KubernetesClientConfiguration.IsInCluster() ? "InCluster" : "File based");

KubernetesClientConfiguration GetConfiguration()
{
    return KubernetesClientConfiguration.IsInCluster() ? KubernetesClientConfiguration.InClusterConfig() : KubernetesClientConfiguration.BuildDefaultConfig();
}

KubernetesClientConfiguration configuration;
try
{
    configuration = GetConfiguration();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "{Message}", ex.Message);
    Environment.Exit(1);
    return;
}

logger.LogInformation("{Type} Configuration loaded successfully",
    (KubernetesClientConfiguration.IsInCluster() ? "InCluster" : "File based"));

using var client = new k8s.Kubernetes(configuration);
var (address, port) = ("http://127.0.0.1/metrics", 9000);
//using var prom = new Prometheus.MetricServer(port, address);
var op = new Operator(new OperatorConfiguration(), client, loggerFactory);

if (logLevel <= LogLevel.Debug)
{
    ServiceClientTracing.IsEnabled = true;
    ServiceClientTracing.AddTracingInterceptor(new ConsoleTracingInterceptor());
}

op.AddControllerOfType<MariaDBController, MariaDB>();

//prom.Start();
//logger.LogInformation("Metrics server exposed on {Address}:{Port}", address, port);
logger.LogInformation("Starting operator...");
await op.StartAsync();
