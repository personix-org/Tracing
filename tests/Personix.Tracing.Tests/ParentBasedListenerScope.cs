using System.Diagnostics;

namespace Personix.Tracing.Tests;

/// <summary>
/// Registers an <see cref="ActivityListener"/> whose sampling decision mirrors a real parent-based
/// sampler: it honours the <c>Recorded</c> flag carried by the incoming parent context instead of
/// always recording. <see cref="ListenerScope"/> forces every activity to be recorded, which is
/// exactly why it cannot be used to prove that the sampling decision on an incoming traceparent
/// actually reaches the listener — this scope exists so tests can tell the two flag values apart.
/// </summary>
internal sealed class ParentBasedListenerScope : IDisposable
{
    private readonly ActivityListener _listener;

    internal ParentBasedListenerScope(string activitySourceName)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == activitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.PropagationData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.PropagationData,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();
}
