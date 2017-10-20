# Sample Provider - FamilyTree

## Step 1- Install SHiPS

Follow the instructions [in here][readme] to download and install the SHiPS.

## Step 2 - Review the sample code

See the [FamilyTree module][fm].

## Step 3 - Try it out

  ```powershell
  cd <yourclonefolder>\SHiPS
  Import-Module  SHiPS
  Import-Module  .\samples\FamilyTree

  new-psdrive -name Austin -psprovider SHiPS -root 'FamilyTree#Austin'
  cd Austin:
  dir
  cd Ben

  dir | %{$_.data}

  PS Austin:\> dir

      Container: Microsoft.PowerShell.SHiPS\SHiPS::FamilyTree#Austin

  Mode  Name
  ----  ----
  +     Ben
  .     Bill


  PS Austin:\>cd Ben
  PS Austin:\Ben>
  PS Austin:\Ben> dir

    Container: Microsoft.PowerShell.SHiPS\SHiPS::FamilyTree#Austin\Ben
  Mode  Name
  ----  ----
  .     Chris
  .     Cathy

  PS Austin:\Ben>dir | %{$_.Data}

  Name  DOB  Gender
  ----  ---  ------
  Chris 5034 M
  Cathy 5050 F

```

## Key Takeaways from this example

- Demonstrated how to use a node property
  - We defined a class, named as `Person`.
  - In the type Chris and Cathy, we exposed $Data field as a node's property
  - When a user `dir` under Ben, it will show two child nodes, Chris and Cathy. But if you do `dir | fl *`, you will see a `Data` property which contains more data about the node.

[readme]: ../../README.md#Installing-SHiPS
[fm]: FamilyTree.psm1
