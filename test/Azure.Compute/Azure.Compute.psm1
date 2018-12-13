<#
    $driveName = 'azCompute'
    Remove-PSDrive $driveName -ErrorAction SilentlyContinue
    ipmo .\Azure.Compute.psd1 -Force
    new-psdrive -Name $driveName -PSProvider SHiPS -Root Azure.Compute#Compute
    cd $driveName":"
#>
using namespace Microsoft.PowerShell.SHiPS

class Compute : SHiPSDirectory
{
    Compute ()
    {
        $this.Name = $this.GetType().Name;
    }

    Compute ([string]$name)
    {
        $this.Name     = $name;
    }

    [object[]] GetChildItem()
    {
        $result =  @()
        $result += [AvailabilitySets]::new()
        $result += [Locations]::new()
        $result += [VirtualMachines]::new()
        $result += [VirtualMachineScaleSets]::new()
        return $result
    }
}

class ComputeLeaf : SHiPSLeaf
{
    static [void] WebRedirectMessage([string]$webUri, [string]$area)
    {
        Write-Verbose -Verbose "No items found. Please visit $webUri to learn more about $area"
    }
}

#TODO: What's the story for Disks, Images, Operations, RestorePointCollections,Snapshots

class AvailabilitySets : Compute
{
    [object[]] GetChildItem()
    {
        $result = Get-AzureRmResourceGroup | ForEach-Object {AzureRM.Compute\Get-AzureRmAvailabilitySet -ResourceGroupName $_.ResourceGroupName}

        if(-not $result){
            [ComputeLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/virtual-machines/virtual-machines-windows-classic-configure-availability','AvailabilitySets')
        }

        return $result
    }
}

class Locations : Compute
{
    [object[]] GetChildItem()
    {
        $result = AzureRM.Resources\Get-AzureRmLocation

        if(-not $result){
            [ComputeLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/#pivot=services&panel=Compute','Locations')
        }

        return $result
    }
}

class VirtualMachines : Compute
{
    [object[]] GetChildItem()
    {
        $result = AzureRM.Compute\Get-AzureRmVM

        if(-not $result){
            [ComputeLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/virtual-machines','VirtualMachines')
        }

        return $result
    }
}

class VirtualMachineScaleSets : Compute
{
    [object[]] GetChildItem()
    {
        $result = AzureRM.Compute\Get-AzureRmVmss

        if(-not $result){
            [ComputeLeaf]::WebRedirectMessage('https://docs.microsoft.com/en-us/azure/virtual-machine-scale-sets/','VirtualMachineScaleSets')
        }

        return $result
    }
}
