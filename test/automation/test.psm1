<#
   Assuming you have done clone. Now cd to SHiPS\test\automation folder. Try the following.

    Import-Module  ..\..\src\out\SHiPS\SHiPS
    Import-Module  .\test.psm1
    new-psdrive -name JJ -psprovider SHiPS -root Test#Root

    cd JJ:
    dir

#>

using namespace Microsoft.PowerShell.SHiPS
$script:PowerShellProcessName = if($IsCoreCLR) {'pwsh'} else{ 'PowerShell'}



[SHiPSProvider(UseCache=$false)]
class Root : SHiPSDirectory
{
    Root([string]$name): base($name)
    {
    }

    [object] GetChildItemDynamicParameters()
    {
        return [MyDynamicParameter]::new()
    }

    [object[]] GetChildItem()
    {
        $obj = @()
        $obj += [Austin]::new("Bill", $script:PowerShellProcessName);
        $obj += [Chris]::new("William");
        return $obj
    }
}

class Austin : SHiPSDirectory
{
    Hidden [string]$data = $null

    Austin([string]$name): base($name)
    {
    }

    Austin ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }


    [object] Funny()
    {
        return "BITS"
    }

    [object[]] GetChildItem()
    {
        #Write-warning "GetChildItem gets called."

        $obj =  @()
        # $nodeData gets set when the object gets created. Through this we pass around the data from parent node to child
        $processes = Get-Process $this.data
        foreach ($p in $processes) {
            $obj += [AlexLeaf]::new($p.Name);
            break;
        }

        # from function call
        $result = $this.Funny()
        $obj += [AlexLeaf]::new($result);

        # child class
        $obj += [Warning]::new("Warning");

        return $obj;
    }
}

class AlexLeaf : SHiPSLeaf
{
    AlexLeaf([string]$name): base($name)
    {
    }
}


class Warning : SHiPSDirectory
{
    Warning([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
       #Write-Warning ("Testing purpose. Don't panic!")
       return [Chris]::new("Chris");

    }
}


class DupTest: SHiPSDirectory
{
    DupTest([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()


        # child class
        $obj += [ChrisLeaf]::new("Chris");
        $obj += [Chris]::new("Chris");

        return $obj;
    }
}

[SHiPSProvider(UseCache=$false)]
class Chris : SHiPSDirectory
{
    Hidden [string]$data = $null

    Chris([string]$name): base($name)
    {
    }
    Chris ([string]$name, [string]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
        $obj += [ChrisLeaf]::new("Chris Jr.", "Chris's Son");
        $obj += [ChrisLeaf]::new("Chrisylin", "Chris's daughter1");
        $obj += [ChrisLeaf]::new("Chrisylinni", "Chris's daughter2");
        return $obj;
    }
}

class ChrisLeaf : SHiPSLeaf
{
    Hidden [string]$data = $null
    ChrisLeaf([string]$name): base($name)
    {
    }
    ChrisLeaf([string]$name, [string]$data): base($name)
    {
        $this.data=$data
    }
}

class Slash : SHiPSDirectory
{
    Hidden [string]$data = $null

    Slash([string]$name): base($name)
    {
    }

    Slash ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
        $obj = @()
        $obj += [Austin]::new("Bill",  $script:PowerShellProcessName);
        $obj += [Chris]::new("Will/iam", $null);
        return $obj
    }
}



class WithoutCacheTest : SHiPSDirectory
{
    Hidden [string]$data = $null

    WithoutCacheTest([string]$name): base($name)
    {
    }

    WithoutCacheTest ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
       Start-Sleep -Milliseconds 500
       return "hi"
    }
}

[SHiPSProvider(UseCache=$true)]
class WithCacheTest : SHiPSDirectory
{
    Hidden [string]$data = $null

    WithCacheTest([string]$name): base($name)
    {
    }

    WithCacheTest ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
       Start-Sleep -Milliseconds 500
       return "hi"
    }
}

[SHiPSProvider(UseCache=$true, BuiltinProgress=$false)]
class UseCacheTrueAndBuiltinProgressFalse : SHiPSDirectory
{
    Hidden [string]$data = $null

    UseCacheTrueAndBuiltinProgressFalse([string]$name): base($name)
    {
    }

    UseCacheTrueAndBuiltinProgressFalse ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
       return "hi"
    }
}

[SHiPSProvider(BuiltinProgress=$false)]
class WithBuiltinProgressFalseButMsg : SHiPSDirectory
{
    Hidden [string]$data = $null

    WithBuiltinProgressFalseButMsg([string]$name): base($name)
    {
    }

    WithBuiltinProgressFalseButMsg ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
       return "hi"
    }
}

[SHiPSProvider(BuiltinProgress=$true)]
class WithBuiltinProgressAndMsg : SHiPSDirectory
{
    Hidden [string]$data = $null

    WithBuiltinProgressAndMsg([string]$name): base($name)
    {
    }

    WithBuiltinProgressAndMsg ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
       return "hi"
    }
}


class ReturnEmpty : SHiPSDirectory
{
    Hidden [string]$data = $null

    ReturnEmpty([string]$name): base($name)
    {
    }

    ReturnEmpty ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
        return ""
    }
}

class ReturnNull : SHiPSDirectory
{
    Hidden [string]$data = $null

    ReturnNull([string]$name): base($name)
    {
    }

    ReturnNull ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }

    [object[]] GetChildItem()
    {
        return $null
    }
}



class MyDynamicParameter
{
    [Parameter()]
    [Switch]$SHiPSListAvailable

    [Parameter()]
    [string]$CityCapital

    [Parameter()]
    [string[]]$flowers

    [string]$NotDynamicParameter
}
class MyDynamicParameter2
{
    [Parameter()]
    [Switch]$SHiPSListAvailable2

    [Parameter()]
    [string]$CityCapital2

    [Parameter()]
    [string[]]$flowers2
}

[SHiPSProvider(UseCache=$false)]
class DynamicParameterTest : SHiPSDirectory
{
    DynamicParameterTest([string]$name): base($name)
    {
    }

    [object] GetChildItemDynamicParameters()
    {
        return [MyDynamicParameter]::new()
    }

    [object[]] GetChildItem()
    {

        $a = $this.ProviderContext.DynamicParameters -as [MyDynamicParameter]

        $obj =@()
        $obj+= [DynamicParameterTest2]::new("William")

        if($a.SHiPSListAvailable)
        {
            return "Hello DynamicParameterTest"
        }

        return $obj
    }
}

[SHiPSProvider(UseCache=$false)]
class DynamicParameterTest2 : SHiPSDirectory
{

    DynamicParameterTest2([string]$name): base($name)
    {
    }

    [object] GetChildItemDynamicParameters()
    {
        return [MyDynamicParameter2]::new()
    }

    [object[]] GetChildItem()
    {

        $a = $this.ProviderContext.DynamicParameters -as [MyDynamicParameter2]

        if($a.SHiPSListAvailable2)
        {
            return $a.flowers2
        }
        else
        {
            return "Hello DynamicParameterTest2"
        }
    }
}

[SHiPSProvider(UseCache=$true)]
class DynamicParameterTestWithCache : SHiPSDirectory
{
    DynamicParameterTestWithCache([string]$name): base($name)
    {
    }

    [object] GetChildItemDynamicParameters()
    {
        return [MyDynamicParameter]::new()
    }

    [object[]] GetChildItem()
    {
        #Write-Warning "Calling GetChildItem DynamicParameterTest - WithCache"
        $a = $this.ProviderContext.DynamicParameters -as [MyDynamicParameter]

        $obj =@()
        $obj+= [DynamicParameterTest2]::new("William")

        if($a.SHiPSListAvailable)
        {
            return "Hello DynamicParameterTestWithCache"
        }

        return $obj
    }
}


[SHiPSProvider(UseCache=$false)]
class DynamicParameterTestWithNoCache : SHiPSDirectory
{

    DynamicParameterTestWithNoCache([string]$name): base($name)
    {
    }

    [object] GetChildItemDynamicParameters()
    {
        return [MyDynamicParameter2]::new()
    }

    [object[]] GetChildItem()
    {
        #Write-Warning "Calling GetChildItem DynamicParameterTest - WithNoCache"

        $a = $this.ProviderContext.DynamicParameters -as [MyDynamicParameter2]

        if($a.SHiPSListAvailable2)
        {
            return "Hello DynamicParameterTestWithNoCache"
        }

        # calling class with cache
        return [DynamicParameterTestWithCache]::new("William")
    }
}



function GenerateDynamicParameters
{

    $paramDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

    $ageAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ageAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ageAttributeCollection.Add($ageAttribute)


    $cityAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $cityAttribute = New-Object System.Management.Automation.ParameterAttribute
    $cityAttributeCollection.Add($cityAttribute)

    $listAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $listAttribute = New-Object System.Management.Automation.ParameterAttribute
    $listAttribute.ValueFromPipeline = $true
    $listAttributeCollection.Add($listAttribute)

    $ageParam = New-Object System.Management.Automation.RuntimeDefinedParameter('age', [System.Int16], $ageAttributeCollection)
    $cityParam = New-Object System.Management.Automation.RuntimeDefinedParameter('city', [string], $cityAttributeCollection)
    $listParam = New-Object System.Management.Automation.RuntimeDefinedParameter('list', [switch], $listAttributeCollection)

    $paramDictionary.Add('age', $ageParam)
    $paramDictionary.Add('city', $cityParam)
    $paramDictionary.Add('list', $listParam)

    return $paramDictionary

}

class DynamicParameterKeyValuePairTraditional : SHiPSDirectory
{

    DynamicParameterKeyValuePairTraditional([string]$name): base($name)
    {
    }

    [System.Management.Automation.RuntimeDefinedParameterDictionary] GetChildItemDynamicParameters()
    {
        return GenerateDynamicParameters
    }

    [object[]] GetChildItem()
    {
        $dp = $this.ProviderContext.DynamicParameters  -as [System.Management.Automation.RuntimeDefinedParameterDictionary]

        $msg ="DynamicParameterKeyValuePairTraditional:"

        if($dp -and $dp.Containskey("list") -and $dp['list'].IsSet)
        {
            $msg += "true"
        }
        if($dp -and $dp.Containskey("age") -and $dp['age'].IsSet)
        {
            $msg += $($dp['age']).Value

        }
        if($dp -and $dp.Containskey("city") -and $dp['city'].IsSet)
        {
            $msg += $($dp['city']).Value

        }

        return $msg
    }
}



class FilterTest : SHiPSDirectory
{

    FilterTest([string]$name): base($name)
    {
    }


    [object[]] GetChildItem()
    {

        if($this.ProviderContext.Filter)
        {
            if($this.ProviderContext.Recurse)
            {
                return $this.ProviderContext.Filter+"Recurse"
            }

            return $this.ProviderContext.Filter
        }
        else
        {
            return "hi"
        }
    }
}



class SHiPSLeafTest: SHiPSDirectory
{
    SHiPSLeafTest([string]$name): base($name){}

    [object[]] GetChildItem()
    {
        return [Birch]::new()
    }

}

class Birch : SHiPSLeaf
{
    Birch() : base($this.GetType())
    {
    }
}

[SHiPSProvider(UseCache=$true)]
class Home : SHiPSDirectory
{
    Hidden [object]$data = $null

    Home([string]$name): base($name)
    {
    }

    Home ([string]$name, [object]$data) : base ($name)
    {
        $this.data = $data
    }


    [object[]] GetChildItemImpl([string] $data)
    {
       $obj =  @()

       Write-Verbose $data

       dir $data | ForEach-Object {

                if($_.PSIsContainer)
                {
                    $obj+=[Home]::new($_.Name, $_.FullName)
                }
                else
                {
                    $obj+=[CLeaf]::new($_.Name)
                }

        }

        return $obj
     }

    [object[]] GetChildItem()
    {

        $obj =  @()

        if($this.data)
        {
               return $this.GetChildItemImpl($this.data)
        }
        else
        {
              $driveName = $this.GetType().Name
              Write-Verbose "Operating on $driveName"

              return $this.GetChildItemImpl("~")
        }

        return $obj;
    }
}

class CLeaf : SHiPSLeaf
{
    CLeaf([string]$name): base($name)
    {
    }
}


[SHiPSProvider(UseCache=$true)]
class ErrorCaseWriteError : SHiPSDirectory
{

    ErrorCaseWriteError([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
       Write-Error "Hi there this is an expected error!"
       return $null
        #throw "Hi there this is an expected error!"
    }
}

[SHiPSProvider(UseCache=$true)]
class ErrorCaseThrow : SHiPSDirectory
{

    ErrorCaseThrow([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
       Write-Error "Hi there this is an expected error!"
       return $null
        #throw "Hi there this is an expected error!"
    }
}
[SHiPSProvider(UseCache=$true)]
class ErrorCase : SHiPSDirectory
{

    ErrorCase([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
        $obj = @()
        $obj += [ErrorCaseWriteError]::new('WriteError')
        $obj += [ErrorCaseThrow]::new('ErrorThrow')
        $obj += [SHiPSLeafTest]::new('SHiPSLeafTest')
        return $obj
    }
}
