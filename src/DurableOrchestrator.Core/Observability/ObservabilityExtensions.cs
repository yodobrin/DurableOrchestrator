using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.Core.Observability;

/// <summary>
/// Defines a set of extension methods for extending the functionality of application observability.
/// </summary>
public static class ObservabilityExtensions
{
    private static readonly TextMapPropagator s_propagator = new CompositeTextMapPropagator(
        new List<TextMapPropagator> { new TraceContextPropagator(), new BaggagePropagator() });

    /// <summary>
    /// Injects the details of the specified span into the <paramref name="observabilityContext" />.
    /// </summary>
    /// <param name="observabilityContext">The observability context to inject the span details into.</param>
    /// <param name="spanContext">The span context to inject into the observability context.</param>
    public static void InjectObservabilityContext(
        this IObservabilityContext observabilityContext,
        SpanContext spanContext)
    {
        s_propagator.Inject(
            new PropagationContext(spanContext, Baggage.Current),
            observabilityContext.ObservabilityProperties,
            (props, key, value) =>
            {
                props ??= new Dictionary<string, object>();
                props.TryAdd(key, value);
            });
    }

    /// <summary>
    /// Extracts the details of a span from the <paramref name="observabilityContext" />.
    /// </summary>
    /// <param name="observabilityContext">The observability context to extract the span details from.</param>
    /// <returns>The details of the span extracted from the observability context.</returns>
    public static SpanContext ExtractObservabilityContext(this IObservabilityContext observabilityContext)
    {
        var propagationContext = s_propagator.Extract(
            default,
            observabilityContext.ObservabilityProperties,
            (props, key) =>
            {
                if (!props.TryGetValue(key, out var value) || value.ToString() is null)
                {
                    return [];
                }

                return [value.ToString()];
            });

        return new SpanContext(propagationContext.ActivityContext);
    }

    /// <summary>
    /// Configures the application logging and telemetry.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration to use for observability settings.</param>
    /// <param name="applicationName">The name of the application to use for observability.</param>
    /// <param name="isDevelopment">A value indicating whether the application is running in a development environment.</param>
    /// <returns>The service collection to add services to.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        bool isDevelopment)
    {
        var observabilitySettings = ObservabilitySettings.FromConfiguration(configuration);
        services.AddScoped(_ => observabilitySettings);

        services.AddLogging(applicationName, isDevelopment, observabilitySettings)
            .AddOpenTelemetry(applicationName, observabilitySettings);

        return services;
    }

    private static TracerProviderBuilder ConfigureTracerBuilder(
        this TracerProviderBuilder tracerBuilder,
        string serviceName,
        ObservabilitySettings observabilitySettings)
    {
        tracerBuilder.SetResourceBuilder(GetResourceBuilder(serviceName));

        AddActivitySources(tracerBuilder);
        // Add additional external sources here

        tracerBuilder.SetSampler(new AlwaysOnSampler());

        tracerBuilder.AddHttpClientInstrumentation(opts =>
        {
            opts.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.method", request.Method);
                activity.SetTag("http.url", request.RequestUri?.ToString());
            };

            opts.EnrichWithHttpResponseMessage = (activity, response) =>
            {
                activity.SetTag("http.method", response.RequestMessage?.Method.ToString());
                activity.SetTag("http.url", response.RequestMessage?.RequestUri?.ToString());
                activity.SetTag("http.status_code", response.StatusCode.ToString());
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
        string applicationName,
        bool isDevelopment,
        ObservabilitySettings observabilitySettings)
    {
        services.AddLogging(logBuilder =>
        {
            logBuilder.AddOpenTelemetry(otOpts =>
            {
                otOpts.SetResourceBuilder(GetResourceBuilder(applicationName));

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
            logBuilder.SetMinimumLevel(isDevelopment
                ? LogLevel.Information
                : LogLevel.Warning);
        });

        return services;
    }

    private static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services,
        string applicationName,
        ObservabilitySettings observabilitySettings)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        services.AddOpenTelemetry()
            .WithTracing(tracerBuilder =>
            {
                tracerBuilder.ConfigureTracerBuilder(
                    applicationName,
                    observabilitySettings);
            })
            .WithMetrics(metricsBuilder =>
            {
                metricsBuilder.ConfigureMetricsBuilder(
                    applicationName,
                    observabilitySettings);
            });

        services.AddSingleton(new ActivitySource(applicationName));
        services.AddSingleton(TracerProvider.Default.GetTracer(applicationName));

        return services;
    }

    private static MeterProviderBuilder ConfigureMetricsBuilder(
        this MeterProviderBuilder metricsBuilder,
        string serviceName,
        ObservabilitySettings observabilitySettings)
    {
        metricsBuilder.SetResourceBuilder(GetResourceBuilder(serviceName));

        AddMeterSources(metricsBuilder);
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

        return metricsBuilder;
    }

    private static ResourceBuilder GetResourceBuilder(string serviceName)
    {
        return ResourceBuilder.CreateDefault()
            .AddService(serviceName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();
    }

    private static TracerProviderBuilder AddActivitySources(this TracerProviderBuilder builder)
    {
        foreach (var activitySource in ActivitySourceAttribute.GetActivitySourceNames())
        {
            builder.AddSource(activitySource);
        }

        return builder;
    }

    private static MeterProviderBuilder AddMeterSources(this MeterProviderBuilder builder)
    {
        foreach (var meterSource in MeterSourceAttribute.GetMeterSourceNames())
        {
            builder.AddMeter(meterSource);
        }

        return builder;
    }
}
