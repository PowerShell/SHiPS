# SHiPS-based Sample Provider - Show Progress and Other Attributes

## Step 1- Install SHiPS

Follow the instructions in [the front page][readme] to download and install the SHiPS.

## Step 2 - Review the sample code
See the [ShowProgress module][sp].

## Step 3 - Try it out

``` PowerShell
Import-Module  SHiPS                   
Import-Module  .\samples\ShowProgress.psm1
new-psdrive -name n -psprovider SHiPS -root ShowProgress#NoBuiltinProgress

cd n:
dir
dir -verbose

```

## Key takeaways from this example
- By default SHiPS is not caching data returned from Get-ChildItem
- Sometimes it can be slow when you accessing data store especially through the Internet,
you may want to cache the data for better user experience.
to do so, you can add an attribute to your class, e.g.,

  ``` PowerShell
  [SHiPSProvider(UseCache=$true)]

  ```
- By default SHiPS shows progress. However you can disable it by adding the following attribute:

  ``` PowerShell
  [SHiPSProvider(BuiltinProgress= $false)]

  ```
- If needed, you can add Write-Progress in your PowerShell module.

[readme]: ../README.md#Installing-SHiPS
[sp]:../samples/ShowProgress.psm1
