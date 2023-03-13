using Microsoft.Extensions.Configuration;
using System;

namespace Sinapsa.GoL.DistOrleansProc.Orleans.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IConfigurationSection GetConfigurationSectionOrThrow(this IConfiguration configuration, string sectionName)
        {
            var section = configuration.GetSection(sectionName);

            return section.Exists()
                ? section
                : throw new Exception(sectionName);
        }
    }
}
