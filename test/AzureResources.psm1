<#
    Note:

    This is just an example of using SHiPS provider to navigate Azure resources.
    The code below is for demo only. They are not optimized.

    Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

    Import-Module  SHiPS
    Import-Module  .\test\AzureResources.psm1

    new-psdrive -name ar -psprovider SHiPS -root AzureResources#Resources
    cd ar:

#>


using namespace Microsoft.PowerShell.SHiPS

class DataHolder
{
    [object[]]$resource;
    [string]$parentName;
    [int]$tokenIndex = 0;
    [char]$sprtChar = '.';
    [object]$resourceCurrent
}
Enum RMOperation
{
    None = 0;
    Add = 1;
    Remove = 2;
}

[SHiPSProvider(UseCache=$true)]
class Resources : SHiPSDirectory
{
    Hidden [DataHolder]$data = $null
    [object]$Property = $null

    Resources(): base($this.GetType())
    {
    }

    #define this constructor so that it can be a root node such as for testing purpose
    Resources([string]$name): base($name)
    {
    }

    Resources ([string]$name, [DataHolder]$data, [object] $property) : base ($name)
    {
        $this.data = $data
        $this.Property = $property
    }

    [object] UpdateAzureRmObject([string]$command, [hashtable]$Parameters, [RMOperation]$operation)
    {

        if($Parameters){
            $azureObjects = &$command @parameters
        }
        else{
            $azureObjects = &$command
        }

        switch ($Operation)
        {
            'Remove' {
                $azureObjects.Foreach{
                    $oldTypeNames = $_.pstypeNames
                    $_.pstypenames[0] = $oldTypeNames[-2]
                    $_.pstypenames[1] = $oldTypeNames[-1]
                    $_.pstypeNames[2] = 'System.Object'
                    $_.pstypenames.RemoveAt(3)
                }
            }
            'Add' {
                $azureObjects.Foreach{
                    $_.pstypenames.Insert(1,'Microsoft.Azure.Resource')
                }
            }
        }
        return $azureObjects
    }


    [object] FindChildren ([DataHolder] $context)
    {
        $resourceTypeName = $context.resource | ForEach-Object {($_.ResourceType -split '/')[$context.tokenIndex]} | Sort-Object -Unique
        if(-not $resourceTypeName )
        {
            throw "this should never happen!!!"
        }

        $obj =  New-Object System.Collections.ArrayList

        $resourceTypeName | Foreach-Object {

            if($context.parentName){
                $ShortName = $_
                $resourceName = $context.ParentName,$ShortName -join $context.sprtChar
            }
            else{
                $resourceName = $_
                $ShortName = $resourceName.Split($context.sprtChar)[$context.tokenIndex+1]
            }

            [bool]$IsVariableSet = $false

            Write-Verbose -message ("ShortName={0}" -f $ShortName)
            Write-Verbose -message ("tokenIndex={0}" -f ($context.tokenIndex))

            $resourceCurrent = $context.resource | Where-Object{$_.ResourceType -eq $resourceName}
            $resourceChild   = $context.resource | Where-Object{$_.ResourceType.Startswith("$resourceName/")}

            # prep for its child node
            $data = [DataHolder]::new()

            $data.resource   = $resourceChild
            $data.parentName = $resourceName
            $data.tokenIndex = $context.tokenIndex+1
            $data.sprtChar   = '/'
            $data.resourceCurrent = $resourceCurrent


            if($resourceChild)
            {
                $kid = [Resources]::new($ShortName, $data, $data.resourceCurrent)
            }
            else
            {
                $kid = [ResourcesLeaf]::new($ShortName, $data.resourceCurrent)
            }

            $obj.add($kid);  #$resourcechild is its child node

        }

        return $obj;
    }

    # called by SHiPS while a user does 'dir'
    [object[]] GetChildItem()
    {
        if($this.data)
        {
            return $this.FindChildren($this.data);
        }
        else
        {
            $resourceResult = $this.UpdateAzureRmObject('AzureRM.Resources\Get-AzureRmResource', $null, [RMOperation]::Remove);

            $d = [DataHolder]::new()
            $d.resource = $resourceResult
            return $this.FindChildren($d);
        }
    }
}


class ResourcesLeaf : SHiPSLeaf
{

    [object]$Property = $null

    ResourcesLeaf ([string]$name, [object] $property) : base ($name)
    {
        $this.Property = $property
    }
}