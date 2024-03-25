namespace DurableOrchestrator.Core.Observability;

/// <summary>
/// Defines an attribute that is used to mark a class as an activity source for OpenTelemetry.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ActivitySourceAttribute"/> class.
/// </remarks>
/// <param name="name">The name of the activity source.</param>
[AttributeUsage(AttributeTargets.Class)]
public class ActivitySourceAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the activity source.
    /// </summary>
    public string Name { get; } = name;

    internal static IEnumerable<string> GetActivitySourceNames()
    {
        var activitySourceTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(ActivitySourceAttribute), true).Length > 0);

        foreach (var activitySourceType in activitySourceTypes)
        {
            if (activitySourceType.GetCustomAttributes(typeof(ActivitySourceAttribute), true).FirstOrDefault() is
                ActivitySourceAttribute activitySourceAttribute)
            {
                yield return activitySourceAttribute.Name;
            }
        }
    }
}
