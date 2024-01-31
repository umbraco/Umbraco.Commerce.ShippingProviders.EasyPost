using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EasyPost;
using EasyPost.Models.API;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.ShippingProviders;

using EasyPostParams = EasyPost.Parameters;

namespace Umbraco.Commerce.ShippingProviders.EasyPost
{
    [ShippingProvider("easypost", "EasyPost", "EasyPost shipping provider")]
    public class EasyPostShippingProvider : ShippingProviderBase<EasyPostSettings>
    {
        private static Regex PASCAL_CASE_PATTERN = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])");

        private readonly ILogger<EasyPostShippingProvider> _logger;

        public EasyPostShippingProvider(
            UmbracoCommerceContext ctx,
            ILogger<EasyPostShippingProvider> logger)
            : base(ctx)
        {
            _logger = logger;
        }

        public override bool SupportsRealtimeRates => true;

        public override async Task<ShippingRatesResult> GetShippingRatesAsync(ShippingProviderContext<EasyPostSettings> context, CancellationToken cancellationToken = default)
        {
            var package = context.Packages.FirstOrDefault();

            if (package == null || !package.HasMeasurements)
            {
                _logger.Debug("Unable to calculate realtime DHL rates as the package provided is invalid");
                return ShippingRatesResult.Empty;
            }

            var clientConfig = new ClientConfiguration(
                context.Settings.TestMode
                ? context.Settings.TestApiKey
                : context.Settings.LiveApiKey);
            var client = new Client(clientConfig);

            var pkgLength = context.MeasurementSystem == MeasurementSystem.Metric ? CmToIn(package.Length) : package.Length;
            var pkgWidth = context.MeasurementSystem == MeasurementSystem.Metric ? CmToIn(package.Width) : package.Width;
            var pkgHeight = context.MeasurementSystem == MeasurementSystem.Metric ? CmToIn(package.Height) : package.Height;
            var pkgWeight = context.MeasurementSystem == MeasurementSystem.Metric ? KgToOz(package.Weight) : LbToOz(package.Weight);
            var customerName = $"{context.Order.CustomerInfo.FirstName} {context.Order.CustomerInfo.LastName}".Trim();

            var request = new EasyPostParams.Beta.Rate.Retrieve
            {
                Reference = context.Order.Id.ToString(),
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

            if (!string.IsNullOrWhiteSpace(context.Settings.CarrierAccounts))
            {
                var carrierAccount = context.Settings.CarrierAccounts.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => new CarrierAccount { Id = x })
                        .ToArray();
                if (carrierAccount.Length > 0)
                {
                    request.CarrierAccounts.AddRange(carrierAccount);
                }
            }

            var rates = await client.Beta.Rate.RetrieveStatelessRates(request, cancellationToken).ConfigureAwait(false);
            var orderCurrency = Context.Services.CurrencyService.GetCurrency(context.Order.CurrencyId);

            return new ShippingRatesResult
            {
                Rates = rates
                    .Where(x => x.Currency.Equals(orderCurrency.Code, StringComparison.OrdinalIgnoreCase))
                    .Select(x => new ShippingRate(
                            new Price(decimal.Parse(x.Price ?? "0"), 0, context.Order.CurrencyId),
                            new ShippingOption(CreateCompositeId(x.Carrier, x.Service), CreateDescription(x.Carrier, x.Service)),
                            package.Id
                        )).ToList()
            };
        }

        private static string CreateCompositeId(string carrierCode, string productCode)
            => $"{carrierCode}__{productCode}".Trim('_');
        private static string CreateDescription(string carrierCode, string productCode)
            => $"{PASCAL_CASE_PATTERN.Replace(carrierCode, " ")} - {PASCAL_CASE_PATTERN.Replace(productCode, " ")}".Trim(' ', '-');
    }
}
