using Xunit;
using RouteOptimizer.Lambda;
using RouteOptimizer.Lambda.Data;

namespace RouteOptimizer.Integration.Tests;

public class RepositoryIntegrationTests
{
    [Fact]
    public void Handler_Should_Load_Request_From_File_And_Return_Response()
    {
        var repoRoot = GetRepoRoot();
        var repo = new FileDataRepository(repoRoot);
        var handler = new RoutingHandler(repo);

        var response = handler.Handle("tenant-1");

        Assert.NotNull(response);
        Assert.NotEmpty(response.Routes);
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "RouteOptimizer.sln")))
            dir = dir.Parent;

        if (dir == null)
            throw new DirectoryNotFoundException("Не знайдено корінь репозиторію (RouteOptimizer.sln).");

        return dir.FullName;
    }
}
