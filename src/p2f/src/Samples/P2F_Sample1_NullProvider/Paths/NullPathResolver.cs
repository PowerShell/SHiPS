using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace ProviderFramework_1_TheNullProvider
{
    /// <summary>
    /// the path node processor
    /// </summary>
    class NullPathResolver : PathResolverBase
    {
        /// <summary>
        /// returns the first node factory object in the path graph
        /// </summary>
        protected override IPathNode Root
        {
            get
            {
                return new NullRootPathNode();
            }
        }
    }
}
