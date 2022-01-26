using System;
using System.Collections.Generic;
using Unleash;
using Unleash.Internal;
using Unleash.Variants;

namespace ShitpostBot.Infrastructure
{
    public class UnleashClientOptions
    {
        public bool Enabled { get; set; }
        
        public string Token { get; set; }
        
        public string Url { get; set; }
    }

    public class FakeUnleash : IUnleash, IDisposable
    {
        public void Dispose()
        {
        }

        public bool IsEnabled(string toggleName)
        {
            return true;
        }

        public bool IsEnabled(string toggleName, bool defaultSetting)
        {
            return true;
        }

        public bool IsEnabled(string toggleName, UnleashContext context)
        {
            return true;
        }

        public bool IsEnabled(string toggleName, UnleashContext context, bool defaultSetting)
        {
            return true;
        }

        public Variant GetVariant(string toggleName)
        {
            throw new NotImplementedException();
        }

        public Variant GetVariant(string toggleName, Variant defaultValue)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<VariantDefinition> GetVariants(string toggleName)
        {
            throw new NotImplementedException();
        }

        public ICollection<FeatureToggle> FeatureToggles { get; }
    }
}