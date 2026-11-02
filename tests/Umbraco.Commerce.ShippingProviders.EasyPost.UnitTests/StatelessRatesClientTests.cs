using System.Net;
using System.Text;
using System.Text.Json;
using EasyPost;
using FluentAssertions;
using Umbraco.Commerce.Core.Models;

namespace Umbraco.Commerce.ShippingProviders.EasyPost.UnitTests
{
    public class StatelessRatesClientTests
    {
        private readonly EasyPostShippingProvider _provider = new(null!, null!);

        [Fact]
        public async Task RetrieveStatelessRates_Should_Post_Built_Request_And_Parse_Rates_Response()
        {
            // preparation
            var orderId = Guid.NewGuid();
            var package = new Package(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Address("1 Sender St", "Sender City", "US", "11111"),
                new Address("1 Main St", null!, "Springfield", "IL", "US", "62701"),
                Guid.NewGuid(),
                [],
                10, 20, 30, 2);
            var request = _provider.BuildStatelessRatesRequest(orderId, "Jane", "Doe", MeasurementSystem.Imperial, package, null!);

            var handler = new StubHandler("""
                {
                  "rates": [
                    { "id": "rate_1", "carrier": "USPS", "service": "ParcelSelect", "currency": "USD", "rate": "7.25" },
                    { "id": "rate_2", "carrier": "UPSDAP", "service": "Ground", "currency": "EUR", "rate": "9.99" }
                  ]
                }
                """);
            using var httpClient = new HttpClient(handler);
            var client = new Client(new ClientConfiguration("EZTK_test_key") { CustomHttpClient = httpClient });

            // execution
            var rates = await client.Beta.Rate.RetrieveStatelessRates(request);

            // asserts
            handler.Request!.Method.Should().Be(HttpMethod.Post);
            handler.Request.RequestUri!.AbsolutePath.Should().EndWith("/beta/rates");

            using var body = JsonDocument.Parse(handler.Body!);
            var root = body.RootElement.GetProperty("shipment");
            root.GetProperty("reference").GetString().Should().Be(orderId.ToString());
            root.GetProperty("to_address").GetProperty("name").GetString().Should().Be("Jane Doe");
            root.GetProperty("to_address").GetProperty("zip").GetString().Should().Be("62701");
            root.GetProperty("parcel").GetProperty("weight").GetDouble().Should().Be(32);

            rates.Should().HaveCount(2);
            var mapped = EasyPostShippingProvider.MapRates(rates, "USD", Guid.NewGuid(), package.Id);
            mapped.Should().ContainSingle().Which.Option.Id.Should().Be("USPS__ParcelSelect");
        }

        [Fact]
        public async Task RetrieveStatelessRates_Should_Send_Carrier_Accounts_And_Sender_Address()
        {
            // preparation
            var package = new Package(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Address("1 Sender St", "Sender City", "US", "11111"),
                new Address("1 Main St", null!, "Springfield", "IL", "US", "62701"),
                Guid.NewGuid(),
                [],
                10, 20, 30, 2);
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Imperial, package, "ca_1,ca_2");

            using var handler = new StubHandler("""{ "rates": [] }""");
            using var httpClient = new HttpClient(handler);
            var client = new Client(new ClientConfiguration("EZTK_test_key") { CustomHttpClient = httpClient });

            // execution
            await client.Beta.Rate.RetrieveStatelessRates(request);

            // asserts
            using var body = JsonDocument.Parse(handler.Body!);
            var root = body.RootElement.GetProperty("shipment");
            root.GetProperty("from_address").GetProperty("zip").GetString().Should().Be("11111");
            root.GetProperty("carrier_accounts").EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Object ? x.GetProperty("id").GetString() : x.GetString())
                .Should().Equal("ca_1", "ca_2");
        }

        private sealed class StubHandler(string responseJson) : HttpMessageHandler
        {
            public HttpRequestMessage? Request { get; private set; }

            public string? Body { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;
                Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
