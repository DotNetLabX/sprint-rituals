using AwesomeAssertions;
using Identity.API.Auth;
using Xunit;

namespace Identity.API.Tests.Auth;

public class ActiveUserGateScopeTests
{
    // --- Slice 1: Reflekt endpoints are excluded from the global active-user gate (Q4-A) ---

    [Fact]
    public void Should_ExcludeFromGate_When_EndpointDeclaredInReflektApiAssembly()
    {
        ActiveUserGateScope.IsExcludedModule("Reflekt.API")
            .Should().BeTrue();
    }

    // --- Slice 2: the gate still applies to Identity/Fokus endpoints (Reflekt-exact, not global) ---

    [Theory]
    [InlineData("Fokus.API")]
    [InlineData("Identity.API")]
    public void Should_NotExcludeFromGate_When_EndpointDeclaredInOtherModule(string assemblyName)
    {
        ActiveUserGateScope.IsExcludedModule(assemblyName)
            .Should().BeFalse();
    }

    // --- Slice 3: an unresolved endpoint (null) keeps the gate applied (fail safe) ---

    [Fact]
    public void Should_NotExcludeFromGate_When_EndpointAssemblyUnresolved()
    {
        ActiveUserGateScope.IsExcludedModule(null)
            .Should().BeFalse();
    }
}
