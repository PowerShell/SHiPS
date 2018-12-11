using System.Collections.Generic;
using System.Linq;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Given a user path, resolves the corresponding node object that the provider can interpret.
    /// </summary>
    public class PathResolver : IPathResolver
    {
        private readonly SHiPSProvider _provider;
        private readonly SHiPSDrive _drive;

        internal PathResolver(SHiPSProvider provider, SHiPSDrive driveInfo)
        {
            _provider = provider;
            _drive = driveInfo;
        }

        /// <summary>
        /// Resolves to corresponding path node object given a string path.
        /// Usage:cd Azure:
        /// </summary>
        /// <param name="context">provider context</param>
        /// <param name="path">string path given by PowerShell engine</param>
        /// <returns></returns>
        public IEnumerable<IPathNode> ResolvePath(IProviderContext context, string path)
        {
            return ResolvePath(context, path, false);
        }

        /// <summary>
        ///  Resolves to corresponding path node object given a string path.
        /// </summary>
        /// <param name="context">provider context</param>
        /// <param name="path">string path given by PowerShell engine</param>
        /// <param name="force">
        ///  Whether force to refresh cache. For example, dir myitem -force
        ///  Currently this parameter is used in GetItem.
        /// </param>
        /// <returns></returns>
        internal IEnumerable<IPathNode> ResolvePath(IProviderContext context, string path, bool force)
        {
            // We do not call log here. Because PowerShell engine either does not log anything or throw nullref
            // in System.Management.Automation.MshCommandRuntime.ThrowIfWriteNotPermitted
            // context.WriteDebug("Resolving path [{0}] drive [{1}]".StringFormat(path, _drive != null ? _drive.Name : string.Empty));

            if (_drive == null)
            {
                yield break;
            }

            // See Issue: https://github.com/PowerShell/SHiPS/issues/10
            // While a user is under the drive and run some cmdlets such as Get-Module -listavailable,
            // the PowerShell engine calls its all registered providers to see whether
            // any providers can resolve these files defined in .psd1 manifest file.
            // In this case, our provider does not know these files as its child item, cache misses. So it calls GetChildItem(),
            // which triggers the engine to call us again. Cycling around, causing potential deadlock or significant delays.
            // Workaround: filter out all other cmdlets that SHiPS does not handle.
            if (!context.IsProviderDefinedCommand())
            {
                yield break;
            }

            var path1 =  path.TrimDrive(_drive);
            var item = GetNodeObjectFromPath(context, path1, force);
            if (item != null)
            {
                yield return item;
            }
        }


        private IPathNode GetNodeObjectFromPath(IProviderContext context, string path, bool force)
        {
            var parts = path.Split('/', '\\');

            if ((parts.Length == 1) && string.IsNullOrWhiteSpace(parts[0]))
            {
                // path is the root
                return _drive.RootPathNode;
            }

            // When resolving a path, we start from the root node where we know the object already.
            // It can be costly though, for case like "cd" [tab] if there is no cache enabled.
            var item = _drive.RootPathNode as ContainerNodeService;

            if (item == null)
            {
                return null;
            }

            // We consider the same operation if the path matches
            var lastVisisted = SHiPSProvider.LastVisisted;

            if (lastVisisted.NodeObject != null && lastVisisted.Path.EqualsIgnoreCase(context.Path))
            {
                return lastVisisted.NodeObject;
            }

            IPathNode child = null;
            // Filter out any empty or null elements. Note: 'Where' statement preserves the original order.
            // https://stackoverflow.com/questions/204505/preserving-order-with-linq
            var partsExcludeNullOrEmpty = parts.Where(each => !string.IsNullOrWhiteSpace(each)).ToList();

            var pwdParts = _drive.CurrentLocation.Split('/', '\\').Where(each => !string.IsNullOrWhiteSpace(each)).ToList();

            // Travel through the path to ensure each node from the root to leaf all exists
            for (var i = 0; i < partsExcludeNullOrEmpty.Count; i++)
            {
                // Making sure to obey the StopProcessing.
                if (context.Stopping)
                {
                    return null;
                }

                // We do not need to refresh the entire path while resolving the path. Refresh the last node only.
                // Cases:
                // dir -force                    GetChildItems() will refresh the data. ItemExists() will not go out fetch data.
                // dir .\leaf.ps1 -force         GetItem() refreshes the last node under the current path.
                // dir .\foo\bar\leaf.ps1 -force GetItem() refreshes the last node under the current path, i.e., Node 'bar' folder in this example.

                child = GetNodeObjectFromPath(context, item, partsExcludeNullOrEmpty[i], force && (i > pwdParts.Count - 1));

                if (child == null)
                {
                    //the node on the path does not exist
                    break;
                }

                // is a shipsdirectory?
                item = child as ContainerNodeService;
            }

            // Save the info for the node just visited
            lastVisisted.Set(context.Path, child, null);

            return child;
        }

        private IPathNode GetNodeObjectFromPath(IProviderContext context, ContainerNodeService parent, string pathName, bool force)
        {
            var parentNode = parent?.ContainerNode;
            if (parentNode == null) { return null; }

            // Check if the node has been dir'ed
            // Here we do not need to add NeedRefresh check because:
            // For cd (set-location), there is no -force, i.e., NeedRefresh is always false. This means for cached cases,
            // a user needs to do 'dir -force' to get fresh data.
            // For dir case, path in ResolvePath() is pointing to the parent path, e.g., dir c:\foo\bar\baz.txt,
            // the path is poing to c:\foo\bar even if baz.txt just gets created. Thus ResolvePath() only needs to resolve
            // the parent path and the GetChildItem() will check NeedRefresh to get fresh data.
            if (!force && parentNode.UseCache && parentNode.ItemNavigated)
            {
                var nodes = parentNode.Children.Where(each => pathName.EqualsIgnoreCase(each.Key)).Select(item => item.Value)
                    .FirstOrDefault();

                if (nodes != null && nodes.Any())
                {
                    return nodes.FirstOrDefault();
                }
                else
                {
                    // If a childitem exists and cached, but none of them matches what specified in the 'pathName',
                    // we will return null, because
                    //
                    // dir         --- Assuming displays a long list of child items. So user does
                    // dir a[tab]  --- a user wants to see any child items start with 'a'. This may be no child item starts with a.
                    //                 so we do not need to go out fetching again if cache misses.
                    //
                    // caveat:
                    // dir foobar  --- if a user expects foobar child item exists, and cache misses, SHiPS is not going out fetch
                    //                 automatically, unless -force
                    // By removing the else block, SHiPS will go out fetching for data if cache misses. But dir a[tab] will be slow.
                    return null;
                }
            }

            var script = Constants.ScriptBlockWithParam1.StringFormat(Constants.GetChildItem);
            var children = PSScriptRunner.InvokeScriptBlockAndBuildTree(context, parentNode, _drive, script, PSScriptRunner.ReportErrors)?.ToList();
            if (children == null) { return null;}

            foreach (var node in children)
            {
                // Making sure to obey the StopProcessing.
                if (context.Stopping)
                {
                    return null;
                }

                //add it to its child list
                if (node != null)
                {
                    if (pathName.EqualsIgnoreCase(node.Name))
                    {
                        return node;
                    }
                }
            }
            return null;
        }
    }
}
