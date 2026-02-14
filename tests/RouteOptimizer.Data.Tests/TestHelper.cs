namespace RouteOptimizer.Data.Tests;

public static class TestHelper
{
    public static string SamplesPath
    {
        get
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !Directory.Exists(Path.Combine(dir, "samples")))
                dir = Directory.GetParent(dir)?.FullName;

            return Path.Combine(
                dir ?? throw new DirectoryNotFoundException("Cannot find samples directory"),
                "samples", "raw-data");
        }
    }
}