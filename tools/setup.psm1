#region script variables
$script:SHiPS = 'SHiPS'
$script:IsInbox = $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase)
$script:IsWindows = (-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows
$script:IsLinux = (Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux
$script:IsMacOS = (Get-Variable -Name IsMacOS -ErrorAction Ignore) -and $IsMacOS
$script:IsCoreCLR = (Get-Variable -Name IsCoreCLR -ErrorAction Ignore) -and $IsCoreCLR


if($script:IsInbox) {
    $script:ProgramFilesPSPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramFiles -ChildPath "WindowsPowerShell"
} else {
    $script:ProgramFilesPSPath = $PSHome
}

$script:ProgramFilesModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath "Modules"


# AppVeyor.yml sets a value to $env:PowerShellEdition variable,
# otherwise set $script:PowerShellEdition value based on the current PowerShell Edition.
$script:PowerShellEdition = [System.Environment]::GetEnvironmentVariable("PowerShellEdition")
if(-not $script:PowerShellEdition) {
    if($script:IsCoreCLR) {
        $script:PowerShellEdition = 'Core'
    } else {
        $script:PowerShellEdition = 'Desktop'
    }
}

Write-Host "PowerShellEdition value: $script:PowerShellEdition"

$ClonedProjectPath = Resolve-Path "$PSScriptRoot\.."
$script:TestHome= "$ClonedProjectPath\test\"
$OutputBin = "$($script:TestHome)\..\src\out\SHiPS\"


#endregion script variables

function Install-Dependencies {

    # Install any dependences here

    # Update build title for daily builds
    if($script:IsWindows -and (Test-DailyBuild)) {
        if($env:APPVEYOR_PULL_REQUEST_TITLE)
        {
            $buildName += $env:APPVEYOR_PULL_REQUEST_TITLE
        } else {
            $buildName += $env:APPVEYOR_REPO_COMMIT_MESSAGE
        }

        if(-not ($buildName.StartsWith("[Daily]", [System.StringComparison]::OrdinalIgnoreCase))) {
            Update-AppveyorBuild -message "[Daily] $buildName"
        }
    }
}


function Get-PSHome {
    $PowerShellHome = $PSHOME

    # Install PowerShell Core on Windows.
    if(($script:PowerShellEdition -eq 'Core') -and $script:IsWindows)
    {
        # install-powershell.ps1 depends on NuGet provider. Let's install it first.
        $null=Get-PackageProvider -Name NuGet -Force -ForceBootstrap

        ## need to specify TLS version 1.2 since GitHub API requires it
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

        $InstallPSCoreUrl = 'https://aka.ms/install-pscore'
        $InstallPSCorePath = Microsoft.PowerShell.Management\Join-Path -Path $PSScriptRoot -ChildPath 'install-powershell.ps1'
        $null=Microsoft.PowerShell.Utility\Invoke-RestMethod -Uri $InstallPSCoreUrl -OutFile $InstallPSCorePath

        $PowerShellHome = "$env:SystemDrive\PowerShellCore"
        $null=& $InstallPSCorePath -Destination $PowerShellHome -Daily -AddToPath

        if(-not $PowerShellHome -or -not (Microsoft.PowerShell.Management\Test-Path -Path $PowerShellHome -PathType Container))
        {
            Throw "$PowerShellHome path is not available."
        }

        Write-Host ("PowerShell Home Path: '{0}'" -f $PowerShellHome)
    }

    return $PowerShellHome
}

function Install-SHiPS
{
    param(
        [switch]$TestModule = $false
    )

    Write-Host -ForegroundColor Green "`$env:PS_DAILY_BUILD value $env:PS_DAILY_BUILD"
    Write-Host -ForegroundColor Green "`$env:APPVEYOR_SCHEDULED_BUILD value $env:APPVEYOR_SCHEDULED_BUILD"
    Write-Host -ForegroundColor Green "`$env:APPVEYOR_REPO_TAG_NAME value $env:APPVEYOR_REPO_TAG_NAME"
    Write-Host -ForegroundColor Green "TRAVIS_EVENT_TYPE environment variable value $([System.Environment]::GetEnvironmentVariable('TRAVIS_EVENT_TYPE'))"
    Write-Host -ForegroundColor Green "Test-DailyBuild: $(Test-DailyBuild)"

    $env:APPVEYOR_TEST_PASS = $true

    # Install PowerShell it does not exist
    $PowerShellHome = Get-PSHome
    Write-Verbose ("PowerShell Home: '{0}'" -f $PowerShellHome)

    #
    # Setup Test Environment
    #

    # Product setup
    $SHiPSModulesPath = $script:ProgramFilesModulesPath
    if(($script:PowerShellEdition -eq 'Core') -and $script:IsWindows)
    {
        $SHiPSModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $PowerShellHome -ChildPath 'Modules'
    }
    Write-Verbose ("SHiPSModulesPath: '{0}'" -f $SHiPSModulesPath)


    # Copy SHiPS module to PSHOME

    $SHiPSModuleInfo = Test-ModuleManifest "$OutputBin\SHiPS.psd1" -ErrorAction Ignore
    $ModuleVersion = "$($SHiPSModuleInfo.Version)"

    $InstallLocation =  Microsoft.PowerShell.Management\Join-Path -Path $SHiPSModulesPath -ChildPath "SHiPS\$ModuleVersion"

    $null = New-Item -Path $InstallLocation -ItemType Directory -Force

    # Copy binaries to coreclr and fullclr folder. $testframework is coreclr or fullclr

    Write-Verbose "Copied latest SHiPS to $InstallLocation"
    Microsoft.PowerShell.Management\Copy-Item "$OutputBin\*" -Destination $InstallLocation\ -Recurse -Force -verbose


    #Ignore strong name signing
    $null = Use-IgnoreStrongName


    #
    # Install Test modules
    #

    if($TestModule.IsPresent)
    {
        $SHiPSTestInstallLocation =  Microsoft.PowerShell.Management\Join-Path -Path $SHiPSModulesPath -ChildPath "SHiPSTest"
        $null = New-Item -Path $SHiPSTestInstallLocation -ItemType Directory -Force
        $null = Microsoft.PowerShell.Management\Copy-Item "$script:TestHome\automation\SHiPSTest\*" -Destination $SHiPSTestInstallLocation\ -Recurse -Force -verbose
        $null = Microsoft.PowerShell.Management\Copy-Item "$script:TestHome\..\samples\Library\Library.psm1" -Destination $script:TestHome\automation\ -Recurse -Force -verbose
    }

    return  $PowerShellHome
}

function Invoke-SHiPSTest {

    #
    # Install SHiPS and its test files
    #

    $PowerShellHome = Install-SHiPS -TestModule
    Write-Host "PowerShellHome is $PowerShellHome IsWindows=$script:IsWindows script:IsCoreCLR=$script:IsCoreCLR IsCoreCLR=$IsCoreCLR PowerShellEdition=$script:PowerShellEdition"

    if($script:PowerShellEdition -eq 'Core')
    {
        $PowerShellExePath = 'pwsh'
    }
    else {
        $PowerShellExePath = Join-Path -Path $PowerShellHome -ChildPath 'PowerShell.exe'
    }

    #
    # Invoke test
    #

    try {
        Push-Location $script:TestHome

        $TestResultsFile = Microsoft.PowerShell.Management\Join-Path -Path $script:TestHome -ChildPath "TestResults.xml"
        if($script:PowerShellEdition -eq 'Core')
        {
            & $PowerShellExePath -Command "`$env:PSModulePath ; `$PSVersionTable; `$ProgressPreference = 'SilentlyContinue';Install-Module Pester -force; Import-Module Pester; Invoke-Pester -Script $script:TestHome -OutputFormat NUnitXml -OutputFile $TestResultsFile"
        }
        else {
            & $PowerShellExePath -Command "`$env:PSModulePath ; `$PSVersionTable; `$ProgressPreference = 'SilentlyContinue'; Invoke-Pester -Script $script:TestHome -OutputFormat NUnitXml -OutputFile $TestResultsFile"
        }
        $TestResults += [xml](Get-Content -Raw -Path $TestResultsFile)
    }
    finally {
        Pop-Location
    }

    #
    # Report test results
    #

    $FailedTestCount = 0
    $TestResults | ForEach-Object { $FailedTestCount += ([int]$_.'test-results'.failures); $total += ([int]$_.'test-results'.total) }

    if ($FailedTestCount -or $total -eq 0)
    {
        throw "$FailedTestCount tests failed"
    }
}

# tests if we should run a daily build
# returns true if the build is scheduled
# or is a pushed tag
function Test-DailyBuild
{
    # https://docs.travis-ci.com/user/environment-variables/
    # TRAVIS_EVENT_TYPE: Indicates how the build was triggered.
    # One of push, pull_request, api, cron.
    $TRAVIS_EVENT_TYPE = [System.Environment]::GetEnvironmentVariable('TRAVIS_EVENT_TYPE')
    if(($env:PS_DAILY_BUILD -eq 'True') -or
       ($env:APPVEYOR_SCHEDULED_BUILD -eq 'True') -or
       ($env:APPVEYOR_REPO_TAG_NAME) -or
       ($TRAVIS_EVENT_TYPE -eq 'cron') -or
       ($TRAVIS_EVENT_TYPE -eq 'api'))
    {
        return $true
    }

    return $false
}
function Use-IgnoreStrongName
{
    if($script:IsWindows)
    {
      try
      {
          reg ADD "HKLM\Software\Microsoft\StrongName\Verification\Microsoft.PowerShell.SHiPS,31bf3856ad364e35"  /f
          reg ADD "HKLM\Software\Microsoft\StrongName\Verification\CodeOwls.PowerShell.Paths,31bf3856ad364e35"  /f
          reg ADD "HKLM\Software\Microsoft\StrongName\Verification\CodeOwls.PowerShell.Provider,31bf3856ad364e35"  /f

          reg ADD "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\Microsoft.PowerShell.SHiPS,31bf3856ad364e35"  /f
          reg ADD "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\CodeOwls.PowerShell.Paths,31bf3856ad364e35"  /f
          reg ADD "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\CodeOwls.PowerShell.Provider,31bf3856ad364e35"  /f
      }
      catch{}
    }
}
