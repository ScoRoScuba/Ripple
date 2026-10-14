using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ripple.ApiTests.Scaffolding;

public static class MockExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="Mock{T}"/> and its object as <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddMock<T>(this IServiceCollection services) where T : class
    {
        services.AddSingleton(new Mock<T>());
        services.AddSingleton(sp => sp.GetRequiredService<Mock<T>>().Object);
        return services;
    }

    /// <summary>
    /// Returns the host's mock for <typeparamref name="T"/>, cleared of setups from earlier tests.
    /// </summary>
    public static Mock<T> ResetMock<T>(this IServiceProvider services) where T : class
    {
        var mock = services.GetRequiredService<Mock<T>>();
        mock.Reset();
        return mock;
    }
}
