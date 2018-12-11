using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines the implementation of a 'generic' PowerShell provider.  This provider
    /// allows for stateless namespace navigation of a system you are modeling.
    /// </summary>
    [CmdletProvider(SHiPSProvider.ProviderName, ProviderCapabilities.Filter | ProviderCapabilities.ShouldProcess)]
    public class SHiPSProvider : CodeOwls.PowerShell.Provider.Provider
    {
        public const string ProviderName = "SHiPS";
        //internal static Regex PathRegex = new Regex(@"^([^:\\/]*?):[\\/](.*)$", RegexOptions.IgnoreCase);
        internal static Regex WellFormatPathRegex = new Regex(@"^([^:\\/]*?)[:\\/]+(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        //Support pattern: Module#Type:\\foobar
        internal static Regex ModuleRegex = new Regex(@"^(.[^#]+\s*#[^:\\/]+)[:\\/]*(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal SHiPSDrive Drive
        {
            get
            {
                return PSDriveInfo as SHiPSDrive;
            }
        }

        protected override IPathResolver PathResolver
        {
            get
            {
                return Drive != null ? Drive.PathResolver : null;
            }
        }

        /// <summary>
        /// Give it a second chance to try to see if we can handle the FQ provider path.
        /// e.g., SHiPS\SHiPS::JT:\StorageAccount\
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected override IPathResolver PathResolver2(string path)
        {
            var match = ModuleRegex.Match(path);
            if (match.Success)
            {
                //deal with a case: New-PSDrive -Name HM -PSProvider SHiPS -Root Module#Type
                var driveName = match.Groups[1].Value;
                var drive = PSDriveInfo as SHiPSDrive ??
                            ProviderInfo.Drives.FirstOrDefault(each => each.Name.EqualsIgnoreCase(driveName) ||
                            each.Root.EqualsIgnoreCase(driveName)) as SHiPSDrive;
                return drive != null ? drive.PathResolver : null;
            }

            return null;
        }


        /// <summary>
        /// Determines if the specified drive can be created.
        /// </summary>
        /// <param name="drive"></param>
        /// <returns></returns>
        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            if (drive == null)
            {
                throw new ArgumentNullException("drive");
            }

            if (string.IsNullOrWhiteSpace(drive.Root))
            {
                ThrowError(Resources.Resource.NewDriveRootDoesNotExist, ErrorId.NewDriveRootDoesNotExist);
                return null;
            }

            return new SHiPSDrive(drive, drive.Root, this);
        }

        internal void ThrowError(string errorMessage, string errorId, object targetObject = null)
        {
            InvalidOperationException ex = new InvalidOperationException(errorMessage);
            ErrorRecord er = new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, targetObject);
            ThrowTerminatingError(er);
        }

        #region Path to Node Object Optimization

        // Note: The following methods are for reducing the calls to SHiPS based providers.
        //
        // When a user types "dir" for example, the PowerShell engine calls the methods defined in it's provider, including
        // ItemExists, IsItemContainer, GetChildItems.
        //
        // When a user types "cd tab" for example, the PowerShell engine calls it's provider the following,
        // ItemExists, IsValidPath, HasChildItems, GetChildNames, ItemExist, Get-Item, ItemExist, Get-Item, and so on.
        //
        // For each of the above API calls, eventually a SHiPS based provider gets called, i.e., calling from C# to PowerShell
        // module if the provider is defined in a PowerShell module. This call is not cheap.
        // To reduce such repeatedly calls, we can reuse some of the information pulled down from the previous API call.
        // Here we save the last visited node information to a variable, LastVisisted, so that
        // 1. If a user changes the directory, ItemExists will trigger the call to PowerShell. But its subsequence calls
        //     will reuse the information already stored in the LastVisisted during the ItemExists.
        // 2. If a user does not change directory, the node already visited. So return true rigth away in ItemExists.
        // 3. If a node somehow gets renamed or deleted, ItemExists and HasChildItems will always fetch fresh data if not using cache.
        //    With that, we can catch whether the item exists or not too.
        // 4. For 'dir' case, GetChildItems will always fetch fresh data if not using cache.


        internal static PathNodeInfo LastVisisted = new PathNodeInfo();

        protected override bool ItemExists(string path)
        {
            // We cannot use the lastvisisted node info only if path matches. This is because
            // the item could be just removed. So commented out the code below.
            // For example
            // dir ./foobar.ps1
            // delete foobar.ps1
            // dir ./foobar.ps1  should give an error.
            //if (LastVisisted.NodeObject != null && LastVisisted.Path.EqualsIgnoreCase(path))
            //{
            //    return true;
            //}

            // the path matches with any child node's path?
            if (LastVisisted?.Children != null &&
                LastVisisted.PathWithNoEndSlash.EqualsIgnoreCase(Path.GetDirectoryName(path)) &&
                LastVisisted.Children.Any(each => each.Name.EqualsIgnoreCase(Path.GetFileName(path))))
            {
                return true;
            }

            // initialize the PathToNodeTrace object
            LastVisisted.Set(path, null, null);

            // use the regular route, i.e., go fetching data and checking if the item exists
            var foo = base.ItemExists(path);
            return foo;
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            // Before calling this GetChildNames(), the PowerShell engine has called ItemExists, IsItemContainer,
            // and HasChildItems. HasChildItem will then call GetNodeChildren() -> GetNodeChildrenInternal().
            // GetNodeChildrenInternal() will call GetChildItems for fetching data.

            if (LastVisisted.NodeObject != null && LastVisisted.Children != null && LastVisisted.Path.EqualsIgnoreCase(path))
            {
                base.GetChildNames(path, LastVisisted.Children);
                return;
            }

            // Go fetching data
            base.GetChildNames(path, returnContainers);
        }

        protected override void GetItem(string path)
        {
            // Once we reached here, it means the item indeed exists. If misses, we go with the regular route, i.e. fetching data.
            if (LastVisisted?.Children != null && LastVisisted.PathWithNoEndSlash.EqualsIgnoreCase(Path.GetDirectoryName(path)))
            {
                var childname = Path.GetFileName(path);
                foreach (var node in LastVisisted.Children)
                {
                    if (Stopping)
                    {
                        return;
                    }

                    if (node.Name.EqualsIgnoreCase(childname))
                    {
                        GetItem(path, node);
                        return;
                    }
                }
            }

            // reset the node info object
            LastVisisted.Set(path, null, null);

            // Go fetching data to find out if the item exists
            base.GetItem(path);
        }

        /// <summary>
        /// There is a case when a user type 'dir myitem -force', the PowerShell engine calls GetItem() instead of GetChildItems().
        /// To support the above scenario, we do not put context.Force in the path resolver because ResolvePath is called
        /// in GetChildItemsDynamicParameters , ItemExist, IsItemContainer, blah, blah multiple places.
        /// To improve the performance, we add the logic for fetching data only in ItemExist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected override IEnumerable<IPathNode> GetNodeFactoryFromPathForItemExists(string path)
        {
            path = EnsurePathIsRooted(path);
            var pathResolver = PathResolver ?? PathResolver2(path);
            var shipsPathResolver = pathResolver as Microsoft.PowerShell.SHiPS.PathResolver;

            if (pathResolver == null || shipsPathResolver == null)
            {
                return Enumerable.Empty<IPathNode>();
            }

            var context = CreateContext(path);
            return shipsPathResolver.ResolvePath(context, path, context.Force);
        }

        #endregion
    }
}
