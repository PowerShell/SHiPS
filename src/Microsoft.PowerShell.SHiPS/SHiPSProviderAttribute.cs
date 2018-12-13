using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// This attribute is used on SHiPS' derived classes to mark proper actions that SHiPS
    /// is expected to take.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class SHiPSProviderAttribute : Attribute
    {
        /// <summary>
        /// True, SHiPS does caching from Get-ChildItem; false otherwise.
        /// </summary>
        public bool UseCache { get; set; } = SHiPSBase.UseCacheDefaultValue;

        /// <summary>
        /// Gets and sets a flag specifying whether SHiPS needs to Write-Progress.
        /// True, SHiPS will call Write-Progress; false it is assumed module will call Write-Progress.
        /// </summary>
        public bool BuiltinProgress { get; set; } = SHiPSBase.BuiltinProgressDefaultValue;
    }
}
