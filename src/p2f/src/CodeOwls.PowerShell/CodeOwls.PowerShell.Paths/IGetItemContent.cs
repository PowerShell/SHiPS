using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Provider;
using System.Text;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetItemContent
    {
        IContentReader GetContentReader(IProviderContext providerContext);
        object GetContentReaderDynamicParameters(IProviderContext providerContext);
    }
}
