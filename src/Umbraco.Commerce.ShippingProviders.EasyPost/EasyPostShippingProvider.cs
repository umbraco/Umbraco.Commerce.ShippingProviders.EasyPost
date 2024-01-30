using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Commerce.Common.Logging;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.EasyPost
{
    [ShippingProvider("easypost", "EasyPost", "EasyPost shipping provider")]
    public class EasyPostShippingProvider : ShippingProviderBase<EasyPostSettings>
    {
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
            return null;

            var client = EasyPostClient.Create(_httpClientFactory, context.Settings);
            //var package = context.Packages.FirstOrDefault();

            //var request = new ShipmondoQuoteListRequest
            //{
            //    Receiver = new ShipmondoAddress
            //    {
            //        Address1 = package.ReceiverAddress.AddressLine1,
            //        Address2 = package.ReceiverAddress.AddressLine2,
            //        City = package.ReceiverAddress.City,
            //        ZipCode = package.ReceiverAddress.ZipCode,
            //        CountryCode = package.ReceiverAddress.CountryIsoCode
            //    },
            //    Sender = new ShipmondoAddress
            //    {
            //        Address1 = package.SenderAddress.AddressLine1,
            //        Address2 = package.SenderAddress.AddressLine2,
            //        City = package.SenderAddress.City,
            //        ZipCode = package.SenderAddress.ZipCode,
            //        CountryCode = package.SenderAddress.CountryIsoCode
            //    }
            //};

            //var l = context.MeasurementSystem == MeasurementSystem.Metric ? package.Length : InToCm(package.Length);
            //var w = context.MeasurementSystem == MeasurementSystem.Metric ? package.Width : InToCm(package.Width);
            //var h = context.MeasurementSystem == MeasurementSystem.Metric ? package.Height : InToCm(package.Height);
            //var wg = context.MeasurementSystem == MeasurementSystem.Metric ? package.Weight : LbToKg(package.Weight);

            //request.Parcels.Add(new ShipmondoParcel
            //{
            //    Description = context.Order.OrderNumber,
            //    Weight = (int)Math.Ceiling(wg * 1000), // Kg to Grams
            //    Length = (int)Math.Ceiling(l),
            //    Width = (int)Math.Ceiling(w),
            //    Height = (int)Math.Ceiling(h)
            //});

            //var quotes = await client.GetQuoteList(request).ConfigureAwait(false);
            //var orderCurrency = Context.Services.CurrencyService.GetCurrency(context.Order.CurrencyId);

            //return new ShippingRatesResult
            //{
            //    Rates = quotes
            //        //.Where(x => x.CurrencyCode.Equals(orderCurrency.Code, StringComparison.OrdinalIgnoreCase))
            //        .Select(x => new ShippingRate(
            //                new Price(x.PriceBeforeVat, x.Price - x.PriceBeforeVat, context.Order.CurrencyId),
            //                new ShippingOption(CreateCompositeId(x.CarrierCode, x.ProductCode), x.Description),
            //                package.Id
            //            )).ToList()
            //};
        }

        private static string CreateCompositeId(string carrierCode, string productCode)
            => $"{carrierCode}__{productCode}".Trim('_');
    }
}
