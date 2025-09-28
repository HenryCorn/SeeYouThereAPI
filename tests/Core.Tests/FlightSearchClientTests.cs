using Core.Models;

namespace Core.Tests
{
    public class FlightSearchClientTests
    {
        [Fact]
        public async Task SearchFlightsAsync_WithValidRequest_ReturnsResults()
        {
            var client = new FakeFlightSearchClient();
            var request = new FlightSearchRequest
            {
                Origin = "LON",
                DepartureDate = DateTime.Now.AddDays(7),
                Currency = "EUR"
            };

            var results = await client.SearchFlightsAsync(request);

            Assert.NotEmpty(results);
            foreach (var result in results)
            {
                Assert.Equal("LON", result.Origin);
                Assert.NotEmpty(result.DestinationCityCode);
                Assert.NotEmpty(result.DestinationCountryCode);
                Assert.True(result.Price > 0);
                Assert.Equal("EUR", result.Currency);
            }
        }

        [Fact]
        public async Task SearchFlightsAsync_WithCountryFilter_ReturnsFilteredResults()
        {
            var client = new FakeFlightSearchClient();
            var request = new FlightSearchRequest
            {
                Origin = "LON",
                CountryFilter = "FR",
                DepartureDate = DateTime.Now.AddDays(7),
                Currency = "EUR"
            };

            var results = await client.SearchFlightsAsync(request);

            Assert.NotEmpty(results);
            foreach (var result in results)
            {
                Assert.Equal("FR", result.DestinationCountryCode);
            }
        }

        [Fact]
        public async Task GetCheapestDestinationsAsync_OrdersByPrice()
        {
            var client = new FakeFlightSearchClient();
            var request = new FlightSearchRequest
            {
                Origin = "LON",
                DepartureDate = DateTime.Now.AddDays(7),
                Currency = "EUR"
            };

            var results = await client.GetCheapestDestinationsAsync(request);
            var resultsList = results.ToList();

            Assert.NotEmpty(resultsList);
            for (int i = 1; i < resultsList.Count; i++)
            {
                Assert.True(resultsList[i - 1].Price <= resultsList[i].Price, 
                    "Results should be ordered by price (cheapest first)");
            }
        }

        [Fact]
        public async Task SearchFlightsAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            var client = new FakeFlightSearchClient();

            await Assert.ThrowsAsync<ArgumentNullException>(() => client.SearchFlightsAsync(null));
        }

        [Fact]
        public async Task SearchFlightsAsync_WithEmptyOrigin_ThrowsArgumentException()
        {
            var client = new FakeFlightSearchClient();
            var request = new FlightSearchRequest
            {
                Origin = "",
                DepartureDate = DateTime.Now.AddDays(7),
                Currency = "EUR"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => client.SearchFlightsAsync(request));
        }
    }
}
