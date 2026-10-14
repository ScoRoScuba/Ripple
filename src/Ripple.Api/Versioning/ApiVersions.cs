using Asp.Versioning;

namespace Ripple.Api.Versioning;

/// <summary>
/// Every API version the service supports. Add a new version here, then map the endpoints that implement it.
/// </summary>
public static class ApiVersions
{
    public static readonly ApiVersion V1 = new(1);

    public static readonly IReadOnlyList<ApiVersion> All = [V1];
}
