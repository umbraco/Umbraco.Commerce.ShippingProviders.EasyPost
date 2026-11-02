using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EasyPost;
using EasyPost.Models.API;
using EasyPost.Models.API.Beta;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.ShippingProviders;

using EasyPostParams = EasyPost.Parameters;

namespace Umbraco.Commerce.ShippingProviders.EasyPost
{
    [ShippingProvider("easypost")]
    public class EasyPostShippingProvider(
        UmbracoCommerceContext ctx,
        ILogger<EasyPostShippingProvider> logger)
        : ShippingProviderBase<EasyPostSettings>(ctx)
    {
        private static Regex PASCAL_CASE_PATTERN = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])");

        public override bool SupportsRealtimeRates => true;

        public override async Task<ShippingRatesResult> GetShippingRatesAsync(ShippingProviderContext<EasyPostSettings> context, CancellationToken cancellationToken = default)
        {
            var package = context.Packages.FirstOrDefault();

            if (package == null || !package.HasMeasurements)
            {
                logger.Debug("Unable to calculate realtime DHL rates as the package provided is invalid");
                return ShippingRatesResult.Empty;
            }

            var clientConfig = new ClientConfiguration(
                context.Settings.TestMode
                ? context.Settings.TestApiKey
                : context.Settings.LiveApiKey);
            var client = new Client(clientConfig);

            var request = BuildStatelessRatesRequest(
                context.Order.Id,
                context.Order.CustomerInfo.FirstName,
                context.Order.CustomerInfo.LastName,
                context.MeasurementSystem,
                package,
                context.Settings.CarrierAccounts);

            var rates = await client.Beta.Rate.RetrieveStatelessRates(request, cancellationToken);
            var orderCurrency = await Context.Services.CurrencyService.GetCurrencyAsync(context.Order.CurrencyId);

            return new ShippingRatesResult
            {
                Rates = MapRates(rates, orderCurrency.Code, context.Order.CurrencyId, package.Id)
            };
        }

        internal EasyPostParams.Beta.Rate.Retrieve BuildStatelessRatesRequest(
            Guid orderId,
            string customerFirstName,
            string customerLastName,
            MeasurementSystem measurementSystem,
            Package package,
            string carrierAccounts)
        {
            var pkgLength = measurementSystem == MeasurementSystem.Metric ? CmToIn(package.Length) : package.Length;
            var pkgWidth = measurementSystem == MeasurementSystem.Metric ? CmToIn(package.Width) : package.Width;
            var pkgHeight = measurementSystem == MeasurementSystem.Metric ? CmToIn(package.Height) : package.Height;
            var pkgWeight = measurementSystem == MeasurementSystem.Metric ? KgToOz(package.Weight) : LbToOz(package.Weight);
            var customerName = $"{customerFirstName} {customerLastName}".Trim();

            var request = new EasyPostParams.Beta.Rate.Retrieve
            {
                Reference = orderId.ToString(),
                ToAddress = new EasyPostParams.Address.Create
                {
                    Name = customerName,
                    Street1 = package.ReceiverAddress.AddressLine1,
                    City = package.ReceiverAddress.City,
                    State = package.ReceiverAddress.Region,
                    Zip = package.ReceiverAddress.ZipCode,
                    Country = package.ReceiverAddress.CountryIsoCode
                },
                FromAddress = new EasyPostParams.Address.Create
                {
                    Street1 = package.ReceiverAddress.AddressLine1,
                    City = package.ReceiverAddress.City,
                    State = package.ReceiverAddress.Region,
                    Zip = package.ReceiverAddress.ZipCode,
                    Country = package.ReceiverAddress.CountryIsoCode
                },
                Parcel = new EasyPostParams.Parcel.Create
                {
                    Length = (double)pkgLength,
                    Width = (double)pkgWidth,
                    Height = (double)pkgHeight,
                    Weight = (double)pkgWeight
                }
            };

            var carrierAccountList = ParseCarrierAccounts(carrierAccounts);
            if (carrierAccountList.Length > 0)
            {
                request.CarrierAccounts.AddRange(carrierAccountList);
            }

            return request;
        }

        internal static CarrierAccount[] ParseCarrierAccounts(string carrierAccounts)
        {
            if (string.IsNullOrWhiteSpace(carrierAccounts))
            {
                return [];
            }

            return carrierAccounts.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new CarrierAccount { Id = x })
                .ToArray();
        }

        internal static List<ShippingRate> MapRates(IEnumerable<StatelessRate> rates, string orderCurrencyCode, Guid currencyId, Guid packageId)
            => rates
                .Where(x => x.Currency.Equals(orderCurrencyCode, StringComparison.OrdinalIgnoreCase))
                .Select(x => new ShippingRate(
                    new Price(decimal.Parse(x.Price ?? "0"), 0, currencyId),
                    new ShippingOption(CreateCompositeId(x.Carrier, x.Service), CreateDescription(x.Carrier, x.Service)),
                    packageId))
                .ToList();

        internal static string CreateCompositeId(string carrierCode, string productCode)
            => $"{carrierCode}__{productCode}".Trim('_');
        internal static string CreateDescription(string carrierCode, string productCode)
            => $"{PASCAL_CASE_PATTERN.Replace(carrierCode, " ")} - {PASCAL_CASE_PATTERN.Replace(productCode, " ")}".Trim(' ', '-');
    }
}
