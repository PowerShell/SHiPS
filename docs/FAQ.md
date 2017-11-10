# Q & A

## General

- Why am I hitting "Cannot create an instance of type... " error?

  Please check the following:

  - The syntax below is to create a PowerShell drive:

  ```powershell
    new-psdrive -name yourdrivename -psprovider SHiPS -root `yourmodule#yourtype`
  ```
  - The module and type exists.
  - Open the module, make sure the constructor with one name  parameter is define and set properly.
  - Run `Get-Module` to ensure no module name collision.
  - Avoid your class type as a root node object conflicting with any .Net type.

    It's observed that loading .Net type takes precedence of user defined.
    This is by design by PowerShell.
    In order to load the module and type correctly, root type must be define the module that specified in the `-root` while a user new-psdrive.
    In order word, a type as root node can not be defined in any referenced module while the type has the same name as .net builtin type.

- I noticed my provider is caching data from Get-ChildItem, How can I refresh data?

    try:

    ```powershell
    dir -force
    ```

- Why am I asked constantly to login to Azure even right after I just did it for your sample Azure provider?

    It is possibly caused by some of the Azure modules versions mismatch.
    You can remove all Azure modules and reinstall them on your machine via `Uninstall-Module` and `Install-Module`.

- Apart from `Get-Item` and `Get-ChildItem`, which other provider cmdlets are supported?

  Others are not supported yet.

## Authoring Experience

- Any examples to get started by writing SHiPS based provider?

  Yes. See [Getting started doc][gs].

- Why I can `cd` to a leaf node sometimes?

  - Make sure your type is not inherited from SHiPSDirectory, which is designed for a node which contains child items.
  - Make sure child item name is unique. Like a FileSystem, no file or directory names are the same under the same directory.
  The same convention applies to SHiPS and its providers.

- Can node name contains slash?

    No. `slash` is considered as path delimiter in the PowerShell provider scope.
    However, in some cases ,like Azure service, name may contains slashes.
    In that case, it's recommended that an author should consider split the name by slash and make them as its child items.

  >NOTE: SHiPS automatically replaces the slash with '-' to avoid any navigation behavior issues.

- Why my module is not get reloaded, after I modified it and ran `Install-Module -Force` on it?

  A PowerShell class under the hood is the .Net based type and .Net type cannot be reloaded.
  That's why you observed that a module defining a class does not get reloaded if it is already loaded in your current session.

  The workaround is to start a new PowerShell process.

- When to use cache?

  By default, SHiPS does not cache any items in memory.
  However if it takes time for retrieving data, you may choose to use caching.
  The syntax for caching is as follows:

   ```powershell
  [SHiPSProvider(UseCache=$true)]
  class C : SHiPSDirectory
  {
    # add code here
  }
  ```

  Please note that SHiPS follows tree structure to cache the child items.
  Thus a child node can be cached only if its parent nodes (up to root) have the `UseCache` attribute set to true.

  Also see [here][attribute] for SHiPS's attributes.

[attribute]: ./PublicAPIsAndMore.md
[gs]: ./README.md
