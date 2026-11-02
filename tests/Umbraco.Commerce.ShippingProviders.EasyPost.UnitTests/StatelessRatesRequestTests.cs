using FluentAssertions;
using Umbraco.Commerce.Core.Models;

namespace Umbraco.Commerce.ShippingProviders.EasyPost.UnitTests
{
    public class StatelessRatesRequestTests
    {
        private readonly EasyPostShippingProvider _provider = new(null!, null!);

        private static Package CreatePackage(decimal length = 10, decimal width = 20, decimal height = 30, decimal weight = 2)
            => new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Address("1 Sender St", "Sender City", "US", "11111"),
                new Address("1 Main St", null!, "Springfield", "IL", "US", "62701"),
                Guid.NewGuid(),
                [],
                length,
                width,
                height,
                weight);

        [Fact]
        public void BuildStatelessRatesRequest_Should_Convert_Metric_Package_To_Inches_And_Ounces()
        {
            // preparation
            var package = CreatePackage(length: 10, width: 20, height: 30, weight: 2);

            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Metric, package, null!);

            // asserts
            var parcel = (global::EasyPost.Parameters.Parcel.Create)request.Parcel;
            parcel.Length.Should().BeApproximately(10d / 2.54d, 0.01);
            parcel.Width.Should().BeApproximately(20d / 2.54d, 0.01);
            parcel.Height.Should().BeApproximately(30d / 2.54d, 0.01);
            parcel.Weight.Should().BeApproximately(2d * 35.274d, 0.1);
        }

        [Fact]
        public void BuildStatelessRatesRequest_Should_Convert_Imperial_Weight_From_Pounds_To_Ounces_And_Keep_Dimensions()
        {
            // preparation
            var package = CreatePackage(length: 10, width: 20, height: 30, weight: 2);

            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Imperial, package, null!);

            // asserts
            var parcel = (global::EasyPost.Parameters.Parcel.Create)request.Parcel;
            parcel.Length.Should().Be(10);
            parcel.Width.Should().Be(20);
            parcel.Height.Should().Be(30);
            parcel.Weight.Should().Be(32);
        }

        [Fact]
        public void BuildStatelessRatesRequest_Should_Use_Order_Id_As_Reference()
        {
            // preparation
            var orderId = Guid.NewGuid();

            // execution
            var request = _provider.BuildStatelessRatesRequest(orderId, "Jane", "Doe", MeasurementSystem.Metric, CreatePackage(), null!);

            // asserts
            request.Reference.Should().Be(orderId.ToString());
        }

        [Fact]
        public void BuildStatelessRatesRequest_Should_Map_Receiver_Address_To_ToAddress()
        {
            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Metric, CreatePackage(), null!);

            // asserts
            var to = (global::EasyPost.Parameters.Address.Create)request.ToAddress;
            to.Name.Should().Be("Jane Doe");
            to.Street1.Should().Be("1 Main St");
            to.City.Should().Be("Springfield");
            to.State.Should().Be("IL");
            to.Zip.Should().Be("62701");
            to.Country.Should().Be("US");
        }

        [Fact]
        public void BuildStatelessRatesRequest_Should_Map_Sender_Address_To_FromAddress()
        {
            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Metric, CreatePackage(), null!);

            // asserts
            var from = (global::EasyPost.Parameters.Address.Create)request.FromAddress;
            from.Street1.Should().Be("1 Sender St");
            from.City.Should().Be("Sender City");
            from.Zip.Should().Be("11111");
            from.Country.Should().Be("US");
        }

        [Fact]
        public void BuildStatelessRatesRequest_Should_Set_Carrier_Accounts_From_Setting()
        {
            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Metric, CreatePackage(), "ca_1, ca_2");

            // asserts
            request.CarrierAccounts.Should().NotBeNull();
            request.CarrierAccounts!.Select(x => x.Id).Should().Equal("ca_1", "ca_2");
        }

        [Fact]
        public void BuildStatelessRatesRequest_Should_Leave_Carrier_Accounts_Unset_When_Setting_Is_Blank()
        {
            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), "Jane", "Doe", MeasurementSystem.Metric, CreatePackage(), " ");

            // asserts
            request.CarrierAccounts.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData("Jane", "Doe", "Jane Doe")]
        [InlineData("Jane", "", "Jane")]
        [InlineData("", "Doe", "Doe")]
        [InlineData("", "", "")]
        public void BuildStatelessRatesRequest_Should_Trim_Customer_Name(string firstName, string lastName, string expected)
        {
            // execution
            var request = _provider.BuildStatelessRatesRequest(Guid.NewGuid(), firstName, lastName, MeasurementSystem.Metric, CreatePackage(), null!);

            // asserts
            ((global::EasyPost.Parameters.Address.Create)request.ToAddress).Name.Should().Be(expected);
        }

        [Fact]
        public void ParseCarrierAccounts_Should_Trim_Entries_And_Skip_Empty_Ones()
        {
            // execution
            var accounts = EasyPostShippingProvider.ParseCarrierAccounts(" ca_1 , ,ca_2,, ca_3");

            // asserts
            accounts.Select(x => x.Id).Should().Equal("ca_1", "ca_2", "ca_3");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(" , ,")]
        public void ParseCarrierAccounts_Should_Return_Nothing_When_Setting_Is_Blank(string? setting)
        {
            EasyPostShippingProvider.ParseCarrierAccounts(setting!).Should().BeEmpty();
        }
    }
}
