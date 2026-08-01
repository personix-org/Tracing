using System.Diagnostics;

namespace Personix.Tracing.Tests;

/// <summary>
/// Registers an <see cref="ActivityListener"/> for a single activity source so that
/// <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> actually produces an activity.
/// Without a listener the runtime samples everything out and returns <c>null</c>.
/// </summary>
internal sealed class ListenerScope : IDisposable
{
    private readonly ActivityListener _listener;

    internal ListenerScope(string activitySourceName)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == activitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();
}
