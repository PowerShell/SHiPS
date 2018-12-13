<#
    Note:

    1. This is just an example of using SHiPS provider to navigate Azure resources.
        The code below is for demo only. They are not optimized.

    2. You may need to install the following PowerShell modules to run this example.
        Install-Module AzureRM.profile
        Install-Module AzureRM.Resources
        install-module AzureRM.Storage
        Install-Module -Name AzureRM.Sql
        Install-Module -Name AzureRM.Websites
        Install-Module -Name AzureRM.Network
        install-Module -Name AzureRM.Cdn


    Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

    Install-SHiPS
    Import-Module  SHiPS
    Import-Module  .\test\AzureResourceNavigation.psm1
    new-psdrive -name az -psprovider SHiPS -root AzureResourceNavigation#Azure

    cd az:
    dir

#>


using namespace Microsoft.PowerShell.SHiPS
using Module .\AzureResources.psm1

[SHiPSProvider(UseCache=$true)]
class Azure : SHiPSDirectory
{
    Azure([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
         (AzureRM.profile\Get-AzureRmSubscription).ForEach{
            $obj +=  [Subscription]::new($_.Name, $_.Id);
        }
        return $obj;
    }

}

class Subscription : Azure
{
    Hidden [object]$data = $null

    Subscription ([string]$name, [object]$data) : base ($name)
    {
        $this.data = $data
    }

    [object[]] GetChildItem()
    {
        AzureRM.profile\Select-AzureRmSubscription -SubscriptionId $this.data | out-null
        $obj =  @()
        $obj+=[Resources]::new();
        $obj+=[Compute]::new();
        $obj+=[ResourceGroups]([RGImpl]::RGInstance());
        $obj+=[Networking]::new();
        $obj+=[Storage]::new();
        return $obj;
    }
 }

class RGImpl
{
    Hidden static [object[]] $childitems = $null
    Hidden static [ResourceGroups] $instance = [ResourceGroups]::new();

    # Explicit static constructor to tell C# compiler
    # not to mark type as beforefieldinit
    static RGImpl()
    {
    }

    RGImpl()
    {
    }

    static [ResourceGroups] RGInstance()
    {        
        return [RGImpl]::instance    
    }

    static [object[]] FindChildItem([bool]$force)
    {
        if(-not [RGImpl]::childitems -or $force)
        {
            Write-Verbose "childitems = [RGImpl]::childitems; force=$force"

            $obj =  @()
            (AzureRM.Resources\Get-AzureRmResourceGroup).ForEach{
                $name=$_.ResourceGroupName
                $obj+=[ResourcesName]::new($($name), $name, $_);
            }

            [RGImpl]::childitems = $obj
        }

        return  [RGImpl]::childitems
    }
}

class ResourceGroups : Azure
{
    ResourceGroups(): base($this.GetType())
    {
    }

    ResourceGroups([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
        return [RGImpl]::FindChildItem($this.ProviderContext.Force)
    }
}

class ResourcesName : Azure
{
    [object]$Property = $null
    Hidden [object]$data = $null


    ResourcesName ([string]$name) : base ($name)
    {
    }
    ResourcesName ([string]$name, [object] $data, [object] $property) : base ($name)
    {
        $this.data = $data
        $this.Property = $property
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
        $resource = AzureRM.Resources\Find-AzureRmResource -ResourceGroupName $this.data
        $resources=[object[]]$resource
        return $resource
    }
}

#install-module AzureRM.Cdn
class Networking : Azure
{
    Hidden [object]$data = $null

    Networking(): base($this.GetType())
    {
    }

    Networking([string]$name): base($name)
    {
    }

    Networking ([string]$name, [object]$data) : base ($name)
    {
        $this.data = $data
    }


    [object[]] VN ()
    {
       $obj =  @()
        (AzureRM.Network\Get-AzureRmVirtualNetwork).Foreach{
            $obj+= [NetworkingLeaf]::new($_.Name, $_)
        }

        return $obj
    }

    [object[]] CDN ()
    {
        $obj =  @()
        (AzureRM.Cdn\Get-AzureRmCdnProfile).Foreach{
            $obj+= [NetworkingLeaf]::new($_.Name, $_)
        }

        return $obj
    }

    [object[]] IpAddress ()
    {
        $obj =  @()
        (AzureRM.Network\Get-AzureRmPublicIpAddress).Foreach{
            $obj+= [NetworkingLeaf]::new($_.Name, $_)
        }

        return $obj
    }

    [object[]] NetworkInterface ()
    {
        $obj =  @()
        (AzureRM.Network\Get-AzureRmNetworkInterface).Foreach{
            $obj+= [NetworkingLeaf]::new($_.Name, $_)
        }

        return $obj
    }

    [object[]] GetChildItem()
    {
        if($this.data)
        {
            switch ($this.data)
            {
             "VN"  {  return $this.VN();}
             "CDN"  {  return $this.CDN();}
             "IpAddress"  {  return $this.IpAddress();}
             "NetworkInterface"  {  return $this.NetworkInterface();}
            }

            return null;
        }
        else
        {
            $obj =  @()

                $obj+= [Networking]::new("Virtual Network", "VN")
                $obj+= [Networking]::new("CDN Profiles", "CDN")
                $obj+= [Networking]::new("Public IPAddress", "IpAddress")
                $obj+= [Networking]::new("Network interface", "NetworkInterface")

            return $obj
        }
    }
}

class NetworkingLeaf : SHiPSLeaf
{
    [object]$Property = $null
    NetworkingLeaf ([string]$name, [object]$property) : base ($name)
    {
        $this.Property = $property
    }

}

#install-Module AzureRM.Storage
class Storage : Azure
{
    Storage(): base($this.GetType())
    {
    }
    Storage([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
         return [StorageAccount]::new()
    }
}

class StorageAccount : Azure
{
    StorageAccount(): base($this.GetType())
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
        (AzureRM.Storage\Get-AzureRmStorageAccount).Foreach{
             $obj += [StorageContainer]::new($($_.StorageAccountName), $_, $_)
            }

        return $obj
    }
}

class StorageContainer : Azure
{
    Hidden [object]$data = $null
    [object]$Property = $null

    StorageContainer ([string]$name, [object]$data, [object]$property) : base ($name)
    {
        $this.data = $data
        $this.Property = $property
    }


    [object[]] GetChildItem()
    {
        $obj =  @()

        $containers = Get-AzureStorageContainer -Context $this.data.Context
        $containers.Foreach{
            $blobs = Get-AzureStorageBlob -Context $_.Context -Container $_.Name

                $obj+= [Blob]::new($($_.Name), $_, $blobs);
            }

        return $obj
    }
}

class Blob : Azure
{
    [object]$Property = $null
    Hidden [object]$data = $null

    Blob ([string]$name, [object]$data, [object]$property) : base ($name)
    {
        $this.data = $data
        $this.Property = $property
    }

    [object[]] GetChildItem()
    {
        $obj =  @()

        $blobs = Get-AzureStorageBlob -Context $this.data.Context -Container $this.data.Name
        return $blobs
    }
}

class Compute : Azure
{
    Compute(): base($this.GetType())
    {
    }

    Compute([string]$name): base($name)
    {
    }

    [object] GetAzureRmResource()
    {
        return AzureRM.Resources\Get-AzureRmResource;
    }

    [object[]] GetChildItem()
    {
        $obj =  @()

        $resourceSet = $this.GetAzureRmResource();

        # SubSet of Compute resources only
        $computeSet = $resourceSet.Where{$_.ResourceType -like 'Microsoft.Compute*'}

        # Name of Unique Compute resources to create top level item
        $computeTypeName = $computeSet.ForEach{($_.resourceType -split '/')[1]} | sort -Unique

        $computeTypeName.Foreach{
            $Name = $_
            $FullName = 'Microsoft.Compute',$Name -join '/'
            $obj+= [ComputeResource]::new($Name, $computeSet, $_, $FullName);
        }

        $obj+= [ResourceGroups]([RGImpl]::RGInstance());
        return $obj
    }
}

class ComputeResource : Azure
{
    Hidden [string]$fullName;
    Hidden [object]$data = $null
    [object]$Property = $null

    ComputeResource ([string]$name, [object]$data, [object]$property, [string]$fullName) : base ($name)
    {
        $this.data = $data
        $this.Property = $property
        $this.fullName = $fullName
    }

    [object[]] GetChildItem()
    {
        $obj =  @()

        # SubSet of named compute resources and their children, grandchildren etc.
        $childSet = $this.data.Where{$_.ResourceType.Startswith($this.fullName)}

         Write-Verbose $this.fullName

        if($this.fullName -eq "Microsoft.Compute/virtualMachines")
        {
            return Get-AzureRmVM
        }
        else
        {
            # Display the subSet of named compute resources only
            $this.data.Where{$_.ResourceType -eq $fullName} | select ResourceName,ResourceType,ResourceGroupName

            if($childSet) {

                # Name of unique child resources for the named Compute resource
                $childTypeName = $childSet.ForEach{($_.ResourceType -split '/')[2]} | sort -Unique

                $childTypeName.Foreach{
                    $obj += [SHiPSLeaf]::new($_);
                }
            }
        }

        return $obj
    }
}
