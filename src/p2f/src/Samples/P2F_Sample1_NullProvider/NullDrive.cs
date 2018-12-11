using System.Management.Automation;

namespace ProviderFramework_1_TheNullProvider
{
    /// <summary>
    /// the drive class
    ///
    /// used by the NullProvider class to define the default drive
    /// for the provider
    /// </summary>
    public class NullDrive : CodeOwls.PowerShell.Provider.Drive
    {
        public NullDrive(PSDriveInfo driveInfo) : base(driveInfo)
        {
        }
    }
}
