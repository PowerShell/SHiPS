namespace Microsoft.PowerShell.SHiPS
{
    internal static class ErrorId
    {
        internal static readonly string InvalidRootFormat = "InvalidRootFormat";
        internal static readonly string CannotGetModule = "CannotGetModule";
        internal static readonly string CannotCreateInstance = "CannotCreateInstance";
        internal static readonly string RootNodeTypeMustBeContainer = "RootNodeTypeMustBeContainer";
        internal static readonly string NodeNameIsNullOrEmpty = "NodeNameIsNullOrEmpty";
        internal static readonly string NewDriveRootDoesNotExist = "NewDriveRootDoesNotExist";
        internal static readonly string NotContainerNode = "NotContainerNode";      
    }

    internal static class Constants
    {
        internal static readonly string Leaf = "Leaf";
        internal static readonly string GetChildItemDynamicParameters = "GetChildItemDynamicParameters";
        internal static readonly string GetChildItem = "GetChildItem";

        internal static string[] SupportedCommands = {
            "Set-Location",
            "Get-Location",
            "Pop-Location",
            "Push-Location",
            "Get-ChildItem",
            "Resolve-Path",
            "Get-Item",
            "Test-Path"
        };
    }
}
