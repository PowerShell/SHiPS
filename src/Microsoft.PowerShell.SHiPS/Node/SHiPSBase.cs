using CodeOwls.PowerShell.Paths.Extensions;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines a base type for node objects.
    /// </summary>
    public abstract class SHiPSBase
    {
        internal static bool UseCacheDefaultValue = false;
        internal static bool BuiltinProgressDefaultValue = true;
        internal readonly ProviderContext SHiPSProviderContext = new ProviderContext();
        private bool? _builtinProgress = null;
        private bool? _useCache = null;

        /// <summary>
        /// Default C'tor.
        /// </summary>
        protected SHiPSBase() : this(null)
        {
        }

        /// <summary>
        /// C'tor.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        protected SHiPSBase(string name) : this (name, isLeaf:false)
        {
        }

        /// <summary>
        /// C'tor.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        /// <param name="isLeaf">True if the node is a leaf node; false, the node has child items.</param>
        protected SHiPSBase(string name, bool isLeaf)
        {
            //When name contains slash, it breaks provider navigation or caching.
            //We changed it to '-' before caching it. Otherwise, we will get cache missing every time.

            Name = name.MakeSafeForPath();
            IsLeaf = isLeaf;
            if (!isLeaf)
            {
                var pp = SHiPSAttributeHandler.GetSHiPSProperty(this.GetType());
                if (pp != null)
                {
                    UseCache = pp.UseCache;
                    BuiltinProgress = pp.BuiltinProgress;
                }
            }
        }

        /// <summary>
        /// Name of a node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A subset of context data from its base provider, i.e., CmdletProvider.
        /// The context under which the cmdlet is being called.
        /// </summary>
        protected ProviderContext ProviderContext
        {
            get { return SHiPSProviderContext; }
        }

        #region Internal Properties

        /// <summary>
        /// True if the node is a leaf node.
        /// </summary>
        internal bool IsLeaf { get; set; }

        /// <summary>
        /// True, data from Get-ChildItem call will be cached. False otherwise.
        /// By default, UseCache is set to false.
        /// </summary>
        internal bool UseCache
        {
            get
            {
                return _useCache ?? UseCacheDefaultValue;
            }
            set { _useCache = value; }
        }

        /// <summary>
        /// True SHiPS will call Write-Progress. False otherwise.
        /// By default BuiltinProgress is set to true.
        /// </summary>
        internal bool BuiltinProgress
        {
            get
            {
                return _builtinProgress ?? BuiltinProgressDefaultValue;
            }
            set { _builtinProgress = value; }
        }

        /// <summary>
        /// Parent node
        /// </summary>
        internal SHiPSDirectory Parent { get; set; }

        #endregion
    }
}
