using System.Diagnostics;
using Personix.Tracing;
using Shouldly;
using Xunit;

namespace Personix.Tracing.Tests;

public sealed class ActivityHelperTests
{
    private const string ActivityName = "work";
    private const string IncomingActivityName = "incoming";
    private const string OutgoingActivityName = "outgoing";
    private const string MalformedTraceparent = "not-a-traceparent";
    private const string RecordedFlags = "01";

    private const string ConfigureSourceBefore = "Configure.Before";
    private const string ConfigureSourceAfter = "Configure.After";
    private const string UnlistenedSource = "StartActivity.Unlistened";
    private const string ListenedSource = "StartActivity.Listened";
    private const string DefaultKindSource = "StartActivity.DefaultKind";
    private const string ExplicitKindSource = "StartActivity.ExplicitKind";
    private const string CurrentTraceparentSource = "Traceparent.Current";
    private const string NullTraceparentSource = "FromTraceparent.Null";
    private const string MalformedTraceparentSource = "FromTraceparent.Malformed";
    private const string ValidTraceparentSource = "FromTraceparent.Valid";
    private const string TraceparentKindSource = "FromTraceparent.Kind";

    private static string Traceparent(ActivityTraceId traceId, ActivitySpanId spanId)
    {
        return $"00-{traceId}-{spanId}-{RecordedFlags}";
    }

    [Fact]
    public void Configure_ReplacesTheActivitySource()
    {
        // Establish a known starting point, otherwise a source that already happened to carry the
        // target name would let this test pass without Configure doing anything at all.
        ActivityHelper.Configure(ConfigureSourceBefore);
        var original = ActivityHelper.Source;
        original.Name.ShouldBe(ConfigureSourceBefore);

        ActivityHelper.Configure(ConfigureSourceAfter);

        ActivityHelper.Source.Name.ShouldBe(ConfigureSourceAfter);
        ActivityHelper.Source.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void StartActivity_ReturnsNull_WhenNothingListens()
    {
        ActivityHelper.Configure(UnlistenedSource);

        using var activity = ActivityHelper.StartActivity(ActivityName);

        activity.ShouldBeNull();
    }

    [Fact]
    public void StartActivity_ReturnsActivity_WhenSourceIsListenedTo()
    {
        ActivityHelper.Configure(ListenedSource);
        using var listener = new ListenerScope(ListenedSource);

        using var activity = ActivityHelper.StartActivity(ActivityName);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(ActivityName);
    }

    [Fact]
    public void StartActivity_DefaultsToInternalKind()
    {
        ActivityHelper.Configure(DefaultKindSource);
        using var listener = new ListenerScope(DefaultKindSource);

        using var activity = ActivityHelper.StartActivity(ActivityName);

        activity!.Kind.ShouldBe(ActivityKind.Internal);
    }

    [Fact]
    public void StartActivity_HonoursExplicitKind()
    {
        ActivityHelper.Configure(ExplicitKindSource);
        using var listener = new ListenerScope(ExplicitKindSource);

        using var activity = ActivityHelper.StartActivity(IncomingActivityName, ActivityKind.Server);

        activity!.Kind.ShouldBe(ActivityKind.Server);
    }

    [Fact]
    public void GetCurrentTraceparent_ReturnsNull_WhenNoActivityIsCurrent()
    {
        Activity.Current = null;

        ActivityHelper.GetCurrentTraceparent().ShouldBeNull();
    }

    [Fact]
    public void GetCurrentTraceparent_ReturnsW3CHeaderForTheCurrentActivity()
    {
        ActivityHelper.Configure(CurrentTraceparentSource);
        using var listener = new ListenerScope(CurrentTraceparentSource);

        using var activity = ActivityHelper.StartActivity(ActivityName);

        var traceparent = ActivityHelper.GetCurrentTraceparent();

        traceparent.ShouldBe(Traceparent(activity!.TraceId, activity.SpanId));
    }

    [Fact]
    public void StartActivityFromTraceparent_StartsARootActivity_WhenTraceparentIsNull()
    {
        ActivityHelper.Configure(NullTraceparentSource);
        using var listener = new ListenerScope(NullTraceparentSource);

        using var activity = ActivityHelper.StartActivityFromTraceparent(ActivityName, traceparent: null);

        activity.ShouldNotBeNull();
        activity.ParentSpanId.ShouldBe(default(ActivitySpanId));
    }

    [Fact]
    public void StartActivityFromTraceparent_StartsARootActivity_WhenTraceparentIsMalformed()
    {
        ActivityHelper.Configure(MalformedTraceparentSource);
        using var listener = new ListenerScope(MalformedTraceparentSource);

        using var activity = ActivityHelper.StartActivityFromTraceparent(ActivityName, MalformedTraceparent);

        activity.ShouldNotBeNull();
        activity.ParentSpanId.ShouldBe(default(ActivitySpanId));
    }

    [Fact]
    public void StartActivityFromTraceparent_AdoptsTheIncomingTraceAndParent()
    {
        ActivityHelper.Configure(ValidTraceparentSource);
        using var listener = new ListenerScope(ValidTraceparentSource);

        var traceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();

        using var activity = ActivityHelper.StartActivityFromTraceparent(
            ActivityName,
            Traceparent(traceId, parentSpanId));

        activity.ShouldNotBeNull();
        activity.TraceId.ShouldBe(traceId);
        activity.ParentSpanId.ShouldBe(parentSpanId);
    }

    [Fact]
    public void StartActivityFromTraceparent_HonoursExplicitKind()
    {
        ActivityHelper.Configure(TraceparentKindSource);
        using var listener = new ListenerScope(TraceparentKindSource);

        var traceparent = Traceparent(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom());

        using var activity = ActivityHelper.StartActivityFromTraceparent(
            OutgoingActivityName,
            traceparent,
            ActivityKind.Client);

        activity!.Kind.ShouldBe(ActivityKind.Client);
    }
}
