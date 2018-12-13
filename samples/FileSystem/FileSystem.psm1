<#
    Modeling the FileSystem

    Try it:
        Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

        Import-Module  SHiPS
        Import-Module  .\samples\FileSystem.psm1
        new-psdrive -name FS -psprovider SHiPS -root FileSystem#Home
        cd FS:
        dir

#>

using namespace Microsoft.PowerShell.SHiPS


[SHiPSProvider(BuiltinProgress=$false)]
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