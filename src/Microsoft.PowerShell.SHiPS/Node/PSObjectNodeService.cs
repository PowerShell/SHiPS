using System.Management.Automation;
using CodeOwls.PowerShell.Paths.Extensions;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines actions that applies to a PSObject node.
    /// </summary>
    internal class PSObjectNodeService : PathNodeBase
    {
        private readonly PSObject _pso;
        private string _name = null;
        private static readonly string _leaf = ".";

        internal PSObjectNodeService(object pso)
        {
            _pso = pso as PSObject;
        }

        public override IPathValue GetNodeValue()
        {
            //PSObjects are the leaf nodes only
            return new LeafPathValue(_pso, Name);
        }

        public override string ItemMode
        {
            get
            {
                return _leaf;
            }
        }

        public override string Name
        {
            get
            {
                if (_name != null) { return _name;}
                if (_pso != null)
                {
                    //When name contains slash, it breaks provider navigation or caching. calling MakeSafeForPath()
                    //to replace slash with '-' before caching it. Otherwise, we will get cache missing.
                    _name = _pso.SafeGetPropertyValue<object>("Name", () => _pso.ToString())?.ToString().MakeSafeForPath();
                    return _name;
                }

                return string.Empty;
            }
        }
    }
}
