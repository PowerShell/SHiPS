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
        internal static readonly string SetContentNotSupportedErrorId = "SetContent.NotSupported";

    }

    internal static class Constants
    {
        internal static readonly string Leaf = "Leaf";
        internal static readonly string GetChildItemDynamicParameters = "GetChildItemDynamicParameters";
        internal static readonly string InvokeItemDynamicParameters = "InvokeItemDynamicParameters";
        internal static readonly string GetChildItem = "GetChildItem";
        internal static readonly string GetContent = "GetContent";
        internal static readonly string SetContent = "SetContent";
        internal static readonly string ScriptBlockWithParam1  = "[CmdletBinding()] param([object]$object) $object.{0}()";
        internal static readonly string ScriptBlockWithParam2 = "[CmdletBinding()] param([object]$object, [string]$p0) $object.{0}($p0)";
        internal static readonly string ScriptBlockWithParam3 = "[CmdletBinding()] param([object]$object, [string]$p0, [string]$p1) $object.{0}($p0, $p1)";


        internal static string[] DefinedCommands = {
            "Set-Location",
            "Get-Location",
            "Pop-Location",
            "Push-Location",
            "Get-ChildItem",
            "Resolve-Path",
            "Get-Item",
            "Test-Path",
            "Get-Content",
            "Set-Content",
             // Below are NotSupported commands, but we do handle their error messages.
            "Clear-Content",
            "Move-Item",
            "Copy-Item",
            "New-Item",
            "Remove-Item",
            "Rename-Item",
            "Clear-Item",
            "Set-Item",
            "Invoke-Item"
        };
    }
}
