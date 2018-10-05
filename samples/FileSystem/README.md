# Sample Provider - Modeling Windows File System

## Step 1- Install SHiPS

Follow the instructions [in here][readme] to download and install the SHiPS.

## Step 2 - Review the sample code

See the [FileSystem module][fs].

## Step 3 - Try it out

```powershell
Import-Module  SHiPS
Import-Module .\samples\FileSystem
New-PSDrive -Name FS -PSProvider SHiPS -Root FileSystem#Home
cd FS:
dir
```

## Key takeaways from this example

- Unlike  the [FamilyTree][ft], a tree structure, the FileSystem follows a recursion model from root to leaf.
- We used a data field, `Hidden [object]$data`  to pass down the context from parent node to child.

[readme]: ../../README.md#Installing-SHiPS
[fs]: FileSystem.psm1
[ft]: ../FamilyTree
