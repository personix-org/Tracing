using System.Diagnostics;
using Personix.Tracing;
using Shouldly;
using Xunit;

namespace Personix.Tracing.Tests;

/// <summary>
/// <see cref="ListenerScope"/> forces every activity to <c>AllDataAndRecorded</c>, which is exactly
/// why none of the tests that use it can prove anything about the sampling decision carried by an
/// incoming traceparent — every activity comes out recorded no matter what the caller sent. These
/// tests use <see cref="ParentBasedListenerScope"/> instead, whose Sample callback actually looks at
/// the incoming flag the way a real parent-based sampler would, so a broken propagation path is
/// observable through <see cref="Activity.Recorded"/> and through <see cref="ActivityHelper.GetCurrentTraceparent"/>.
/// </summary>
public sealed class ActivityHelperSamplingPropagationTests
{
    private const string ActivityName = "work";
    private const string SampledSource = "Sampling.Sampled";
    private const string NotSampledSource = "Sampling.NotSampled";
    private const string RoundTripSource = "Sampling.RoundTrip";
    private const string UnsampledRootSource = "Sampling.UnsampledRoot";
    private const string SampledFlags = "01";
    private const string NotSampledFlags = "00";

    private static string Traceparent(ActivityTraceId traceId, ActivitySpanId spanId, string flags)
    {
        return $"00-{traceId}-{spanId}-{flags}";
    }

    [Fact]
    public void StartActivityFromTraceparent_PropagatesTheSampledFlag_ToAParentBasedSampler()
    {
        ActivityHelper.Configure(SampledSource);
        using var listener = new ParentBasedListenerScope(SampledSource);

        var traceparent = Traceparent(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), SampledFlags);

        using var activity = ActivityHelper.StartActivityFromTraceparent(ActivityName, traceparent);

        activity.ShouldNotBeNull();
        activity.Recorded.ShouldBeTrue();
    }

    [Fact]
    public void StartActivityFromTraceparent_PropagatesTheNotSampledFlag_ToAParentBasedSampler()
    {
        ActivityHelper.Configure(NotSampledSource);
        using var listener = new ParentBasedListenerScope(NotSampledSource);

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceparent = Traceparent(traceId, spanId, NotSampledFlags);

        using var activity = ActivityHelper.StartActivityFromTraceparent(ActivityName, traceparent);

        // Not recorded, but still the same trace: the sampling decision travelled without the
        // sampler having to fall back to starting a brand-new, disconnected activity.
        activity.ShouldNotBeNull();
        activity.Recorded.ShouldBeFalse();
        activity.TraceId.ShouldBe(traceId);
        activity.ParentSpanId.ShouldBe(spanId);
    }

    [Fact]
    public void StartActivityFromTraceparent_RoundTripsTheNotSampledFlag_ThroughGetCurrentTraceparent()
    {
        ActivityHelper.Configure(RoundTripSource);
        using var listener = new ParentBasedListenerScope(RoundTripSource);

        // Captured independently so the assertion below checks against the trace ID that was
        // actually sent in, not against whatever the activity happens to carry — otherwise this
        // would still pass even if the incoming context were dropped and a brand-new, disconnected
        // trace ID were generated instead.
        var traceId = ActivityTraceId.CreateRandom();
        var traceparent = Traceparent(traceId, ActivitySpanId.CreateRandom(), NotSampledFlags);

        using var activity = ActivityHelper.StartActivityFromTraceparent(ActivityName, traceparent);

        activity.ShouldNotBeNull();
        activity.TraceId.ShouldBe(traceId);
        ActivityHelper.GetCurrentTraceparent().ShouldBe(Traceparent(traceId, activity.SpanId, NotSampledFlags));
    }

    [Fact]
    public void GetCurrentTraceparent_ReturnsNotSampledFlag_WhenTheCurrentActivityWasNotSampled()
    {
        ActivityHelper.Configure(UnsampledRootSource);
        using var listener = new ParentBasedListenerScope(UnsampledRootSource);

        // No incoming parent at all: the parent-based sampler in ParentBasedListenerScope treats a
        // missing/default parent context the same as an unsampled one, so this activity is created
        // but not recorded — the counterpart to the always-recorded case that every other
        // GetCurrentTraceparent test exercises via ListenerScope.
        using var activity = ActivityHelper.StartActivity(ActivityName);

        activity.ShouldNotBeNull();
        activity.Recorded.ShouldBeFalse();
        ActivityHelper.GetCurrentTraceparent().ShouldBe(Traceparent(activity.TraceId, activity.SpanId, NotSampledFlags));
    }
}
