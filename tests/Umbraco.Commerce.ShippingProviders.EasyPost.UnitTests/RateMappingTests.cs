using EasyPost.Models.API.Beta;
using FluentAssertions;

namespace Umbraco.Commerce.ShippingProviders.EasyPost.UnitTests
{
    public class RateMappingTests
    {
        private static StatelessRate CreateRate(string carrier = "USPS", string service = "Priority", string currency = "USD", string? price = "12.50")
            => new() { Carrier = carrier, Service = service, Currency = currency, Price = price! };

        [Fact]
        public void MapRates_Should_Filter_Rates_By_Currency_Ignoring_Case()
        {
            // preparation
            var rates = new[]
            {
                CreateRate(service: "Priority", currency: "usd"),
                CreateRate(service: "Express", currency: "USD"),
                CreateRate(service: "Ground", currency: "EUR"),
            };

            // execution
            var result = EasyPostShippingProvider.MapRates(rates, "USD", Guid.NewGuid(), Guid.NewGuid());

            // asserts
            result.Select(x => x.Option.Id).Should().Equal("USPS__Priority", "USPS__Express");
        }

        [Fact]
        public void MapRates_Should_Return_Empty_When_No_Rate_Matches_Currency()
        {
            // execution
            var result = EasyPostShippingProvider.MapRates([CreateRate(currency: "EUR")], "DKK", Guid.NewGuid(), Guid.NewGuid());

            // asserts
            result.Should().BeEmpty();
        }

        [Fact]
        public void MapRates_Should_Parse_Price_And_Set_Currency_And_Package()
        {
            // preparation
            var currencyId = Guid.NewGuid();
            var packageId = Guid.NewGuid();

            // execution
            var result = EasyPostShippingProvider.MapRates([CreateRate(price: "12.50")], "USD", currencyId, packageId);

            // asserts
            var rate = result.Should().ContainSingle().Subject;
            rate.Value.WithoutTax.Should().Be(12.50m);
            rate.Value.Tax.Should().Be(0m);
            rate.Value.CurrencyId.Should().Be(currencyId);
            rate.PackageId.Should().Be(packageId);
        }

        [Fact]
        public void MapRates_Should_Treat_Missing_Price_As_Zero()
        {
            // execution
            var result = EasyPostShippingProvider.MapRates([CreateRate(price: null)], "USD", Guid.NewGuid(), Guid.NewGuid());

            // asserts
            result.Should().ContainSingle().Which.Value.WithoutTax.Should().Be(0m);
        }

        [Fact]
        public void MapRates_Should_Set_Option_Id_And_Name_From_Carrier_And_Service()
        {
            // execution
            var result = EasyPostShippingProvider.MapRates([CreateRate(carrier: "UPSDAP", service: "NextDayAirEarlyAM")], "USD", Guid.NewGuid(), Guid.NewGuid());

            // asserts
            var option = result.Should().ContainSingle().Subject.Option;
            option.Id.Should().Be("UPSDAP__NextDayAirEarlyAM");
            option.Name.Should().Be("UPSDAP - Next Day Air Early AM");
        }

        [Theory]
        [InlineData("USPS", "Priority", "USPS__Priority")]
        [InlineData("USPS", "", "USPS")]
        [InlineData("", "Priority", "Priority")]
        [InlineData("", "", "")]
        public void CreateCompositeId_Should_Join_With_Double_Underscore_And_Trim_Underscores(string carrier, string service, string expected)
        {
            EasyPostShippingProvider.CreateCompositeId(carrier, service).Should().Be(expected);
        }

        [Theory]
        [InlineData("USPS", "Priority", "USPS - Priority")]
        [InlineData("USPS", "ParcelSelect", "USPS - Parcel Select")]
        [InlineData("DHLExpress", "ExpressWorldwide", "DHL Express - Express Worldwide")]
        [InlineData("USPS", "", "USPS")]
        [InlineData("", "Priority", "Priority")]
        public void CreateDescription_Should_Split_Pascal_Case_And_Trim_Separator(string carrier, string service, string expected)
        {
            EasyPostShippingProvider.CreateDescription(carrier, service).Should().Be(expected);
        }
    }
}
