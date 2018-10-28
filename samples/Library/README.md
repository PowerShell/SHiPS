# Sample Provider - Library

## Step 1- Install SHiPS

Follow the instructions [in here][readme] to download and install the SHiPS.

## Step 2 - Review the sample code

See the [Library module][fm].

## Step 3 - Try it out

  ``` powershell
  cd <yourclonefolder>\SHiPS
  Import-Module  SHiPS
  Import-Module  .\samples\Library\Library.psm1
  new-psdrive -name M -psprovider SHiPS -root 'Library#Music'

  cd M:
  PS M:\> dir

    Directory: M:

  Mode  Name
  ----  ----
  +     Classic
  +     Rock

  PS M:\> cd .\Classic\
  PS M:\Classic> Get-Content .\SwanLake

      Tchaikovsky's magical ballet tells ...

  PS M:\Classic>Set-Content .\Nocturnes  -Value "Beautiful"
  PS M:\Classic> dir

      Directory: MM:\Classic

  Mode  Name
  ----  ----
  .     SwanLake
  .     BlueDanube
  .     Nocturnes


  PS M:\Classic> Get-Content .\Nocturnes

  Beautiful

 ```

## Key Takeaways from this example

- Demonstrated how to use Get-Content and Set-Content cmdlet
  - Get-Content is supported on Leaf node only
  - Set-Content is supported on both Leaf and Directory. A child node is created under the current directory if the item does not exist.

[readme]: ../../README.md#Installing-SHiPS
[fm]: Library.psm1
