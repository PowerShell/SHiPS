using System.Management.Automation;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using Microsoft.PowerShell.SHiPS;
using ProviderContext = CodeOwls.PowerShell.Provider.PathNodeProcessors.ProviderContext;

namespace CodeOwls.PowerShell.Paths;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines actions that applies to a SHiPSLeaf node.
    /// </summary>
    internal class NodeServiceBase : PathNodeBase,
        IGetItemContent,
        ISetItemContent,
        IClearItemContent
    {
        public override string Name
        {
            get { return _shipsNode.Name; }
        }

        public override IPathValue GetNodeValue()
        {
            return new PathValue(_shipsNode, Name, false);
        }

        private SHiPSBase _shipsNode;

        internal NodeServiceBase(object shipsNode)
        {
            _shipsNode = shipsNode as SHiPSBase;
        }

        #region IContentCmdletProvider - Get/Set/Clear-Content
        public void ClearContent(IProviderContext providerContext)
        {
            var cmdletContext = providerContext as ProviderContext;
            GetNotSupportedMessage(cmdletContext);
        }

        public object ClearContentDynamicParameters(IProviderContext providerContext)
        {
            throw new System.NotImplementedException();
        }

        public IContentWriter GetContentWriter(IProviderContext providerContext)
        {
            throw new System.NotImplementedException();
        }

        public object GetContentWriterDynamicParameters(IProviderContext providerContext)
        {
            throw new System.NotImplementedException();
        }

        public IContentReader GetContentReader(IProviderContext providerContext)
        {
            throw new System.NotImplementedException();
        }

        public object GetContentReaderDynamicParameters(IProviderContext providerContext)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region private methods
        /// <summary>
        /// Compose the NotSupported Message.
        /// </summary>
        /// <param name="command">Command executed</param>
        /// <param name="path">Path parameter of the command</param>
        /// <returns>The Error Message</returns>
        private string GetNotSupportedMessage(ProviderContext ctx)
        {
            var fullpath = ctx.Path?.Replace(ctx.Drive.Root, ctx.Drive.Name + ":");
            return $"This operation is not supported in {ctx.Drive.Name} drive. Current path: {fullpath}";
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
