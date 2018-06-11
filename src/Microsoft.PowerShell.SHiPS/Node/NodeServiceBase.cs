using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines actions that applies to a SHiPSLeaf node.
    /// </summary>
    internal class LeafNodeService : PathNodeBase
    {
        private readonly SHiPSLeaf _shipsLeaf;
        private static readonly string _leaf = ".";

        internal LeafNodeService(object leafObject)
        {
            _shipsLeaf = leafObject as SHiPSLeaf;
        }

        public override IPathValue GetNodeValue()
        {
            return new LeafPathValue(_shipsLeaf, Name);
        }

        public override string ItemMode
        {
            get {return _leaf; }
        }

        public override string Name
        {
            get { return _shipsLeaf.Name; }
        }
    }
}
