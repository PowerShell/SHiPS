# Getting Started with Authoring a SHiPS-based PowerShell Provider

## Design Document

- [SHiPS design details.][design]

## Public APIs, Parameters, etc.

- [See SHiPS APIs, parameters, etc.][api]

## Step 1 - Model your system

- Let's say I want to navigate the following tree:

```powershell
       Austin
       /   \
     Ben   Bill
     / \
 Cathy  Chris
```

## Step 2 - Add code to your editor

- Open your favorite text editor, such as [VSCode][vscode] or PowerShell ISE
- For each node in the above tree, we'll create a class for it. The sample code looks like below.
- Copy the code to your editor
- Save it. Say `MyProvider.psm1`.

  ```powershell
  using namespace Microsoft.PowerShell.SHiPS

  class Austin : SHiPSDirectory
  {
      Austin([string]$name): base($name)
      {
      }

      [object[]] GetChildItem()
      {
          $obj =  @()
          $obj += [Ben]::new();
          $obj += [Bill]::new();
          return $obj;
      }
  }

  class Bill : SHiPSLeaf
  {
      Bill () : base ("Bill")
      {
      }
  }

  class Ben : SHiPSDirectory
  {
      Ben () : base ("Ben")
      {
      }

      [object[]] GetChildItem()
      {
          $obj =  @()
          $obj += [Chris]::new();
          $obj += [Cathy]::new();
          return $obj;
      }
  }

  class Chris : SHiPSLeaf
  {
       Chris () : base ("Chris")
      {
      }
  }

  class Cathy : SHiPSLeaf
  {
      Cathy () : base ("Cathy")
      {
      }
  }

  ```

## Step 3 - Use MyProvider.psm1

- Import-Module SHiPS (See [Installing SHiPS][readme] for details)
- Create a PSDrive

  ```powershell

  new-psdrive -name Austin -psprovider SHiPS -root MyProvider#Austin

  cd Austin:
  PS Austin:\> dir
    Container: Microsoft.PowerShell.SHiPS\SHiPS::tt#Austin

  Mode  Name
  ----  ----
  +     Ben
  .     Bill

  ```

## Key takeaways from this example

- As MyProvider is built on top of the SHiPS, we need to include the namespace at the beginning
  ```powerShell
  using namespace Microsoft.PowerShell.SHiPS
  ```
- Each class as a navigation node needs inherits from `SHiPSDirectory` for a directory type, i.e., node contains child items.
- For leaf nodes, you can return any type. For the sake of output formatting, you may define a class inherits from `SHiPSLeaf`.

- As a root node, you need to define a constructor with node name as a parameter. For example,

  ```powershell
  Austin([string]$name): base($name)

  ```
- To support Get-ChildItem or dir, you need to implement the below method in your class.

  ```powershell
  [object[]] GetChildItem()

  ```
- When you create a PSDrive, the supported format is "module#type", e.g.,

  ```powershell

  new-psdrive -name Austin -psprovider SHiPS -root MyProvider#Austin
  ```

## More Samples

Wanna try more samples? Review the following:

- [FamilyTree][ft]
- [File System as a Recursion Example][fs]
- [Show Progress, and Other Attributes][sp]
- [Dynamic Parameters Example][ds]
- [C# Based Class][csharp]
- [Navigate Azure Resources][az]
- [Navigate CIM Classes and Namespaces][cim]

[vscode]: https://github.com/PowerShell/PowerShell/blob/master/docs/learning-powershell/using-vscode.md#editing-with-vs-code
[readme]: ../README.md#Installing-SHiPS
[ft]: ../samples/FamilyTree
[fs]: ../samples/FileSystem
[sp]: ../samples/ShowProgress
[csharp]: ../samples/FamilyTreeInCSharp
[ds]: ../samples/DynamicParameter
[az]: https://github.com/PowerShell/AzurePSDrive
[design]: ./Design.md
[api]: ./PublicAPIsAndMore.md
[cim]: https://github.com/PowerShell/CimPSDrive
