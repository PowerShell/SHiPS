# SHiPS Public APIs, Parameters, and More

## SHiPS Namespace - Microsoft.PowerShell.SHiPS

You can follow the format below to reference SHiPS namespace.

  `PowerShell`

  ```powershell
  using namespace Microsoft.PowerShell.SHiPS
  ```

  `C#`

  ``` C#
  using Microsoft.PowerShell.SHiPS
  ```

## SHiPS Public Types

- SHiPSDirectory

    A base class for defining a type that represents a node object which contains any child items.

- SHiPSLeaf

    Defines a type that represents a leaf node.

    > NOTE:
Above types have a constructor with one string parameter, which represents as a node name.
The name is mandatory and must be unique under the same parent node - 'directory'.
With that,  in PowerShell, you can pass in a node name argument as follows.

    ```powershell
    Employee([string]$name) : base ($name)
    {
    }
    ```

## Public APIs

- [object[]] GetChildItem()

  Returns a list of objects while a user types  `Get-ChildItem (or dir)`

- [object] GetChildItemDynamicParameters()

    Defines Get-ChildItem dynamic parameters for `Get-ChildItem`

- [object] SetContent([string]$content, [string]$path)
  Sets the content string text in $content to the path node specified in $path.

- [string] GetContent()
  Gets the content text from the current node object.

## Public Attributes

SHiPS contains the following attributes:

- UseCache

    By default, SHiPS does not cache any child items in to memory.
    However for better navigation user experience like a situation where requires some time to retrieve data from remote data store, an author can decide to set UseCache to true to cache data returned from Get-ChildItem to memory in the current PowerShell session.
    The cache data will be deleted once the PowerShell session is closed.

    ```powershell
    [SHiPSProvider(UseCache=$true)]
    ```

    To refresh data, a user can type `-force`

- BuiltinProgress

    By default, SHiPS shows the progress. However an module author can decide to not enable the builtin progress by setting BuiltinProgress to false.

    ```powershell
    [SHiPSProvider(BuiltinProgress=$false)]
    ```

## Public Parameters

The following properties are exposed to SHiPS' derived classes as protected members.

- `Name`: provider's node name

  As above mentioned, you can pass in the node name to base class such as

  ```powershell
  Compute([string]$name) : base ($name)
  {
  }
  ```
  or

  ```powershell
  Compute ([string]$name)
  {
     $this.Name = $name;
  }
  ```

- `ProviderContext`: provider's data

  - `DynamicParameters`
  As above mentioned, an author can define dynamic parameters for their providers.
  To process whether a user uses these dyanmic parameters, author can access them via the following mechanism.

    ```powershell
    $this.ProviderContext.DynamicParameters
    ```

    For more usages, see [dynamic parameters example][ds].

  - `Filter`
  Gets the filter property that was supplied by a user.
  `-Filter` is a builtin parameter in the Get-ChildItem cmdlet.
  Considering there are times you may want to process a 'server-side' filtering for better user experience, SHiPS exposed Filter query string to the module. For example you can access it as follows:

    ```powershell
    $queryString = $this.ProviderContext.Filter
    ```

  - `Recurse`: Gets the recurse property.
  It's possibly you want to perform filtering in your side server and instruct the server whether filtering recursively or not. In that you can check whether a user specifies -Recurse.

    ```powershell
    if(-not $this.ProviderContext.Recurse)
    ```
  - `Force` Gets the force property.

    ```powershell
    if(-not $this.ProviderContext.Force)
    ```

[ds]:../samples/DynamicParameter
