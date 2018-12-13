using CodeOwls.PowerShell.Paths.Extensions;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{

    /// <summary>
    /// Stores the SHiPS provider's the context data for PowerShell modules to use.
    /// </summary>
    public class ProviderContext
    {
        /// <summary>
        /// Gets the force property.
        /// </summary>
        public bool Force { get; internal set; }

        /// <summary>
        /// Gets the recurse property.
        /// </summary>
        public bool Recurse { get; internal set; }

        /// <summary>
        /// Gets the filter property that was supplied by a user.
        /// </summary>
        public string Filter { get; internal set; }

        /// <summary>
        /// Dynamic parameters passed in from commandline.
        /// </summary>
        public object DynamicParameters { get; internal set; }

        /// <summary>
        /// Cmdlet bound parameters.
        /// </summary>
        public object BoundParameters { get; internal set; }

        internal void Set(IProviderContext context)
        {
            Force = context.Force;
            Recurse = context.Recurse;
            Filter = context.Filter;
            DynamicParameters = context.DynamicParameters;
        }

        internal void Clear()
        {
            Force = false;
            Recurse = false;
            Filter = null;
            DynamicParameters = null;
        }
    }
}
