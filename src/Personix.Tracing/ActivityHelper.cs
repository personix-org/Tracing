using System.Diagnostics;

namespace Personix.Tracing;

/// <summary>
/// Static wrapper over a single application-wide <see cref="ActivitySource"/>, so every part of the
/// application starts activities against the same source without having to pass it around.
/// </summary>
/// <remarks>
/// <see cref="Configure"/> is meant to be called once during start-up, not per request — every call
/// replaces <see cref="Source"/> with a brand-new <see cref="ActivitySource"/> instance.
/// </remarks>
public static class ActivityHelper
{
    /// <summary>
    /// Gets the <see cref="ActivitySource"/> that <see cref="StartActivity"/> and
    /// <see cref="StartActivityFromTraceparent"/> start activities on.
    /// </summary>
    /// <remarks>Named <c>"Application"</c> until <see cref="Configure"/> is called.</remarks>
    public static ActivitySource Source { get; private set; } = new("Application");

    /// <summary>Replaces <see cref="Source"/> with a new source of the given name.</summary>
    /// <param name="activitySourceName">
    /// The name to start activities under. Must match the name the OpenTelemetry pipeline listens to,
    /// or nothing will be exported.
    /// </param>
    public static void Configure(string activitySourceName) => Source = new ActivitySource(activitySourceName);

    /// <summary>Starts a new root activity on <see cref="Source"/>.</summary>
    /// <param name="name">The operation name recorded on the activity.</param>
    /// <param name="kind">The activity's relationship to its caller and callees.</param>
    /// <returns>
    /// The started activity, or <see langword="null"/> when nothing listens to <see cref="Source"/>.
    /// A <see langword="null"/> result is the normal, expected case — callers are expected to use
    /// <c>activity?.</c> rather than assume a non-null result.
    /// </returns>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(name, kind);
    }

    /// <summary>Formats <see cref="Activity.Current"/> as a W3C <c>traceparent</c> header value, for sending across a process boundary.</summary>
    /// <returns>
    /// The current activity as <c>00-{traceId}-{spanId}-{flags}</c>, where <c>flags</c> is
    /// <c>01</c> when the activity is recorded and <c>00</c> otherwise; or <see langword="null"/> when
    /// there is no current activity.
    /// </returns>
    public static string? GetCurrentTraceparent()
    {
        var activity = Activity.Current;

        return activity is null ? null : $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";
    }

    /// <summary>Starts an activity that continues the trace carried by an incoming <c>traceparent</c> header, so a request stays a single trace across process boundaries.</summary>
    /// <param name="name">The operation name recorded on the activity.</param>
    /// <param name="traceparent">
    /// The W3C <c>traceparent</c> header value from the incoming request or message, typically produced
    /// by <see cref="GetCurrentTraceparent"/> on the sending side.
    /// </param>
    /// <param name="kind">The activity's relationship to its caller and callees.</param>
    /// <returns>
    /// An activity that is the child of <paramref name="traceparent"/>'s context. When
    /// <paramref name="traceparent"/> is <see langword="null"/> or fails to parse, falls back to
    /// <see cref="StartActivity"/> and starts a new root activity instead — this method never throws
    /// on malformed input.
    /// </returns>
    public static Activity? StartActivityFromTraceparent(
        string name,
        string? traceparent,
        ActivityKind kind = ActivityKind.Internal)
    {
        if (traceparent is null || !ActivityContext.TryParse(traceparent, null, out var parentContext))
        {
            return StartActivity(name, kind);
        }

        return Source.StartActivity(name, kind, parentContext);
    }
}