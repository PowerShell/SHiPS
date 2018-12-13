<#
   Assuming you clone to E:\azure\

    Import-Module  ..\SHiPS.psd1
    Import-Module  .\adhoc1.psm1
    new-psdrive -name JJ -psprovider SHiPS -root adhoc1#Root

    cd JJ:
    dir

test:
    ipmo will show the psxml foramt file is found and loaded.
#>

using namespace Microsoft.PowerShell.SHiPS

$MyInvocation

class Root : SHiPSDirectory
{
    Root([string]$name): base($name){}

    Root ([string]$name, [object]$nodeProperty) : base ($name) {}

    [object[]] GetChildItem()
    {

        #the following two lines are evil to see it breaks or hangs
        $MyInvocation
        ipmo -force azurerm.resources -Debug -verbose

        $obj = @()
        $obj += [Austin]::new("Bill",  "PowerShell");
        $obj += [Chris]::new("William");
        return $obj
    }
}

class Austin : SHiPSDirectory
{
    Hidden [object]$data = $null

    Austin([string]$name): base($name)
    {
    }

    Austin ([string]$name, [object]$data) : base ($name)
    {
        $this.data = $data
    }

    [object] Funny()
    {
        return Get-Service BITS
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
        # $data gets set when the object gets created. Through this we pass around the data from parent node to child
        $processes = Get-Process $this.data
        foreach ($p in $processes) {
            $obj += $p;
            break;
        }

        # from function call
        $result = $this.Funny()
        $obj += $result;

        # child class
        $obj += [Chris]::new("Hello Chris");

        return $obj;
    }
}


class Chris : SHiPSLeaf
{
    Chris([string]$name): base($name)
    {
    }
}