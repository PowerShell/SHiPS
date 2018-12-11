using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Utility class for handling Get-Content and Set-Content.
    /// </summary>
    internal class ContentHelper
    {
        private readonly SHiPSBase _node;
        private readonly SHiPSDrive _drive;

        internal ContentHelper(SHiPSBase node, SHiPSDrive drive)
        {
            _node = node;
            _drive = drive;
        }

        #region IGetItemContent

        public IContentReader GetContentReader(IProviderContext context)
        {
            // Calling GetContent()
            var script = Constants.ScriptBlockWithParam1.StringFormat(Constants.GetContent);
            var results = PSScriptRunner.InvokeScriptBlock(context, _node, _drive, script, PSScriptRunner.ReportErrors);

            // Expected a collection of strings returned from GetContent() and save it to a stream
            var stream = new ContentReader(results, context);
            return stream;
        }

        public object GetContentReaderDynamicParameters(IProviderContext context)
        {
            return null;
        }

        #endregion

        #region ISetItemContent

        public IContentWriter GetContentWriter(IProviderContext context)
        {
            var stream = new ContentWriter(context, _drive, _node);
            return stream;
        }

        public object GetContentWriterDynamicParameters(IProviderContext context)
        {
            return null;
        }

        #endregion

        #region IClearItemContent

        public void ClearContent(IProviderContext providerContext)
        {
            // Define ClearContent for now as the PowerShell engine calls ClearContent first for Set-Content cmdlet.
            return;
        }

        public object ClearContentDynamicParameters(IProviderContext providerContext)
        {
            return null;
        }

        #endregion
    }
}
