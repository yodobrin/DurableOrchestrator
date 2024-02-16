using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

    private static IServiceCollection AddLogging(this IServiceCollection services, HostBuilderContext builder, ObservabilitySettings observabilitySettings)
    {
        services.AddLogging(logBuilder =>
        {
            logBuilder.AddOpenTelemetry(otOpts =>
            {
                if (!string.IsNullOrEmpty(observabilitySettings.ApplicationInsightsConnectionString))
                {
                    otOpts.AddAzureMonitorLogExporter(amOpts => amOpts.ConnectionString = observabilitySettings.ApplicationInsightsConnectionString);
                }

                otOpts.AddConsoleExporter();
                otOpts.AddOtlpExporter();
                otOpts.IncludeFormattedMessage = true;
            });

            logBuilder.AddConsole();
            logBuilder.SetMinimumLevel(builder.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning);
        });

        return services;
    }

    private static IServiceCollection AddOpenTelemetry(this IServiceCollection services, HostBuilderContext builder, ObservabilitySettings observabilitySettings)
    {
        void EnrichActivity(Activity activity)
        {
            activity.SetTag("service.name", builder.HostingEnvironment.ApplicationName);
            activity.SetTag("service.environment", builder.HostingEnvironment.EnvironmentName);
        }

        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        if (!string.IsNullOrEmpty(observabilitySettings.ApplicationInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetry(opts =>
            {
                opts.ConnectionString = observabilitySettings.ApplicationInsightsConnectionString;
                opts.EnableAdaptiveSampling = false;
                opts.EnableQuickPulseMetricStream = false;
            });
        }

        services.AddOpenTelemetry().WithTracing(tracerBuilder =>
        {
            tracerBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(builder.HostingEnvironment.ApplicationName).AddTelemetrySdk().AddEnvironmentVariableDetector());

            AddActivitySources(tracerBuilder);
            // Add additional external sources here

            tracerBuilder.SetSampler(new AlwaysOnSampler());

            tracerBuilder.AddAspNetCoreInstrumentation(opts =>
            {
                opts.EnrichWithHttpRequest = (activity, request) =>
                {
                    EnrichActivity(activity);
                    activity.SetTag("http.method", request.Method);
                    activity.SetTag("http.url", request.Path);
                };
            });

            tracerBuilder.AddHttpClientInstrumentation(opts =>
            {
                opts.EnrichWithHttpWebRequest = (activity, request) =>
                {
                    EnrichActivity(activity);
                    activity.SetTag("http.method", request.Method);
                    activity.SetTag("http.url", request.RequestUri.ToString());
                };
            });

            // Add additional instrumentation here (e.g. SQL, Entity Framework, etc.)

            tracerBuilder.AddConsoleExporter();
            tracerBuilder.AddOtlpExporter();

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

            if (builder.HostingEnvironment.IsDevelopment())
            {
                tracerBuilder.AddZipkinExporter(opts =>
                {
                    opts.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                });
            }
        })
        .WithMetrics(metricsBuilder =>
        {
            AddMeters(metricsBuilder);
            // Add additional external meters here

            metricsBuilder.AddConsoleExporter();
            metricsBuilder.AddOtlpExporter();

            if (!string.IsNullOrEmpty(observabilitySettings.ApplicationInsightsConnectionString))
            {
                metricsBuilder.AddAzureMonitorMetricExporter(opts =>
                {
                    opts.ConnectionString = observabilitySettings.ApplicationInsightsConnectionString;
                });
            }
        });

        services.AddSingleton(new ActivitySource(builder.HostingEnvironment.ApplicationName));
        services.AddSingleton(TracerProvider.Default.GetTracer(builder.HostingEnvironment.ApplicationName));

        return services;
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
