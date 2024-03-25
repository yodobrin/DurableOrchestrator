namespace DurableOrchestrator.Core.Observability;

/// <summary>
/// Defines an attribute that is used to mark a class as a meter source for OpenTelemetry.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MeterSourceAttribute"/> class.
/// </remarks>
/// <param name="name">The name of the meter source.</param>
[AttributeUsage(AttributeTargets.Class)]
public class MeterSourceAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the meter.
    /// </summary>
    public string Name { get; } = name;

    internal static IEnumerable<string> GetMeterSourceNames()
    {
        var meterTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(MeterSourceAttribute), true).Length > 0);

        foreach (var meterType in meterTypes)
        {
            if (meterType.GetCustomAttributes(typeof(MeterSourceAttribute), true).FirstOrDefault() is
                MeterSourceAttribute meterAttribute)
            {
                yield return meterAttribute.Name;
            }
        }
    }
}
