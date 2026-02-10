using Xunit;

namespace RouteOptimizer.Integration.Tests;

public class GraphQlSchemaTests
{
    [Fact]
    public void Schema_File_Should_Exist()
    {
        var path = GetRepoFilePath("lambda", "schema.graphql");
        Assert.True(File.Exists(path));
        Assert.True(new FileInfo(path).Length > 0);
    }

    private static string GetRepoFilePath(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "RouteOptimizer.sln")))
            dir = dir.Parent;

        if (dir == null)
            throw new DirectoryNotFoundException("Repo root not found.");

        return Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
    }
}
