
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Provider.PathNodes
{
    public interface IMoveItem
    {
        object MoveItemParameters { get; }

        IPathValue MoveItem(IProviderContext providerContext, string path, string movePath, IPathValue destinationContainer);
    }
}
