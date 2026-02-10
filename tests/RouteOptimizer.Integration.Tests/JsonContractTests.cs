using System.Text.Json;
using Xunit;
using RouteOptimizer.Lambda.Models;
using RouteOptimizer.Lambda.Validation;

namespace RouteOptimizer.Integration.Tests;

public class JsonContractTests
{
    [Fact]
    public void Can_Read_And_Parse_RoutingRequest_From_Json()
    {
        // 1) знайти шлях до файлу
        var path = GetRepoFilePath("samples", "routing", "routing-request.json");

        // 2) прочитати JSON
        var json = File.ReadAllText(path);

        // 3) розпарсити у RoutingRequest
        var request = JsonSerializer.Deserialize<RoutingRequest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // 4) перевірки
        Assert.NotNull(request);
        Assert.True(request.Technicians > 0);
        Assert.True(request.Visits > 0);
    }

    [Fact]
    public void Can_Read_And_Parse_RoutingResponse_From_Json()
    {
        var path = GetRepoFilePath("samples", "routing", "routing-response.json");

        var json = File.ReadAllText(path);

        var response = JsonSerializer.Deserialize<RoutingResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(response);
        Assert.NotNull(response.Routes);
        Assert.NotEmpty(response.Routes);
    }
    [Fact]
    public void Should_Fail_When_Route_Has_No_Visits()
    {
        var response = new RoutingResponse
        {
            Routes =
            [
                new Route { TechnicianId = 1, VisitIds = [] }
            ]
        };

        var validator = new RouteValidator();

        Assert.False(validator.NoEmptyRoutes(response));
    }

    [Fact]
    public void Should_Pass_When_Valid_Response()
    {
        var response = new RoutingResponse
        {
            Routes =
            [
                new Route { TechnicianId = 1, VisitIds = [101, 102] }
            ]
        };

        var validator = new RouteValidator();

        Assert.True(validator.HasRoutes(response));
        Assert.True(validator.NoEmptyRoutes(response));
        Assert.True(validator.NoDuplicateVisits(response));
    }

    private static string GetRepoFilePath(params string[] parts)
    {
        // BaseDirectory = папка, звідки запускаються тести (bin/Debug/...)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        // Піднімаємось вгору, поки не знайдемо файл RouteOptimizer.sln (ознака кореня репо)
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "RouteOptimizer.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
            throw new DirectoryNotFoundException("Не знайдено корінь репозиторію (RouteOptimizer.sln).");

        // Склеюємо шлях до потрібного файлу
        return Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
    }
}
