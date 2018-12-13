using System.Collections.Generic;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// A class containing the current node information.
    /// </summary>
    internal class PathNodeInfo
    {
        internal string Path { get; set; }
        internal string PathWithNoEndSlash { get; set; }
        internal IPathNode NodeObject { get; set; }
        internal List<IPathNode> Children { get; set; }

        internal void Set(string path, IPathNode node, List<IPathNode> children)
        {
            Path = path;
            PathWithNoEndSlash = path?.TrimEnd('/', '\\');
            NodeObject = node;
            Children = children;
        }
    }
}
