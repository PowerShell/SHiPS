# Sample Provider - Library

## Step 1- Install SHiPS

Follow the instructions [in here][readme] to download and install the SHiPS.

## Step 2 - Review the sample code

See the [Library module][fm].

## Step 3 - Try it out

  ```powershell
  cd <yourclonefolder>\SHiPS
  Import-Module  SHiPS
  Import-Module  .\samples\Library\Library.psm1

  new-psdrive -name M -psprovider SHiPS -root 'Library#Music'
  cd M:
  dir
  

## Key Takeaways from this example

- Demonstrated how to use Get-Content and Set-Content cmdlet
  - Get-Content is supported on Leaf node only
  - Set-Content is supported on both Leaf and Directory. A child node is created under the current directory if the item does not exist.
 


[readme]: ../../README.md#Installing-SHiPS
[fm]: Library.psm1
