# Sample Provider - Show Progress and Other Attributes

## Step 1- Install SHiPS

Follow the instructions [in here][readme] to download and install the SHiPS.

## Step 2 - Review the sample code

See the [ShowProgress module][sp].

## Step 3 - Try it out

```powershell
Import-Module  SHiPS
Import-Module  .\samples\ShowProgress
new-psdrive -name n -psprovider SHiPS -root ShowProgress#NoBuiltinProgress

cd n:
dir
dir -verbose
```

## Key takeaways from this example

- By default SHiPS is not caching data returned from Get-ChildItem
- Sometimes it can be slow when you accessing data store especially through the Internet, you may want to cache the data for better user experience. To do so, you can add an attribute to your class, e.g.,

  ```powershell
  [SHiPSProvider(UseCache=$true)]
  ```
- By default SHiPS shows progress. However you can disable it by adding the following attribute:

  ```powershell
  [SHiPSProvider(BuiltinProgress= $false)]
  ```
- If needed, you can add Write-Progress in your PowerShell module.

[readme]: ../../README.md#Installing-SHiPS
[sp]:ShowProgress.psm1
