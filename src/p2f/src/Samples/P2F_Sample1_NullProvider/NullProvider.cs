using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;

namespace ProviderFramework_1_TheNullProvider
{
    /// <summary>
    /// The provider class.
    /// </summary>
    [CmdletProvider("NullProvider", ProviderCapabilities.ShouldProcess)]
    public class NullProvider : CodeOwls.PowerShell.Provider.Provider
    {
        /// <summary>
        /// a required P2F override
        ///
        /// supplies P2F with the path processor for this provider
        /// </summary>
        protected override CodeOwls.PowerShell.Paths.Processors.IPathResolver PathResolver
        {
            get { return new NullPathResolver(); }
        }

        /// <summary>
        /// overridden to supply a default drive when the provider is loaded
        /// </summary>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            return new Collection<PSDriveInfo>
                       {
                           new NullDrive(
                               new PSDriveInfo( "Null", ProviderInfo, String.Empty, "Null Drive", null )
                           )
                       };
        }
    }
}
