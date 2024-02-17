namespace DurableOrchestrator.Observability;

/// <summary>
/// Defines an attribute that is used to mark a class as a meter for OpenTelemetry.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MeterAttribute"/> class.
/// </remarks>
/// <param name="name">The name of the meter.</param>
[AttributeUsage(AttributeTargets.Class)]
public class MeterAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the meter.
    /// </summary>
    public string Name { get; } = name;

    internal static IEnumerable<string> GetMeterNames()
    {
        var meterTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(MeterAttribute), true).Length > 0);

        foreach (var meterType in meterTypes)
        {
            if (meterType.GetCustomAttributes(typeof(MeterAttribute), true).FirstOrDefault() is MeterAttribute meterAttribute)
            {
                yield return meterAttribute.Name;
            }
        }
    }
}
