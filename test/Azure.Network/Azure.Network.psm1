<#
    $driveName = 'AzureNetwork'
    Remove-PSDrive $driveName -ErrorAction SilentlyContinue
    Import-Module .\Azure.Network.psd1 -Force
    New-PSDrive -Name $driveName -PSProvider SHiPS -Root Azure.Network#Network
    cd $driveName":"
#>

using namespace Microsoft.PowerShell.SHiPS

class Network : SHiPSDirectory
{
    Network ()
    {
        $this.Name = $this.GetType().Name;
    }

    Network ([string]$name) : base([string]$name)
    {
        $this.Name     = $name;
    }

    [object[]] GetChildItem()
    {
        $result =  @()
        $result += [NetworkInterfaces]::new()
        $result += [NetworkSecurityGroups]::new()
        $result += [VirtualNetworks]::new()
        return $result
    }

    static [void] WebRedirectMessage([string]$webUri, [string]$area)
    {
        Write-Verbose -Verbose "No items found. Please visit $webUri to learn more about $area"
    }
}

class NetworkLeaf : SHiPSLeaf
{
    static [void] WebRedirectMessage([string]$webUri, [string]$area)
    {
        Write-Verbose -Verbose "No items found. Please visit $webUri to learn more about $area"
    }
}

class NetworkInterfaces : Network
{

    NetworkInterfaces () : base ($this.GetType().Name)
    {
    }

    [object[]] GetChildItem()
    {
        $result = Get-AzureRmResourceGroup | ForEach-Object {AzureRM.Network\Get-AzureRmNetworkInterface -ResourceGroupName $_.ResourceGroupName}

        if(-not $result){
            [NetworkLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/#pivot=services&panel=network','NetworkInterfaces')
        }

        return $result
    }
}

class NetworkSecurityGroups : Network
{
    NetworkSecurityGroups () : base ($this.GetType().Name)
    {
    }

    [object[]] GetChildItem()
    {
        $result = AzureRM.Network\Get-AzureRmNetworkSecurityGroup

        if(-not $result){
            [NetworkLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/#pivot=services&panel=network','NetworkSecurityGroup')
        }

        return $result
    }
}

class VirtualNetworks : Network
{
    VirtualNetworks () : base ($this.GetType().Name)
    {
    }

    [object[]] GetChildItem()
    {
        $result = AzureRM.Network\Get-AzureRmVirtualNetwork

        if(-not $result){
            [NetworkLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/#pivot=services&panel=network','VirtualNetwork')
        }

        return $result
    }
}