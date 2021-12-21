// See https://aka.ms/new-console-template for more information

using System.Reflection;
using k8s;
using k8s.Operators;
using k8s.Operators.Logging;
using mariadb_operator.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;


if (!Enum.TryParse(Environment.GetEnvironmentVariable("LOG_LEVEL"), true, out LogLevel logLevel))
{
    logLevel = LogLevel.Information;
}

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddJsonConsole(opts =>
    {
        opts.UseUtcTimestamp = false;
        opts.IncludeScopes = true;
    }).SetMinimumLevel(logLevel);
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation($"MariaDB Operator version {typeof(Program).Assembly.GetCustomAttribute<AssemblyVersionAttribute>()}");

logger.LogInformation("Loading configuration");

var configuration = KubernetesClientConfiguration.IsInCluster()
    ? KubernetesClientConfiguration.InClusterConfig()
    : KubernetesClientConfiguration.BuildConfigFromConfigFile();


logger.LogInformation($"{(KubernetesClientConfiguration.IsInCluster() ? "InCluster" : "File based")} Configuration loaded successfully");

using var client = new Kubernetes(configuration);
var (address, port) = ("127.0.0.1", 9000);
using var prom = new Prometheus.MetricServer(port, address);
var op = new Operator(new OperatorConfiguration(), client, loggerFactory);

if (logLevel <= LogLevel.Debug)
{
    ServiceClientTracing.IsEnabled = true;
    ServiceClientTracing.AddTracingInterceptor(new ConsoleTracingInterceptor());
}

op.AddControllerOfType<MariaDBController>();

prom.Start();
logger.LogInformation($"Metrics server exposed on {address}:{port}");
logger.LogInformation("Starting operator...");
await op.StartAsync();
