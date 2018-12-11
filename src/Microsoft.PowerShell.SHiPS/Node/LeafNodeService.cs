using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines actions that applies to a SHiPSLeaf node.
    /// </summary>
    internal class LeafNodeService : PathNodeBase,
        IGetItemContent,
        ISetItemContent,
        IClearItemContent
    {
        private readonly SHiPSLeaf _shipsLeaf;
        private static readonly string _leaf = ".";
        private readonly SHiPSDrive _drive;
        private readonly ContentHelper _contentHelper;

        internal LeafNodeService(object leafObject, SHiPSDrive drive, SHiPSDirectory parent)
        {
            _shipsLeaf = leafObject as SHiPSLeaf;
            _drive = drive;
            _contentHelper = new ContentHelper(_shipsLeaf, drive);
            if (_shipsLeaf != null) { _shipsLeaf.Parent = parent; }
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

        #region IGetItemContent

        public IContentReader GetContentReader(IProviderContext context)
        {
            return _contentHelper.GetContentReader(context);
        }

        public object GetContentReaderDynamicParameters(IProviderContext context)
        {
            return _contentHelper.GetContentReaderDynamicParameters(context);
            ;
        }

        #endregion

        #region ISetItemContent

        public IContentWriter GetContentWriter(IProviderContext context)
        {
            return _contentHelper.GetContentWriter(context);
        }

        public object GetContentWriterDynamicParameters(IProviderContext context)
        {
            return _contentHelper.GetContentWriterDynamicParameters(context);
        }

        #endregion

        #region IClearItemContent

        public void ClearContent(IProviderContext context)
        {
            // Define ClearContent for now as the PowerShell engine calls ClearContent first for Set-Content cmdlet.
            _contentHelper.ClearContent(context);
        }

        public object ClearContentDynamicParameters(IProviderContext context)
        {
            return _contentHelper.ClearContentDynamicParameters(context);
        }

        #endregion
    }
}
