# Sample Provider - FamilyTree Written in C# #

As you may have tried the [FamilyTree][ft] written in PowerShell class.
It seems to be straight forward. And in some cases, you may want to write a class in C#.
Following example shows you how to build a C# class library and to be loaded into SHiPS.

## Step 1 - Get SHiPS

- Follow the instruction [here][readme] to compile SHiPS.
  - if you build the SHiPS code from Visual Studio,  `Microsoft.PowerShell.SHiPS.dll` will be generated under SHiPS\test\ folder
  - if you using dotnet cli build, the `Microsoft.PowerShell.SHiPS.dll` can be found under SHiPS\src\out\SHiPS
  - future: you can `Install-Module SHiPS` to install the latest version of SHiPS.

## Step 2 - Create FamilyTreeInCSharp Assembly

- Open your Visual Studio or your favorite editor, copy the following code, save it as FamilyTreeInCSharp.cs
- Add a reference to Microsoft.PowerShell.SHiPS.dll
- Compile it. An assembly, FamilyTreeInCSharp.dll, will be generated.

``` C#
using System.Collections.Generic;
using Microsoft.PowerShell.SHiPS;

namespace FamilyTreeInCSharp
{

    /// <summary>
    /// A class defines the family tree root.
    /// </summary>
    public class Root : SHiPSDirectory
    {
        public Root(string name) : base(name)
        {
        }

        public override object[] GetChildItem()
        {
            return new List<object>
            {
                new Erin("Erin"),
                new Ethen("Ethen")
            }.ToArray();
        }
    }

    /// <summary>
    /// Defines a node with children.
    /// </summary>
    public class Erin : SHiPSDirectory
    {
        public Erin(string name) : base(name)
        {
        }

        public override object[] GetChildItem()
        {
             return new object[] { new Mike("Erin's kid")};
        }
    }

    /// <summary>
    /// Defines a node with children.
    /// </summary>
    public class Mike : SHiPSDirectory
    {
        public Mike(string name) : base(name)
        {
        }

        public override object[] GetChildItem()
        {
            return new object[] {"Hello I am Mike."};
        }
    }

    /// <summary>
    /// Defines a leaf node.
    /// </summary>
    public class Ethen : SHiPSLeaf
    {
        public Ethen(string name) : base(name)
        {
        }
    }
}

```

## Step 3 - Create A PowerShell Module

```powershell
New-ModuleManifest -path .\FamilyTreeInCSharp.psd1
```

Open the FamilyTreeInCSharp.psd1 in your favorite editor, edit the following line in the FamilyTreeInCSharp.psd1.
Since the business logic is implemented in FamilyTreeInCSharp.dll, set RootModule to FamilyTreeInCSharp.dll.

```powershell
RootModule = 'FamilyTreeInCSharp.dll'
```

Save the file.

## Step 4 - Import Module

- If you build the code using dotnet Cli
  - cd to your git clone folder
  - cd SHiPS\src\out
  -  Import-Module SHiPS

- If you build via Visual Studio
  - cd to youclonefolder\SHiPS\Test
  -  Import-Module SHiPS

- If you installed the module from PowerShellGallery, simply Import-Module SHiPS

>NOTE:
If you build the assembly, you may need to ignore strong name signing on Windows.

## Step 5 - Create a PowerShell Drive

```powershell
Import-Module  .\FamilyTreeInCSharp.psd1
new-psdrive -name ft -psprovider SHiPS -root 'FamilyTreeInCSharp#FamilyTreeInCSharp.Root'

cd ft:
dir
```

Output:

```powershell

PS C:\> new-psdrive -name ft -psprovider SHiPS -root 'FamilyTreeInCSharp#FamilyTreeInCSharp.Root'

Name           Used (GB)     Free (GB) Provider      Root
----           ---------     --------- --------      ----
ft                                     SHiPS         FamilyTreeInCSharp#FamilyTreeInC...

PS ft:\> dir
    Container: Microsoft.PowerShell.SHiPS\SHiPS::FamilyTreeInCSharp#FamilyTreeInCSharp.Root
Mode  Name
----  ----
+     Erin
.     Ethen

PS ft:\> cd Erin
PS ft:\Erin> dir
    Container: Microsoft.PowerShell.SHiPS\SHiPS::FamilyTreeInCSharp#FamilyTreeInCSharp.Root\Erin

Mode  Name
----  ----
+     Erin's kid

PS ft:\Erin> cd '.\Erin''s kid\'
PS ft:\Erin\Erin's kid> dir
Hello I am Mike.

```

[ft]: ../FamilyTree
[readme]:../../README.md
