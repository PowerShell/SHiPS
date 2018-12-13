<#
    Assuming you have done clone. Now cd to SHiPS\test\automation folder. Try the following.

    Import-Module  ..\..\src\out\SHiPS\SHiPS
    Import-Module  .\abc.psm1
    new-psdrive -name abc -psprovider SHiPS -root abc#abc
    cd abc:
    dir

#>

using namespace Microsoft.PowerShell.SHiPS
$script:PowerShellProcessName = if($IsCoreCLR) {'pwsh'} else{ 'PowerShell'}

class ABC : SHiPSDirectory
{
    # Optional method
    # Must define this c'tor if it can be as a drive root, e.g.
    # new-psdrive -name abc -psprovider SHiPS -root abc#abc
    # Also it is good practice to define this c'tor so that you can test it in isolation fashion.
    ABC([string]$name): base($name)
    {
    }

    # Mandatory it gets called by PSMeteProvider while a user does 'dir'

    [object[]] GetChildItem()
    {
        #drawback: cannot be async as 'yield return' is not supported in class
        $obj =  @()

        Write-verbose "You should see this verbose message without -verbose!!" -Verbose
        Write-warning "hello1 Warning!!"
        Write-debug "hello1 debuggggggggggggggggggggg!!"


        $bits= Get-Service BITS
        $ps=get-process $script:PowerShellProcessName

        Write-warning "hello2 Warning!!"

        $obj += [ABCLeaf]::new($bits.Name, $bits);

        Write-debug "hello2 debuggggggggggggggggggggg!!"

        $obj += [ABCLeaf]::new($ps.Name, $ps);
        $obj += get-process $script:PowerShellProcessName


        Write-verbose "hello2 you have used -verbose!!"

        return $obj;
    }

    <#
    1. set-alias works only under a specific path.
    2. dynamic parameters:
    1)  Cannot show cmdlet default possible values (e.g., AllUser, CurrentUser )
    2)  Deal with parameterSets,
    3)  It will be tricky if New-Item and its corresponding command has conflict Parametersets?
    4)  Inability to hide/mark built-in parameters of provider cmdlets such as ItemType, Value, etc.
    #>
    [object]NewItem([string] $currentPath)
    {
        # need to know:
        # 1. dynamic parameters
        # 2. command

        $b = New-Object 'system.collections.generic.dictionary[string,hashtable]'
        $a=@{}
        if($currentPath -eq "VM")
        {
            $a = (gcm New-ModuleManifest).Parameters;
            $b.Add("New-AzureRmVM", $a)
            return $b
        }
        elseif($currentPath -eq "Image")
        {
            $b.Clear();
            $a = (gcm New-AzureRmImage).Parameters; #you can do extra parameter process
            $b.Add("New-AzureRmImage", $a)
            return $b

        }
        else
        {
            $b.Clear();
            $a = (gcm New-ModuleManifest).Parameters;
            $b.Add("New-ModuleManifest", $a)
            return $b
            #return $rootdefault
        }

    }
}


class ABCLeaf : SHiPSLeaf
{
    [object]$Property = $null

    ABCLeaf([string]$name): base($name)
    {
    }

    # Optional method
    # This c'tor is used in yourself. But name parameter must exist.
    ABCLeaf ([string]$name, [object]$property) : base ($name)
    {
        $this.Property = $property
    }
}
