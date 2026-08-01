using Microsoft.Extensions.DependencyInjection;
using Personix.Tracing;
using Shouldly;
using Xunit;

namespace Personix.Tracing.Tests;

public sealed class TracingServiceRegistrationTests
{
    private const string ChainingSource = "Registration.Chaining";
    private const string NamedSource = "Registration.Named";
    private const string DefaultSourceName = "Application";
    private const string BeforeSentinelSource = "Registration.BeforeSentinel";

    [Fact]
    public void AddApplicationTracing_ReturnsTheSameCollectionForChaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddApplicationTracing(ChainingSource);

        returned.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddApplicationTracing_ConfiguresTheActivitySourceName()
    {
        // Establish a known starting point that differs from NamedSource, otherwise a Source left
        // over from a previous test — or never touched at all — could let this test pass without
        // AddApplicationTracing doing anything.
        ActivityHelper.Configure(BeforeSentinelSource);

        new ServiceCollection().AddApplicationTracing(NamedSource);

        ActivityHelper.Source.Name.ShouldBe(NamedSource);
    }

    [Fact]
    public void AddApplicationTracing_FallsBackToApplication_WhenNoNameIsGiven()
    {
        // Same reasoning as above, and it matters even more here: ActivityHelper.Source defaults to
        // "Application" from its field initializer, which is exactly the value this test expects —
        // so without this sentinel, the test would pass even if AddApplicationTracing were a no-op
        // that never called Configure at all, as long as it happened to run before anything else
        // touched ActivityHelper.Source.
        ActivityHelper.Configure(BeforeSentinelSource);

        new ServiceCollection().AddApplicationTracing();

        ActivityHelper.Source.Name.ShouldBe(DefaultSourceName);
    }
}
