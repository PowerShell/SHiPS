<#
    Modeling a Family Tree for example:

    Austin
          - Bill
          - Ben
                - Cathy
                - Chris



    Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

    Import-Module  SHiPS
    Import-Module  .\samples\FamilyTree.psm1

    new-psdrive -name Austin -psprovider SHiPS -root FamilyTree#Austin
    cd Austin:
    dir
    cd Ben
    dir | %{$_.data}



    new-psdrive -name son -psprovider SHiPS -root FamilyTree#Ben
    cd son:
    dir
    dir | %{$_.data}

#>

using namespace Microsoft.PowerShell.SHiPS

class Person
{
    [string]$Name;
    [string]$DOB;
    [string]$Gender;

    Person([string]$name, [string]$dob, [string]$gender)
    {
        $this.Name = $name
        $this.DOB      = $dob
        $this.Gender   = $gender
    }
}

class Austin : SHiPSDirectory
{
    Austin() : base($this.GetType())
    {
    }

    # Optional method
    # Must define this c'tor if it can be used as a drive root, e.g.
    # new-psdrive -name abc -psprovider SHiPS -root module#type
    # Also it is good practice to define this c'tor so that you can create a drive and test it in isolation fashion.
    Austin([string]$name): base($name)
    {
    }

    # Mandatory it gets called by SHiPS while a user does 'dir'
    [object[]] GetChildItem()
    {
        $obj =  @()

        Write-verbose "hello you have specified -verbose."

        $obj += [Ben]::new();
        $obj += [Bill]::new();

        return $obj;
    }
}

class Bill : SHiPSLeaf
{
    static $PersonName = "Bill"
    static $PersonData =[Person]::new([Bill]::PersonName, "5015", "M");

    Bill () : base ([Bill]::PersonName)
    {
    }
}

class Ben : SHiPSDirectory
{
    static $PersonName = "Ben"
    static $PersonData =[Person]::new([Ben]::PersonName, "5005", "M");

    Ben () : base ([Ben]::PersonName)
    {
    }

    Ben([string]$name): base($name)
    {
    }
    [object[]] GetChildItem()
    {
        $obj =  @()
        $obj += [Chris]::new();
        $obj += [Cathy]::new();
        return $obj;
    }
}


class Chris : SHiPSLeaf
{
    static $PersonName = "Chris"
    static $PersonData = [Person]::new([Chris]::PersonName, "5034", "M");
    $Data = [Chris]::PersonData

    Chris () : base ([Chris]::PersonName)
    {
    }
}

class Cathy : SHiPSLeaf
{
    static $PersonName = "Cathy"
    static $PersonData =[Person]::new([Cathy]::PersonName, "5050", "F");
    $Data = [Cathy]::PersonData

    Cathy () : base ("Cathy")
    {
    }
}
