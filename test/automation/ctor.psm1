<# 
    Import-Module  E:\azure\PSCloudConsole\test\ctor.psm1 -force
    $mod=get-module ctor
    &($mod){[Austin]::new('hello')}

    
    Import-Module  ..\SHiPS.psd1                        
    Import-Module  .\ctor.psm1
    new-psdrive -name cc -psprovider SHiPS -root ctor#Austin
    cd cc:
    dir

#>

using namespace Microsoft.PowerShell.SHiPS
$script:PowerShellProcessName = if($IsCoreCLR) {'pwsh'} else{ 'PowerShell'}


# collision with .Net - should work
class Environment : SHiPSDirectory
{

    Environment() 
    {        
        $this.Name = $this.GetType()
    } 

    Environment([string]$name) 
    {
        $this.Name = $name
    }
  
    
    [object[]] GetChildItem()
    { 
        return [Environment]::new()
    }
 }


class RegularClass
{

    RegularClass()
    {

    } 
    RegularClass([string]$name)
    {
    }
  
    
    [object[]] GetChildItem()
    { 
        return [Belly]::new()
    }
 }


class ClassWithNoRootNameCtor : SHiPSDirectory
{

    ClassWithNoRootNameCtor()
    {

    } 
    
    [object[]] GetChildItem()
    { 
        return [Belly]::new()
    }
 }

class ClassWithEmptyRootName : SHiPSDirectory
{

    ClassWithEmptyRootName()
    {

    } 
    
    ClassWithEmptyRootName([string]$name)
    {
        $this.Name = ""
    }
    [object[]] GetChildItem()
    { 
        return [Belly]::new()
    }
 }

class ClassWithNoneRootNodeType : SHiPSDirectory
{

    ClassWithNoneRootNodeType()
    {

    } 
    
    ClassWithNoneRootNodeType([string]$name)
    {
        $this.Name = $name
    }
    [object[]] GetChildItem()
    { 
        return [Belly]::new()
    }
 }

class ClassWithWrongNodeType : SHiPSDirectory
{

    ClassWithWrongNodeType()
    {

    } 
    
    ClassWithWrongNodeType([string]$name)
    {
        $this.Name = $name
    }
    [object[]] GetChildItem()
    { 
        return [Belly]::new()
    }
 }

class ClassWithNullName : SHiPSDirectory
{

    ClassWithNullName()
    {
        #$this.Name = "test"
    } 
    
    ClassWithNullName([string]$name)
    {
        $this.Name = $name
    }
    [object[]] GetChildItem()
    { 
        return [ChildClassWithNullName]::new()
    }
 }

class ChildClassWithNullName : ClassWithNullName
{
    [object[]] GetChildItem()
    { 
        write-Warning("hello world")   
        return get-process $script:PowerShellProcessName
    }
}

class ClassInheritsFromShipsLeaf : SHiPSLeaf
{

    ClassInheritsFromShipsLeaf()
    {
        $this.Name = "test"
    } 
    
    ClassInheritsFromShipsLeaf([string]$name)
    {
        $this.Name = $name
    }
    [object[]] GetChildItem()
    { 
        return [ClassInheritsFromShipsLeaf]::new()
    }
 }


class Austin : Microsoft.PowerShell.SHiPS.SHiPSDirectory
{

    Austin()
    {
        $this.Name = $this.GetType()
    } 
    Austin([string]$name)
    {
        $this.Name = $name       
    }
  
    
    [object[]] GetChildItem()
    { 
        #return [Belly]::new()
        return [Belly]::new("belly")
    }
 }


class Belly :Austin
{
    Belly([string]$name) : base($name)
    {
    }

    [object[]] GetChildItem()
    { 
        write-Warninggg("hello world")   
        return get-process $script:PowerShellProcessName
    }
}

