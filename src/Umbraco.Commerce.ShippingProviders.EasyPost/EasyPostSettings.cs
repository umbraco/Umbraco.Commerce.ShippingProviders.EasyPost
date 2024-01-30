using Umbraco.Commerce.Core.ShippingProviders;

namespace Umbraco.Commerce.ShippingProviders.EasyPost
{
    public class EasyPostSettings
    {
        [ShippingProviderSetting(Name = "Test API Key",
            Description = "The Test API Key from the Easypost portal.",
            SortOrder = 200)]
        public string TestApiKey { get; set; }

        [ShippingProviderSetting(Name = "Live API Key",
            Description = "The Live API Key from the Easypost portal.",
            SortOrder = 200)]
        public string LiveApiKey { get; set; }

        [ShippingProviderSetting(Name = "Test Mode",
            Description = "Set whether to run in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }
    }
}
