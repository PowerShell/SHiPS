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
    public class SHiPSProvider : CodeOwls.PowerShell.Provider.Provider,
        IContentCmdletProvider
    {
        public const string ProviderName = "SHiPS";
        //internal static Regex PathRegex = new Regex(@"^([^:\\/]*?):[\\/](.*)$", RegexOptions.IgnoreCase);
        internal static Regex WellFormatPathRegex = new Regex(@"^([^:\\/]*?)[:\\/]+(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
 
        //Support pattern: Module#Type:\\foobar
        internal static Regex ModuleRegex = new Regex(@"^(.[^#]+\s*#[^:\\/]+)[:\\/]*(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private string driveNameWithColon;

        internal SHiPSDrive Drive
        {
            get
            {
                return PSDriveInfo as SHiPSDrive;
            }
        }

        internal string DriveNameWithColon
        {
            get
            {
                if(string.IsNullOrEmpty(driveNameWithColon))
                {
                    driveNameWithColon = Drive.Name + ":";
                }

                return driveNameWithColon;
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

        /// <summary>
        /// Throw terminating error for InvalidOperation.
        /// </summary>
        /// <param name="errorMessage">Error Message.</param>
        /// <param name="errorId">Error Id.</param>
        /// <param name="targetObject">Target Object</param>
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

        #region NotSupportedCommands

        // Implement IContentCmdletProvider. 
        // As these methods are not virtual in base class and we don't want to change base class, I use New and call the interface to enforce the polymorphism.
        // More Explain: https://stackoverflow.com/questions/12314990/override-method-implementation-declared-in-an-interface
        #region IContentCmdletProvider - Get/Set/Clear-Content

        public new void ClearContent(string path)
        {
            var errorMsg = GetNotSupportedMessage("Clear-Content", path);
            WriteErrorNotImplemented(errorMsg);
        }

        public new object ClearContentDynamicParameters(string path)
        {
            ClearContent(path);
            return null;
        }

        void IContentCmdletProvider.ClearContent(string path)
        {
            ClearContent(path);
        }

        object IContentCmdletProvider.ClearContentDynamicParameters(string path)
        {
            return ClearContentDynamicParameters(path);
        }

        public new IContentReader GetContentReader(string path)
        {
            var errorMsg = GetNotSupportedMessage("Get-Content", path);
            WriteErrorNotImplemented(errorMsg);

            return null;
        }

        public new object GetContentReaderDynamicParameters(string path)
        {
            return GetContentReader(path);
        }

        IContentReader IContentCmdletProvider.GetContentReader(string path)
        {
            return GetContentReader(path);
        }

        object IContentCmdletProvider.GetContentReaderDynamicParameters(string path)
        {
            return GetContentReaderDynamicParameters(path);
        }

        public new IContentWriter GetContentWriter(string path)
        {
            var errorMsg = GetNotSupportedMessage("Set-Content", path);
            WriteErrorNotImplemented(errorMsg);

            return null;
        }

        public new object GetContentWriterDynamicParameters(string path)
        {
            return GetContentWriter(path);
        }

        IContentWriter IContentCmdletProvider.GetContentWriter(string path)
        {
            return GetContentWriter(path);
        }

        object IContentCmdletProvider.GetContentWriterDynamicParameters(string path)
        {
            return GetContentWriterDynamicParameters(path);
        }
        #endregion

        // Implement the abstract methods of NavigationCmdletProvider.
        #region NavigationCmdletProvider - MoveItem

        protected override void MoveItem(string path, string destination)
        {
            var errorMsg = GetNotSupportedMessage("Move-Item", path);
            WriteErrorNotImplemented(errorMsg);
        }

        protected override object MoveItemDynamicParameters(string path, string destination)
        {
            MoveItem(path, destination);

            return null;
        }

        #endregion

        // Implement the abstract methods of ContainerCmdletProvider.
        #region ContainerCmdletProvider - CopyItem, NewItem, RemoveItem, RenameItem
        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            var errorMsg = GetNotSupportedMessage("Copy-Item", path);
            WriteErrorNotImplemented(errorMsg);
        }

        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        {
            CopyItem(path, destination, recurse);

            return null;
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            var errorMsg = GetNotSupportedMessage("New-Item", path);
            WriteErrorNotImplemented(errorMsg);
        }

        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            NewItem(path, itemTypeName, newItemValue);

            return null;
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            var errorMsg = GetNotSupportedMessage("Remove-Item", path);
            WriteErrorNotImplemented(errorMsg);
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            RemoveItem(path, recurse);

            return null;
        }

        protected override void RenameItem(string path, string newName)
        {
            var errorMsg = GetNotSupportedMessage("Rename-Item", path);
            WriteErrorNotImplemented(errorMsg);
        }

        protected override object RenameItemDynamicParameters(string path, string newName)
        {
            RenameItem(path, newName);

            return null;
        }

        #endregion

        // Implement the abstract methods of ItemCmdletProvider.
        #region ItemCmdletProvider - ClearItem, SetItem
        protected override void ClearItem(string path)
        {
            var errorMsg = GetNotSupportedMessage("Clear-Item", path);
            WriteErrorNotImplemented(errorMsg);

            base.ClearItem(path);
        }

        protected override object ClearItemDynamicParameters(string path)
        {
            ClearItem(path);

            return base.ClearItemDynamicParameters(path);
        }

        protected override void SetItem(string path, object value)
        {
            var errorMsg = GetNotSupportedMessage("Set-Item", path);
            WriteErrorNotImplemented(errorMsg);
        }

        protected override object SetItemDynamicParameters(string path, object value)
        {
            SetItem(path, value);

            return null;
        }

        #endregion

        /// <summary>
        /// Compose the NotSupported Message.
        /// </summary>
        /// <param name="command">Command executed</param>
        /// <param name="path">Path parameter of the command</param>
        /// <returns>The Error Message</returns>
        private string GetNotSupportedMessage(string command, string path)
        {
            return $"{command} is not supported in {DriveNameWithColon} drive. Current path: {ReplaceDriveRootWithName(path)}";
        }

        /// <summary>
        /// Get user friendly path.
        /// </summary>
        /// <param name="path">Full path in SHiPSProvider, e.g. AzurePSDrive#Azure\AuthMethods_EDOG</param>
        /// <returns>User friendly path, e.g. Azure:\AuthMethods_EDOG</returns>
        private string ReplaceDriveRootWithName(string path)
        {
            return path?.Replace(Drive.Root, DriveNameWithColon);
        }

        /// <summary>
        /// WriteError of NotSupported. This will allow the cmdlet to continue execution.
        /// </summary>
        /// <param name="errorMessage">Error Message</param>
        /// <param name="targetObject">Target Object</param>
        private void WriteErrorNotImplemented(string errorMessage, object targetObject = null)
        {
            var ex = new PSNotSupportedException(errorMessage);
            ErrorRecord er = new ErrorRecord(ex, ErrorId.NotSupported, ErrorCategory.NotImplemented, targetObject);

            // WriteError will not terminate right away, so we can get handling of -ErrorVariable, -ErrorAction.
            WriteError(er);
        }
        #endregion
    }
}
