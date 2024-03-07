using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace DurableOrchestrator.Observability;

/// <summary>
/// Defines a set of extension methods for extending the functionality of application observability.
/// </summary>
internal static class ObservabilityExtensions
{
    /// <summary>
    /// Configures the application logging and telemetry.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The service collection to add services to.</returns>
    internal static IServiceCollection AddObservability(this IServiceCollection services, HostBuilderContext builder)
    {
        var observabilitySettings = ObservabilitySettings.FromConfiguration(builder.Configuration);
        services.AddScoped(_ => observabilitySettings);

        services.AddLogging(builder, observabilitySettings)
            .AddOpenTelemetry(builder, observabilitySettings);

        return services;
    }

    internal static TracerProviderBuilder ConfigureTracerBuilder(
        this TracerProviderBuilder tracerBuilder,
        string serviceName,
        ObservabilitySettings observabilitySettings)
    {
        tracerBuilder.SetResourceBuilder(GetResourceBuilder(serviceName));

        AddActivitySources(tracerBuilder);
        // Add additional external sources here

        tracerBuilder.SetSampler(new AlwaysOnSampler());

        tracerBuilder.AddAspNetCoreInstrumentation(opts =>
        {
            opts.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.method", request.Method);
                activity.SetTag("http.url", request.Path);
            };
        });

        tracerBuilder.AddHttpClientInstrumentation(opts =>
        {
            opts.EnrichWithHttpWebRequest = (activity, request) =>
            {
                activity.SetTag("http.method", request.Method);
                activity.SetTag("http.url", request.RequestUri.ToString());
            };
        });

        // Add additional instrumentation here (e.g. SQL, Entity Framework, etc.)

        tracerBuilder.AddConsoleExporter();

        if (!string.IsNullOrEmpty(observabilitySettings.OtlpExporterEndpoint))
        {
            tracerBuilder.AddOtlpExporter(opts => opts.Endpoint = new Uri(observabilitySettings.OtlpExporterEndpoint));
        }

        if (!string.IsNullOrEmpty(observabilitySettings.ApplicationInsightsConnectionString))
        {
            tracerBuilder.AddAzureMonitorTraceExporter(opts =>
            {
                opts.Diagnostics.IsLoggingEnabled = true;
                opts.Diagnostics.IsTelemetryEnabled = true;
                opts.Diagnostics.IsDistributedTracingEnabled = true;
                opts.Diagnostics.IsLoggingContentEnabled = true;
                opts.ConnectionString = observabilitySettings.ApplicationInsightsConnectionString;
            });
        }

        if (!string.IsNullOrEmpty(observabilitySettings.ZipkinEndpointUrl))
        {
            tracerBuilder.AddZipkinExporter(opts =>
            {
                opts.Endpoint = new Uri(observabilitySettings.ZipkinEndpointUrl);
            });
        }

        return tracerBuilder;
    }

    private static IServiceCollection AddLogging(
        this IServiceCollection services,
        HostBuilderContext builder,
        ObservabilitySettings observabilitySettings)
    {
        services.AddLogging(logBuilder =>
        {
            logBuilder.AddOpenTelemetry(otOpts =>
            {
                otOpts.SetResourceBuilder(GetResourceBuilder(builder.HostingEnvironment.ApplicationName));

                otOpts.IncludeFormattedMessage = true;

                otOpts.AddConsoleExporter();

                if (!string.IsNullOrEmpty(observabilitySettings.OtlpExporterEndpoint))
                {
                    otOpts.AddOtlpExporter(opts => opts.Endpoint = new Uri(observabilitySettings.OtlpExporterEndpoint));
                }

                if (!string.IsNullOrEmpty(observabilitySettings.ApplicationInsightsConnectionString))
                {
                    otOpts.AddAzureMonitorLogExporter(amOpts =>
                        amOpts.ConnectionString = observabilitySettings.ApplicationInsightsConnectionString);
                }
            });

            logBuilder.AddConsole();
            logBuilder.SetMinimumLevel(builder.HostingEnvironment.IsDevelopment()
                ? LogLevel.Information
                : LogLevel.Warning);
        });

        return services;
    }

    private static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services,
        HostBuilderContext builder,
        ObservabilitySettings observabilitySettings)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        services.AddOpenTelemetry()
            .WithTracing(tracerBuilder =>
            {
                tracerBuilder.ConfigureTracerBuilder(
                    builder.HostingEnvironment.ApplicationName,
                    observabilitySettings);
            })
            .WithMetrics(metricsBuilder =>
            {
                metricsBuilder.ConfigureMetricsBuilder(
                    builder.HostingEnvironment.ApplicationName,
                    observabilitySettings);
            });

        services.AddSingleton(new ActivitySource(builder.HostingEnvironment.ApplicationName));
        services.AddSingleton(TracerProvider.Default.GetTracer(builder.HostingEnvironment.ApplicationName));

        return services;
    }

    private static void ConfigureMetricsBuilder(
        this MeterProviderBuilder metricsBuilder,
        string serviceName,
        ObservabilitySettings observabilitySettings)
    {
        metricsBuilder.SetResourceBuilder(GetResourceBuilder(serviceName));

        AddMeters(metricsBuilder);
        // Add additional external meters here

        metricsBuilder.AddConsoleExporter();

        if (!string.IsNullOrEmpty(observabilitySettings.OtlpExporterEndpoint))
        {
            metricsBuilder.AddOtlpExporter(opts => opts.Endpoint = new Uri(observabilitySettings.OtlpExporterEndpoint));
        }

        if (!string.IsNullOrEmpty(observabilitySettings.ApplicationInsightsConnectionString))
        {
            metricsBuilder.AddAzureMonitorMetricExporter(opts =>
            {
                opts.ConnectionString = observabilitySettings.ApplicationInsightsConnectionString;
            });
        }
    }

    private static ResourceBuilder GetResourceBuilder(string serviceName)
    {
        return ResourceBuilder.CreateDefault()
            .AddService(serviceName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector()
            .AddAttributes(new Dictionary<string, object> { { "service.name", serviceName } });
    }

    private static TracerProviderBuilder AddActivitySources(this TracerProviderBuilder builder)
    {
        foreach (var activitySource in ActivitySourceAttribute.GetActivitySourceNames())
        {
            builder.AddSource(activitySource);
        }

        return builder;
    }

    private static MeterProviderBuilder AddMeters(this MeterProviderBuilder builder)
    {
        foreach (var meter in MeterAttribute.GetMeterNames())
        {
            builder.AddMeter(meter);
        }

        return builder;
    }
}
