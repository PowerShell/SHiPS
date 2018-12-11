using System;
using System.Collections.Generic;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{

    /// <summary>
    /// Defines a type that represents a node object which contains any child items.
    /// </summary>
    public class SHiPSDirectory : SHiPSBase
    {
        // Contains all child items of the current node.
        internal Dictionary<string, List<IPathNode>> Children = new Dictionary<string, List<IPathNode>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Default C'tor.
        /// </summary>
        public SHiPSDirectory()
        {
        }

        /// <summary>
        /// C'tor.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        public SHiPSDirectory(string name) : base (name, isLeaf:false)
        {
        }

        /// <summary>
        /// It is expected that the drive class in PowerShell implements the GetChildItem().
        /// </summary>
        /// <returns></returns>
        public virtual object[] GetChildItem()
        {
            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem cmdlet.
        /// </summary>
        /// <returns></returns>
        public virtual object GetChildItemDynamicParameters()
        {
            return null;
        }

        /// <summary>
        /// True if the current item has been visisted. This info is useful for cached case.
        /// </summary>
        internal bool ItemNavigated { get; set; }
    }
}
