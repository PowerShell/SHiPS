param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = "Debug",
    [ValidateSet('netstandard2.0')]   # Keep the Framework parameter in case we need to add a build against netstandard 3.0 e.g. later
    [string]$Framework = 'netstandard2.0',
    [Switch]$Verbose
)

#
# Variables
#


$script:IsWindowsOS = (-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows
$script:IsCoreCLRPlatform = (Get-Variable -Name IsCoreCLR -ErrorAction Ignore) -and $IsCoreCLR

$dotnetPath = "$env:LOCALAPPDATA\Microsoft\dotnet\;"

if(($env:PATH -split ';') -notcontains $dotnetPath)
{
    $env:path=$dotnetPath+$env:path
}

$solutionDir = $PSScriptRoot

Write-Output "Solution directory: '$solutionDir'."



#
# Help functions
#
Function CopyToDestinationDir($itemsToCopy, $destination)
{
    if (-not (Test-Path $destination))
    {
        New-Item -ItemType Directory $destination -Force
    }
    foreach ($file in $itemsToCopy)
    {
        Write-Verbose "file=$file" -Verbose:$Verbose

        if (Test-Path $file)
        {
            Copy-Item -Path $file -Destination $destination -Verbose:$Verbose -Force
            #Copy-Item -Path $file -Destination (Join-Path $destination (Split-Path $file -Leaf)) -Verbose -Force
        }
    }
}


$P2FPath='p2f\src\CodeOwls.PowerShell'
$SHiPSAssemblyName = 'Microsoft.PowerShell.SHiPS'

$AssemblyPaths = @(
    "$P2FPath\CodeOwls.PowerShell.Paths",
    "$P2FPath\CodeOwls.PowerShell.Provider",
    $SHiPSAssemblyName

)

#
# Resgen
#
Push-Location $solutionDir -Verbose:$Verbose

    Write-Output "Generating resources file for $SHiPSAssemblyName"
    .\New-StronglyTypedCsFileForResx.ps1 -Project $SHiPSAssemblyName

Pop-Location

#
# Building SHiPS
#

try
{
    foreach ($assemblyPath in $AssemblyPaths)
    {
        Push-Location $solutionDir\$assemblyPath
        dotnet restore
        dotnet build   --framework $Framework --configuration $Configuration
        #dotnet publish --framework $Framework --configuration $Configuration

        Pop-Location
    }
}
finally
{

}



#
# Copy out the binaries, pdb, and module files
#

Push-Location $solutionDir -Verbose:$Verbose
$Binaries = $AssemblyPaths | % { "$solutionDir\$_\bin\$Configuration\$Framework\*.dll"; "$solutionDir\$_\bin\any cpu\$Configuration\$Framework\*.dll" }
$Pdbs = $AssemblyPaths | % { "$solutionDir\$_\bin\$Configuration\$Framework\*.pdb" ; "$solutionDir\$_\bin\any cpu\$Configuration\$Framework\*.pdb"}

$SHiPSManifest = @("$solutionDir/Modules/SHiPS.psd1", "$solutionDir/Modules/SHiPS.psm1","$solutionDir/Modules/SHiPS.formats.ps1xml")

$destinationDir = "$solutionDir/out/SHiPS/"
$destinationDirBinaries = "$destinationDir"


CopyToDestinationDir $SHiPSManifest $destinationDir
CopyToDestinationDir $SHiPSManifest $destinationDirBinaries
CopyToDestinationDir $Binaries $destinationDirBinaries
CopyToDestinationDir $Pdbs $destinationDirBinaries

# we do not want to pack this sma dll
$sma= "$destinationDirBinaries\System.Management.Automation.dll"

if(test-path $sma)
{
    Remove-Item $sma -force -Verbose:$Verbose
}


#
#Packing
#
$sourcePath = $destinationDir
$packagePath= Split-Path -Path $sourcePath
$packageFileName = Join-Path $packagePath "SHiPS.zip"

if(test-path $packageFileName)
{
    Remove-Item $packageFileName -force
}


if($script:IsWindowsOS -and (-not $script:IsCoreCLRPlatform))
{
    Add-Type -Assembly System.IO.Compression.FileSystem | Out-Null
}

Write-Output "Zipping $sourcePath into $packageFileName"
[System.IO.Compression.ZipFile]::CreateFromDirectory($sourcePath, $packageFileName)


#
# Removing the build generated files
#


$BuildGenerated = @(
    "./Microsoft.PowerShell.SHiPS/Resources/Microsoft.PowerShell.SHiPS.Resources.Resource.resx",
    "./Microsoft.PowerShell.SHiPS/bin/",
    "./Microsoft.PowerShell.SHiPS/gen/",
    "./Microsoft.PowerShell.SHiPS/obj/",
    "./p2f/src/CodeOwls.PowerShell/CodeOwls.PowerShell.Paths/bin/",
    "./p2f/src/CodeOwls.PowerShell/CodeOwls.PowerShell.Paths/obj/",
    "./p2f/src/CodeOwls.PowerShell/CodeOwls.PowerShell.Provider/bin/",
    "./p2f/src/CodeOwls.PowerShell/CodeOwls.PowerShell.Provider/obj/"
)

$BuildGenerated | ForEach-Object {Remove-Item -Path $_ -Recurse -force -ErrorAction SilentlyContinue -Verbose:$Verbose}


if(test-path $packageFileName)
{
    Write-Output "Compilation Completed. SHiPS module is located at $sourcePath."
}


Pop-Location
