#region script variables
$script:SHiPS = 'SHiPS'
$script:IsInbox = $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase)
$script:IsWindows = (-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows
$script:IsLinux = (Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux
$script:IsOSX = (Get-Variable -Name IsOSX -ErrorAction Ignore) -and $IsOSX
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

    # Install PowerShell Core MSI on Windows.
    if(($script:PowerShellEdition -eq 'Core') -and $script:IsWindows)
    {
        $PowerShellMsiPath = Get-PowerShellCoreBuild -AppVeyorProjectName 'PowerShell'
        $PowerShellInstallPath = "$env:SystemDrive\PowerShellCore"
        <#
        $PowerShellMsiUrl = 'https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.11/PowerShell_6.0.0.11-alpha.11-win81-x64.msi'
        $PowerShellMsiName = 'PowerShell_6.0.0.11-alpha.11-win81-x64.msi'
        $PowerShellMsiPath = Microsoft.PowerShell.Management\Join-Path -Path $PSScriptRoot -ChildPath $PowerShellMsiName
        Microsoft.PowerShell.Utility\Invoke-WebRequest -Uri $PowerShellMsiUrl -OutFile $PowerShellMsiPath
        #>
        Start-Process -FilePath "$env:SystemRoot\System32\msiexec.exe" -ArgumentList "/qb INSTALLFOLDER=$PowerShellInstallPath /i $PowerShellMsiPath" -Wait
        
        $PowerShellVersionPath = Get-ChildItem -Path $PowerShellInstallPath -Attributes Directory | Select-Object -First 1 -ErrorAction Ignore
        $PowerShellHome = $null
        if ($PowerShellVersionPath) {
            $PowerShellHome = $PowerShellVersionPath.FullName
        }
        
        if(-not $PowerShellHome -or -not (Microsoft.PowerShell.Management\Test-Path -Path $PowerShellHome -PathType Container))
        {
            Throw "$PowerShellHome path is not available."  
        }

        Write-Host ("PowerShell Home Path '{0}'" -f $PowerShellHome)
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

    #
    # Setup Test Environment
    #  

    # Product setup            
    $SHiPSModulesPath = $script:ProgramFilesModulesPath
    if(($script:PowerShellEdition -eq 'Core') -and $script:IsWindows)
    {
        $SHiPSModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $PowerShellHome -ChildPath 'Modules'
    }

    # Copy SHiPS module to PSHOME    
   
    $SHiPSModuleInfo = Test-ModuleManifest "$OutputBin\SHiPS.psd1" -ErrorAction Ignore
    $ModuleVersion = "$($SHiPSModuleInfo.Version)"

    $InstallLocation =  Microsoft.PowerShell.Management\Join-Path -Path $SHiPSModulesPath -ChildPath "SHiPS\$ModuleVersion"
    
    $null = New-Item -Path $InstallLocation -ItemType Directory -Force

    # Copy binaries to coreclr and fullclr folder. $testframework is coreclr or fullclr
 
    Write-Host "Copied latest SHiPS to $InstallLocation"
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
    }
    
    return  $PowerShellHome
}

function Invoke-SHiPSTest {    

    #
    # Install SHiPS and its test files
    #

    $PowerShellHome = Install-SHiPS -TestModule
    Write-Host "PowerShellHome is $PowerShellHome IsWindows=$script:IsWindows script:IsCoreCLR=$script:IsCoreCLR IsCoreCLR=$IsCoreCLR PowerShellEdition=$script:PowerShellEdition"
     
    if($script:IsWindows){
        $PowerShellExePath = Join-Path -Path $PowerShellHome -ChildPath 'PowerShell.exe'

        if($script:PowerShellEdition -eq 'Core')
        {
            # On Windows tests run agaist the pscore from its checkin build
            $PowerShellExePath = Join-Path -Path $PowerShellHome -ChildPath 'pwsh.exe'
            Write-Verbose "PowerShellExePath=$PowerShellExePath"
        }
    } else {
            # On Linux tests run against the pscore from its official release package          
            $PowerShellExePath = 'pwsh'
    }


    #
    # Invoke test
    # 

    try {
        Push-Location $script:TestHome

        $TestResultsFile = Microsoft.PowerShell.Management\Join-Path -Path $script:TestHome -ChildPath "TestResults.xml"
        & $PowerShellExePath -Command "`$env:PSModulePath ; `$PSVersionTable; `$ProgressPreference = 'SilentlyContinue'; Invoke-Pester -Script $script:TestHome -OutputFormat NUnitXml -OutputFile $TestResultsFile"

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

function Get-PowerShellCoreBuild {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]
        $AppVeyorProjectName = 'powershell-f975h',

        [Parameter()]
        [string]
        $GitHubBranchName = 'master',

        [Parameter()]
        [string]
        $Destination = 'C:\projects'
    )

    $appVeyorConstants =  @{ 
        AccountName = 'powershell'
        ApiUrl = 'https://ci.appveyor.com/api'
    }

    $foundGood = $false
    $records = 20
    $lastBuildId = $null
    $project = $null

    while(!$foundGood)
    {
        $startBuildIdString = [string]::Empty
        if($lastBuildId)
        {
            $startBuildIdString = "&startBuildId=$lastBuildId"
        }


        $project = Invoke-RestMethod -Method Get -Uri "$($appVeyorConstants.ApiUrl)/projects/$($appVeyorConstants.AccountName)/$AppVeyorProjectName/history?recordsNumber=$records$startBuildIdString&branch=$GitHubBranchName"

        foreach($build in $project.builds)
        {
            $version = $build.version
            $status = $build.status
            if($status -ieq 'success')
            {
                Write-Verbose "Using PowerShell Version: $version"

                $foundGood = $true

                Write-Host "Uri = $($appVeyorConstants.ApiUrl)/projects/$($appVeyorConstants.AccountName)/$AppVeyorProjectName/build/$version"
                $project = Invoke-RestMethod -Method Get -Uri "$($appVeyorConstants.ApiUrl)/projects/$($appVeyorConstants.AccountName)/$AppVeyorProjectName/build/$version" 
                break
            }
            else 
            {
                Write-Warning "There is a newer PowerShell build, $version, which is in status: $status"
            }
        }
    }

    # get project with last build details
    if (-not $project) {

        throw "Cannot find a good build for $GitHubBranchName"
    }

    # we assume here that build has a single job
    # get this job id

    $jobId = $project.build.jobs[0].jobId
    Write-Verbose "jobId=$jobId"
    
    Write-Verbose "$project.build.jobs[0]"

    $artifactsUrl = "$($appVeyorConstants.ApiUrl)/buildjobs/$jobId/artifacts"

    Write-Verbose "Uri=$artifactsUrl"
    $artifacts = Invoke-RestMethod -Method Get -Uri $artifactsUrl 

    if (-not $artifacts) {
        throw "Cannot find artifacts in $artifactsUrl"
    }

    # Get PowerShellCore.msi artifacts for Windows
    $artifacts = $artifacts | where-object { $_.filename -like '*powershell*.msi'}
    $returnArtifactsLocation = @{}

    #download artifacts to a temp location
    foreach($artifact in $artifacts)
    {
        $artifactPath = $artifact[0].fileName
        $artifactFileName = Split-Path -Path $artifactPath -Leaf

        # artifact will be downloaded as 
        $tempLocalArtifactPath = "$Destination\Temp-$artifactFileName-$jobId.msi"
        $localArtifactPath = "$Destination\$artifactFileName-$jobId.msi"
        if(!(Test-Path $localArtifactPath))
        {
            # download artifact
            # -OutFile - is local file name where artifact will be downloaded into

            try 
            {
                Write-Host "PowerShell MSI URL: $($appVeyorConstants.ApiUrl)/buildjobs/$jobId/artifacts/$artifactPath"
                $ProgressPreference = 'SilentlyContinue'
                Invoke-WebRequest -Method Get -Uri "$($appVeyorConstants.ApiUrl)/buildjobs/$jobId/artifacts/$artifactPath" `
                    -OutFile $tempLocalArtifactPath  -UseBasicParsing -DisableKeepAlive

                Move-Item -Path $tempLocalArtifactPath -Destination $localArtifactPath   
            } 
            finally
            {
                $ProgressPreference = 'Continue'
                if(test-path $tempLocalArtifactPath)
                {
                    remove-item $tempLocalArtifactPath
                }
            } 
        }
    }

    if(-not $localArtifactPath)
    {
        throw "Cannot find PowerShell.msi from PowerShell artifacts."
    }

    Write-Verbose $localArtifactPath
    return $localArtifactPath
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

