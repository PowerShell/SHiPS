# Simple Hierarchy in PowerShell (SHiPS)

A [PowerShell provider][ps-provider] allows any data store to be exposed like a file system as if it were a mounted drive.
In other words, the data in your data store can be treated like files and directories so that a user can navigate data via `cd` or `dir`.
SHiPS is a PowerShell provider.
To be more precise it's a provider utility that simplifies developing PowerShell providers.

## Build Status

### Development branch

| AppVeyor (Windows)       | Travis CI (Linux / macOS) |
|--------------------------|--------------------------|
| [![av-image-dev][]][av-site-dev] | [![tv-image-dev][]][tv-site-dev] |


### Master branch
| AppVeyor (Windows)       | Travis CI (Linux / macOS) |
|--------------------------|--------------------------|
| [![av-image-master][]][av-site-master] | [![tv-image-master][]][tv-site-master] |

[av-image-master]: https://ci.appveyor.com/api/projects/status/jjy56evq75bxn5w4/branch/master?svg=true
[av-site-master]: https://ci.appveyor.com/project/PowerShell/SHiPS/history/branch/master
[tv-image-master]: https://travis-ci.org/PowerShell/SHiPS.svg?branch=master
[tv-site-master]: https://travis-ci.org/PowerShell/SHiPS/branches

[av-image-dev]:https://ci.appveyor.com/api/projects/status/jjy56evq75bxn5w4/branch/development?svg=true
[av-site-dev]: https://ci.appveyor.com/project/PowerShell/SHiPS/history/branch/development
[tv-image-dev]: https://travis-ci.org/PowerShell/SHiPS.svg?branch=development
[tv-site-dev]: https://travis-ci.org/PowerShell/SHiPS/branches

### Nightly run Master branch


| AppVeyor (Windows)
|--------------------------
| [![av-image-master-n][]][av-site-master-n]

[av-image-master-n]: https://ci.appveyor.com/api/projects/status/od48qs1sf6xo3ro0/branch/master?svg=true
[av-site-master-n]: https://ci.appveyor.com/project/PowerShell/ships-yfgug/branch/master


## Supported Platform

- Windows
  - PowerShell v5 (or later), which is shipped in Win10, Windows Server 2016, or [WMF 5.1][wmf51]
  - [.Net Framework 4.7.1][dotnet471]
- Linux or Mac
  - [PowerShell Core][ps]

## Downloading the Source Code

git clone https://github.com/PowerShell/SHiPS.git

## Building the Source Code

```powerShell
cd <yourclonefolder>\SHiPS\src\
# get the dotnet CLI tool
# and Windows10 SDK if you are running on Windows
.\bootstrap.ps1

# build SHiPS
.\build.ps1 Release

```

## Installing SHiPS

- You can install SHiPS from the [PowerShell Gallery][psgallery]
- Install SHiPS' binaries which you just built on your box:
  ```powerShell
  # you need to launch PowerShell as Administrator
  cd <yourclonefolder>\SHiPS
  Import-Module .\tools\setup.psm1
  Install-SHiPS
  ```

## Running Unit Tests

```powerShell
Import-Module .\tools\setup.psm1
Invoke-SHiPSTest
```

## Try It Out

Let's take the [FamilyTree][ft] module as our example here.
Assuming you have done the above steps, i.e., git clone, build, and run Install-SHiPS, now try the following.

```powerShell
Import-Module SHiPS
Import-Module  .\samples\FamilyTree

# create a PowerShell drive.
new-psdrive -name Austin -psprovider SHiPS -root 'FamilyTree#Austin'
cd Austin:

dir
cd Ben
dir
```

The output looks like below.

```powerShell
PS Austin:\> dir
    Container: Microsoft.PowerShell.SHiPS\SHiPS::FamilyTree#Austin
Type       Name
----       ----
+          Ben
.          Bill

PS Austin:\> cd .\Ben\
PS Austin:\Ben> dir
    Container: Microsoft.PowerShell.SHiPS\SHiPS::FamilyTree#Austin

Type       Name
----       ----
.          Chris
.          Cathy

PS Austin:\Ben> dir | %{$_.Data}
Name  DOB  Gender
----  ---  ------
Chris 5034 M
Cathy 5050 F
```

In fact, we can create a drive at any level. Let's say we are interested in Ben only, we can do something like this:

```powershell
new-psdrive -name son -psprovider SHiPS -root 'FamilyTree#Ben'
cd son:
dir
```

In addition, this can be useful for the isolated testing.

See more samples under [sample folder][sample] to try out.

## Get Started with Writing a PowerShell Provider

If you'd like to try out writing a SHiPS-based provider in PowerShell, we recommend reviewing [the getting started documentation][getstarted].

## SHiPS Architecture

See [here][design] for design details.

## FAQ

See [known issues, FAQ, etc.][faq]

## Developing and Contributing

Please follow [the PowerShell Contribution Guide][ps-contribution] for how to contribute.

## Legal and Licensing

SHiPS is under the [MIT license][license].

[ps]: https://github.com/PowerShell/PowerShell
[ps-provider]: https://msdn.microsoft.com/en-us/powershell/reference/5.1/microsoft.powershell.core/about/about_providers
[ps-contribution]: https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md
[wmf51]: https://www.microsoft.com/en-us/download/details.aspx?id=54616
[license]: /LICENSE.txt
[design]: /docs/Design.md
[sample]: /samples/
[ft]: /samples/FamilyTree
[getstarted]: /docs/README.md
[faq]: /docs/FAQ.md
[psgallery]: https://www.powershellgallery.com/packages/SHiPS
[dotnet471]: http://go.microsoft.com/fwlink/?LinkId=852095
