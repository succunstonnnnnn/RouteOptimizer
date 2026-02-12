using Amazon.LocationService;
using Amazon.LocationService.Model;
using RouteOptimizer.Core.Abstractions;
using RouteOptimizer.Core.Geo;

namespace RouteOptimizer.Data.Geo;

public class GeocodingService : IGeocodingService
{
    private readonly AmazonLocationServiceClient _locationClient;
    private readonly string _indexName;

    public GeocodingService(AmazonLocationServiceClient locationClient, string indexName = "GeoIndex")
    {
        _locationClient = locationClient;
        _indexName = indexName;
    }

    public async Task<List<string>?> GetAddresses(string? address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var request = new SearchPlaceIndexForTextRequest
        {
            IndexName = _indexName,
            Text = address,
            MaxResults = 10
        };

        var response = await _locationClient.SearchPlaceIndexForTextAsync(request);

        if (response.Results.Count > 0)
        {
            return response.Results.Select(r => r.Place.Label).ToList();
        }

        return null;
    }

    public async Task<AddressInfo?> GetCoordinatesFromAddress(string address)
    {
        var request = new SearchPlaceIndexForTextRequest
        {
            IndexName = _indexName,
            Text = address,
            MaxResults = 1
        };

        var response = await _locationClient.SearchPlaceIndexForTextAsync(request);

        if (response.Results.Count > 0)
        {
            var place = response.Results[0].Place;
            return new AddressInfo
            {
                Latitude = place.Geometry.Point[1],  // AWS returns [Lon, Lat]
                Longitude = place.Geometry.Point[0],
                City = place.Municipality,
                ZipCode = place.PostalCode
            };
        }

        return null;
    }
}
