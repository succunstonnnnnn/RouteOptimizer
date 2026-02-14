using RouteOptimizer.Core.Geo;

namespace RouteOptimizer.Core.Abstractions;

public interface IGeocodingService
{
    Task<List<string>?> GetAddresses(string? address);
    Task<AddressInfo?> GetCoordinatesFromAddress(string address);
}
