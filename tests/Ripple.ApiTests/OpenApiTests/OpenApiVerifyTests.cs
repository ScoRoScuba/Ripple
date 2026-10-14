// Snapshot of the published API contract, using https://github.com/VerifyTests/Verify.
// A failure means the contract changed: review the diff, and if the change is intended,
// accept it by replacing the .verified.json with the .received.json.

using Ripple.Api.Endpoints;
using Ripple.Api.Versioning;
using Ripple.ApiTests.Scaffolding;

namespace Ripple.ApiTests.OpenApiTests;

public class OpenApiVerifyTests(CustomWebApplicationFactory<OpenApiScenarioStartup<IEndpoint>> factory)
    : IClassFixture<CustomWebApplicationFactory<OpenApiScenarioStartup<IEndpoint>>>
{
    private readonly HttpClient _client = factory.CreateClient();

    public static TheoryData<int> Versions => [.. ApiVersions.All.Select(v => v.MajorVersion!.Value)];

    [Theory]
    [MemberData(nameof(Versions))]
    public async Task OpenApi_Document_Matches_Approved_Contract(int version)
    {
        var document = await _client.GetStringAsync($"/swagger/v{version}/swagger.json");

        await Verify(document, "json").UseParameters(version);
    }
}
