using DotnetKubernetesClient;
using Jitesoft.MariaDBOperator.Controllers;
using Jitesoft.MariaDBOperator.Finalizers.V1Alpha1;
using Jitesoft.MariaDBOperator.V1Alpha1.Entities;
using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Jitesoft.MariaDBOperator;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
#if DEBUG
            builder.AddSimpleConsole(opts =>
            {
                opts.SingleLine = true;
                opts.ColorBehavior = LoggerColorBehavior.Enabled;
                opts.IncludeScopes = true;
                opts.UseUtcTimestamp = true;
                opts.TimestampFormat = "R";
            });
#else
            builder.AddJsonConsole(opts =>
            {
                opts.UseUtcTimestamp = true;
                opts.IncludeScopes = false;
                opts.TimestampFormat = "R";
            });

#endif
        });
        services.AddSingleton<IKubernetesClient>(new KubernetesClient());
        var operatorBuilder = services.AddKubernetesOperator();

        operatorBuilder
#if DEBUG
            .AddWebhookLocaltunnel()
#endif
            .AddEntity<MariaDB>()
            .AddController<MariaDBController>()
            .AddFinalizer<MariaDBFinalizer>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
