try {
    $IsWindows = (-not (Get-Variable -Name IsWindows -ErrorAction Ignore)) -or $IsWindows
    $IsLinux = (Get-Variable -Name IsLinux -ErrorAction Ignore) -and $IsLinux
    $IsMacOS = (Get-Variable -Name IsMacOS -ErrorAction Ignore) -and $IsMacOS
    $IsCoreCLR = (Get-Variable -Name IsCoreCLR -ErrorAction Ignore) -and $IsCoreCLR
}
catch {
    # on linux error from PowerShell: "Cannot overwrite variable IsLinux because it is read-only".
}

Write-Verbose -Message  "IsWindows=$IsWindows; IsLinux=$IsLinux; IsMacOS=$IsMacOS; IsCoreCLR=$IsCoreCLR" -Verbose

if ($IsLinux) {
    $LinuxInfo = Get-Content /etc/os-release | ConvertFrom-StringData

    $IsUbuntu = $LinuxInfo.ID -match 'ubuntu'
    $IsUbuntu14 = $IsUbuntu -and $LinuxInfo.VERSION_ID -match '14.04'
    $IsUbuntu16 = $IsUbuntu -and $LinuxInfo.VERSION_ID -match '16.04'
    $IsCentOS = $LinuxInfo.ID -match 'centos' -and $LinuxInfo.VERSION_ID -match '7'
}

function Start-DotnetBootstrap
{
    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')]
    param(
        [string]$Channel = 'preview',
        [string]$Version = '2.1.3'
    )

    # Install ours and .NET's dependencies
    $Deps = @()
    if ($IsUbuntu) {
        # Build tools
        $Deps += "curl", "g++", "cmake", "make"

        # .NET Core required runtime libraries
        $Deps += "libunwind8"
        if ($IsUbuntu14) { $Deps += "libicu52" }
        elseif ($IsUbuntu16) { $Deps += "libicu55" }

        # Install dependencies
        sudo apt-get install -y -qq $Deps
    } elseif ($IsCentOS) {
        # Build tools
        $Deps += "which", "curl", "gcc-c++", "cmake", "make"

        # .NET Core required runtime libraries
        $Deps += "libicu", "libunwind"

        # Install dependencies
        sudo yum install -y -q $Deps
    } elseif ($IsMacOS) {

        # Build tools
        $Deps += "curl", "cmake"

        # .NET Core required runtime libraries
        $Deps += "openssl"

        # Install dependencies
        brew install $Deps
    }

    $obtainUrl = "https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain"

    # Install for Linux and OS X
    if ($IsLinux -or $IsMacOS) {
        # Uninstall all previous dotnet packages
        $uninstallScript = if ($IsUbuntu) {
            "dotnet-uninstall-debian-packages.sh"
        } elseif ($IsMacOS) {
            "dotnet-uninstall-pkgs.sh"
        }

        if ($uninstallScript) {
            curl -s $obtainUrl/uninstall/$uninstallScript -o $uninstallScript
            chmod +x $uninstallScript
            sudo ./$uninstallScript
        } else {
            Write-Warning 'This script only removes prior versions of dotnet for Ubuntu 14.04 and OS X'
        }

        # Install new dotnet 1.0.0 preview packages
        $installScript = "dotnet-install.sh"
        curl -s $obtainUrl/$installScript -o $installScript
        chmod +x $installScript
        bash ./$installScript -c $Channel -v $Version

        # .NET Core's crypto library needs brew's OpenSSL libraries added to its rpath
        if ($IsMacOS) {
            # This is the library shipped with .NET Core
            # This is allowed to fail as the user may have installed other versions of dotnet
            Write-Warning '.NET Core links the incorrect OpenSSL, correcting .NET CLI libraries...'
            find $env:HOME/.dotnet -name System.Security.Cryptography.Native.dylib | xargs sudo install_name_tool -add_rpath /usr/local/opt/openssl/lib
        }
    }

    # Install for Windows
    if ($IsWindows) {
        Remove-Item -ErrorAction SilentlyContinue -Recurse -Force ~\AppData\Local\Microsoft\dotnet
        $installScript = 'dotnet-install.ps1'
        Invoke-WebRequest -Uri $obtainUrl/$installScript -OutFile $installScript
        & ./$installScript -Channel $Channel -Version $Version

        # Since we are downloading the installScript everytime, remove it after the run
        # to not create a diff in Git
        #Remove-Item $installScript -Verbose -Force
    }
}

$script:chocolateyPath = "$env:AllUsersProfile\chocolatey\bin"

function Test-Choco
{
    [OutputType([bool])]
    param()

    return ((Get-Command choco -ErrorAction SilentlyContinue) -ne $null)
}

function Install-Choco
{
    if(Test-Choco)
    {
        Write-Verbose -Message 'Chocolatey is already installed. Skipping installation.' -Verbose
    }
    else
    {
        Write-Verbose -Message 'Chocolatey not present. Installing chocolatey.' -Verbose
        Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

        $envVar = [Environment]::GetEnvironmentVariable('Path', 'MACHINE')

        if(($envVar -split ';') -notcontains $script:chocolateyPath)
        {
            $machinePath = [Environment]::GetEnvironmentVariable('Path', 'MACHINE')
            $newMachineEnvironmentPath = $machinePath
            Write-Verbose -Message "Adding $script:chocolateyPath to Path environment variable" -Verbose
            $newMachineEnvironmentPath += ";$script:chocolateyPath"
            $env:Path += ";$script:chocolateyPath"
            [Environment]::SetEnvironmentVariable('Path', $newMachineEnvironmentPath, 'MACHINE')
        }
        else
        {
            Write-Verbose -Message "$script:chocolateyPath already present in Path environment variable" -verbose
        }
    }
}

$script:win10sdkpackagename = 'windows-sdk-10'
# This is the minimum version of windows 10 SDK available in chocolatey for .NET Framework 4.6
$script:win10sdkpackageminimumversion='10.0.26624'
$script:win10sdkdisplaynamehint='Windows Software Development Kit - Windows 10*'

function Test-Windows10SDK
{
    [OutputType([bool])]
    param()

    # Use the PackageManagement to check if the Windows 10 SDK is installed
    # Not specifing the -name because we want to support as low of Windows 10 SDK version as possible and -name doesn't support wildcards.
    # The display name of Windows 10 SDK contains the version number.
    $installedsdk = get-package -ProviderName Programs -ErrorAction SilentlyContinue | Where-Object {$_.Name -like $script:win10sdkdisplaynamehint}
    return ($installedsdk -and ($installedsdk.version -ge $script:win10sdkpackageminimumversion))
}

function Install-Windows10SDK
{
    if (-not $IsWindows -or $IsCoreCLR) {
        return
    }

    Install-Choco

    if (Test-Windows10SDK)
    {
        Write-Verbose -Message "$script:win10sdkpackagename present. Skipping installation."  -Verbose
    }
    else
    {
        Write-Verbose  -Message "Windows 10 SDK not present. Installing $script:win10sdkpackageName with version $script:win10sdkpackageversion." -Verbose

        # Adding --force to take care of scenario where it is in choco catalog cache
        # Not specifing --version to allow chocolatey to install the latest version of the Windows 10 SDK
        choco install $script:win10sdkpackagename --force -y
    }
}

Start-DotnetBootstrap

Install-Windows10SDK
