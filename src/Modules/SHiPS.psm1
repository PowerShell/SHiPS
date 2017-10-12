#
# Script module for module 'Simple Hierarchy in PowerShell(SHiPS)'
#
Set-StrictMode -Version Latest

# Set up some helper variables to make it easier to work with the module
$script:PSModule = $ExecutionContext.SessionState.Module
$script:PSModuleRoot = $script:PSModule.ModuleBase
$script:SHiPSAssemblyName = 'Microsoft.PowerShell.SHiPS.dll'

# Try to import the SHiPS assembly at the same directory regardless fullclr or coreclr
$SHiPSModulePath = Join-Path -Path $script:PSModuleRoot -ChildPath $script:SHiPSAssemblyName
$binaryModuleRoot = $script:PSModuleRoot

if(-not (Test-Path -Path $SHiPSModulePath))
{
    # Import the appropriate nested binary module based on the current PowerShell version
    $binaryModuleRoot = Join-Path -Path $script:PSModuleRoot -ChildPath 'fullclr'

    if (($PSVersionTable.Keys -contains "PSEdition") -and ($PSVersionTable.PSEdition -ne 'Desktop')) {
        $binaryModuleRoot = Join-Path -Path $script:PSModuleRoot -ChildPath 'coreclr'
    }
    
    $SHiPSModulePath = Join-Path -Path $binaryModuleRoot -ChildPath $script:SHiPSAssemblyName
}

$SHiPSModule = Import-Module -Name $SHiPSModulePath -PassThru

# When the module is unloaded, remove the nested binary module that was loaded with it
if($SHiPSModule)
{
    $script:PSModule.OnRemove = {
        Remove-Module -ModuleInfo $SHiPSModule
    }
}
