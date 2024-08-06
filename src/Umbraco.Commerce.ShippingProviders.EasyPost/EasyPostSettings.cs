using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.EasyPost
{
    public class EasyPostSettings
    {
        [ShippingProviderSetting(SortOrder = 200)]
        public string TestApiKey { get; set; }

        [ShippingProviderSetting(SortOrder = 200)]
        public string LiveApiKey { get; set; }

        [ShippingProviderSetting(SortOrder = 10000)]
        public bool TestMode { get; set; }

        [ShippingProviderSetting(SortOrder = 10100, IsAdvanced = true)]
        public string CarrierAccounts { get; set; }
    }
}
