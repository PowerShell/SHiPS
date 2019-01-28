# Sample Provider - How to use dynamic parameters

## Step 1- Install SHiPS

Follow the instructions [in here][readme] to download and install the SHiPS.

## Step 2 - Model the system

Let's say we want to model a tree as follows:

```powershell

   Nature
         - Plant
               - Birch
               - Maple
               - Oak
         - Creature
               - Cat
               - Dog
                   - Husky
                   - Bulldog
```

## Step 3 - Review the sample code

See the [DynamicParameterSample module][ds].

## Step 4 - Try it out

```powershell
   Import-Module  SHiPS
   Import-Module  .\samples\DynamicParameter

   new-psdrive -name n -psprovider SHiPS -root DynamicParameter#Nature
   cd n:
   dir

   dir -Type Plant

   dir -Type Creature

   dir -Filter p* -recurse

<#
The output looks like the following.
PS n:\>dir

Mode  Name
----  ----
+     Plant
+     Creature

PS n:\>dir -Type Plant

    Container: Microsoft.PowerShell.SHiPS\SHiPS::DynamicParameterSample#Nature

Mode  Name
----  ----
+     Plant

PS n:\> dir -Type Creature

    Container: Microsoft.PowerShell.SHiPS\SHiPS::DynamicParameterSample#Nature

Mode  Name
----  ----
+     Creature

PS n:\> cd .\Creature\
PS n:\Creature>dir -Filter p* -recurse
Pointer
Poodle
Pug
Pomeranian
Pui
Pumi

#>
```

## Key takeaways from this example

- Other than built-in parameters in Get-ChildItem, author can add dynamic parameters
  - Define it as a class
  - Add `[Parameter()]`  in a dynamic parameter. That's it.

- Define `[object] GetChildItemDynamicParameters()` method and return a instance of the class that defines your dynamic parameters
- To get whether a user specifies any dynamic parameters, you can use `$this.ProviderContext.DynamicParameters`.
- `Filter` is a built-in parameter.

Sometimes you want to have server-side search for better user experience,
you can get the query string via `$this.ProviderContext.Filter`.
The value of Filter here is ``foobar*`` from -Filter ``foobar*``

[readme]: ../../README.md#Installing-SHiPS
[ds]:DynamicParameter.psm1
