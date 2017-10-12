# Q & A


## General

`1.`  Why am I hitting "Cannot create an instance of type... " error?

Please check the following:

  - The syntax below is to create a PowerShell drive:

      new-psdrive -name yourdrivename -psprovider SHiPS -root `yourmodule#yourtype`

  - The module and type exists.
  - Open the module, make sure the constructor with one name  parameter is define and set properly.
  - Run `Get-Module` to ensure no module name collision.
  - Avoid your class type as a root node object conflicting with any .Net type.

    It's observed that loading .Net type takes precedence of user defined.
    This is by design by PowerShell.
    In order to load the module and type correctly, root type must be define the module that specified in the `-root` while a user new-psdrive.
    In order word, a type as root node can not be defined in any referenced module while the type has the same name as .net builtin type.


`2.` I noticed my provider is caching data from Get-ChildItem, How can I refresh data?

    try:

        dir -force


`3.`	Why constantly asks me Login-AzureRmAccount even right after I just did it for your sample Azure provider?

    It is possibly caused by some of the Azure modules versions mismatch.
    You can remove all Azure modules and reinstall them on your machine via Uninstall-Module and Install-Module.

`4.` Other than Get-Item, Get-ChildItem, how about the rest of other provider cmdlets?

  Not supported yet.

## Authoring Experience


`1.` Any examples to get started by writing SHiPS based provider?

  Yes. See [Getting started doc][gs].

`2.` Why I can `cd` to a leaf node sometimes?

   - Make sure your type is not inherited from SHiPSDirectory, which is designed for a node which contains child items.
   - Make sure child item name is unique. Like a FileSystem, no file or directory names are the same under the same directory.
   The same convention applies to SHiPS and its providers.

`3.` Can node name contains slash?

    No. `slash` is considered as delimiter of path in the PowerShell provider scope.  
    However, some cases like Azure service name may contains slashes. In that case,
    it's recommended that an author should consider split the name by slash and make them as its child items.

  >`note`: SHiPS automatically replaces the slash with '-' to avoid awry navigation behavior.

`4.` Why my module does not get reloaded after I modified and ran install-module -force it?

  A PowerShell class under the hood is the .Net based type. And .Net type cannot be reloaded.
  That's why you observed that a module defining a class does not get reloaded if it is already loaded in your current session.

  The workaround is to re-open a PowerShell console.

`5.` When to use cache?

  SHiPS does not cache any child items into memory by default.
  However if it takes time for retrieving data, you may choose to use caching.
  Below is the syntax.  

   ``` PowerShell

  [SHiPSProvider(UseCache=$true)]
  class C : SHiPSDirectory
  {
    # add code here
  }

  ```

  Please note that SHiPS follows tree structure to cache the child items. Thus a child node can be cached
  only if its parent nodes (up to root) have the UseCache attribute is set to true.


 Also see [here][attribute] for SHiPS's attributes.

[attribute]: ./SHiPSPublicAPIsMore.md
[gs]: ./README.md
