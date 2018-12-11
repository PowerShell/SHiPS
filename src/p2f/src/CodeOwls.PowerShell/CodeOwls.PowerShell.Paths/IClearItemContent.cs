using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface IClearItemContent
    {
        void ClearContent(IProviderContext providerContext);
        object ClearContentDynamicParameters(IProviderContext providerContext);
    }
}
