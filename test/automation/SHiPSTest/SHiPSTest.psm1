<#
    Assuming you have done clone. Now cd to SHiPS\test\automation folder. Try the following.

    Import-Module  ..\..\src\out\SHiPS\SHiPS
    Import-Module  .\SHiPSTest.psm1
    new-psdrive -name jj -psprovider SHiPS -root SHiPSTest#SHiPSTest
    cd jj:
    dir

#>

using namespace Microsoft.PowerShell.SHiPS
$script:PowerShellProcessName = if($IsCoreCLR) {'pwsh'} else{ 'PowerShell'}


class SHiPSTest : SHiPSDirectory
{

    SHiPSTest([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {

        $obj =  @()

        Write-verbose "You should see this verbose message without -verbose!!" -Verbose

        $ps = get-process $script:PowerShellProcessName
        $obj += [SHiPSTestLeaf]::new($ps[0].Name);

        Write-debug "hello debuggggggggggggggggggggg!!"


        $obj += [SHiPSTest]::new("SHiPSTest");
        $obj += "SHiPSTest222"


        Write-verbose "hello you have used -verbose!!"

        return $obj;
    }
}


class SHiPSTestLeaf : SHiPSLeaf
{
    SHiPSTestLeaf([string]$name): base($name)
    {
    }
}
