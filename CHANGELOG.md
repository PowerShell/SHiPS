CHANGELOG
========
## 0.8.1
* Added Get-Content and Set-Content.
## 0.8.0
* Switched to using .NET Standard 2.0 & PowerShellStandard.Library to allow a single cross-platform binary
   - It requires .NET 4.7.1 running on Windows PowerShell
  
## 0.7.5
* Added error handling for unsupported provider cmdlets.
## 0.7.2
* Bug fixes for 'dir -force' case.
## 0.7.1
* Perf improvement. For providers using cached data, [SHiPSProvider(UseCache=$true)], 'dir -force' will only refresh the last node in the path.
* Allowed to navigate home directory, e.g., 'dir ~' or 'cd ~' under SHiPS based provider drive.
## 0.7.0
* Fixed forward slash issue on Linux.
* Fixed Set-Location drive: path issue on Linux.
## 0.6.0
* Fixed Test-Path cmdlet.
* Updated SHiPS to build with .NET Core 2.0.5 to be in sync with pwsh.
* Fixed SHiPS formats.ps1xml.
## 0.3.0
* Big Bang
