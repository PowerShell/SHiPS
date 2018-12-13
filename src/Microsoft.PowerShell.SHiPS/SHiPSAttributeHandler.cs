using System;
using System.Reflection;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// A helper class for getting SHiPSPropertyAttribute settings.
    /// </summary>
    internal class SHiPSAttributeHandler
    {
        private static SHiPSProviderAttribute defaultAttribute = new SHiPSProviderAttribute();

        internal static SHiPSProviderAttribute GetSHiPSProperty(Type type)
        {
            var descriptions = (SHiPSProviderAttribute[])type.GetTypeInfo().GetCustomAttributes(typeof(SHiPSProviderAttribute), inherit:true);

            if (descriptions.Length == 0)
            {
                // if a module does not specify any attributes, we use the default.
                return defaultAttribute;
            }
            return descriptions[0];
        }
    }
}
