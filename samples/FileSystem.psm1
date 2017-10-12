<#
    Modeling the Windows FileSystem

    Try it:
        Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

        Import-Module  SHiPS
        Import-Module  .\samples\FileSystem.psm1
        new-psdrive -name FS -psprovider SHiPS -root FileSystem#C
        cd FS:
        dir

#>

using namespace Microsoft.PowerShell.SHiPS


[SHiPSProvider(BuiltinProgress=$false)]
class C : SHiPSDirectory
{
    Hidden [object]$data = $null

    C([string]$name): base($name)
    {
    }

    C ([string]$name, [object]$data) : base ($name)
    {
        $this.data = $data
    }


    [object[]] GetChildItemImpl([string] $data)
    {
       $obj =  @()

       Write-Verbose $data

       dir $data -force | ForEach-Object {

                if($_.PSIsContainer)
                {
                    $obj+=[C]::new($_.Name, $_.FullName)
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

              return $this.GetChildItemImpl("Microsoft.PowerShell.Core\FileSystem::$($driveName):\")
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

class E : C
{
    E([string]$name): base($name)
    {
    }

     [object[]] GetChildItem()
     {
        return ([C]$this).GetChildItem()
    }
}
