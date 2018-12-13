using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface ISetItemContent
    {
        IContentWriter GetContentWriter(IProviderContext providerContext);
        object GetContentWriterDynamicParameters(IProviderContext providerContext);
    }
}
