using System.Collections.Generic;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// These are PSBoundParameters to be used while PowerShell.Invoke().
    /// </summary>
    internal class SHiPSParameters
    {
        internal bool Force { get; set; }
        internal bool Verbose { get;  set; }
        internal bool Debug { get;  set; }

        internal Dictionary<string, object> BoundParameters { get; set; }
    }
}
