<#
    Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

    Import-Module  SHiPS
    Import-Module  .\samples\ShowProgress.psm1
    new-psdrive -name m -psprovider SHiPS -root ShowProgress#BuiltinProgress

    cd m:
    dir
    dir -verbose

    cd c:\
    new-psdrive -name n -psprovider SHiPS -root ShowProgress#NoBuiltinProgress
    cd n:
    dir -verbose

#>

using namespace Microsoft.PowerShell.SHiPS


[SHiPSProvider(UseCache=$true)]
class BuiltinProgress : SHiPSDirectory
{
    Hidden [object]$data = $null

    BuiltinProgress([string]$name): base($name)
    {
    }



    [object[]] GetChildItem()
    {
        $obj =  @()

        Write-verbose "hello, you specified -verbose." -Verbose
        Write-warning "hello, this is the Warning information."
        Write-debug "hello, this is the debug information."

        $activityMessage ="This is a demo to show you how to use Write-Progress for long running operations"

        $count=1
        $id = 5      # random number
        $parentId = 1 # builtin progressid=1

        While($count -lt 3)
        {
            $count++
            Write-Progress -Id $id -ParentId $parentId -Activity $activityMessage -PercentComplete $count

            Start-Sleep -Seconds 1

            $ps=get-process powershell | Select-Object -First 1

            Write-warning "hello$count Warning info"

            Write-debug "hello$count debug info"

            $obj += [ABCLeaf]::new($ps.Name + $count, $ps);

            Write-verbose "hello$count verbose info"
        }

        Write-Progress -Id $id  -ParentId $parentId -Activity $activityMessage -Completed

        return $obj;
    }
}


[SHiPSProvider(BuiltinProgress= $false)]
class NoBuiltinProgress : SHiPSDirectory
{
    Hidden [object]$data = $null

    NoBuiltinProgress([string]$name): base($name)
    {
    }



    [object[]] GetChildItem()
    {
        $obj =  @()

        Write-verbose "hello, you specified -verbose."
        Write-warning "hello, this is the Warning information."
        Write-debug "hello, this is the debug information."

        $activityMessage ="This is a demo to show you how to use Write-Progress for long running operations"

        $count=1
        $id = 50      # random number
        $parentId = 1 # random number

        While($count -lt 3)
        {
            $count++
            Write-Progress -Id $id -Activity $activityMessage -PercentComplete $count -ParentId $parentId

            Start-Sleep -Seconds 1

            $ps=get-process powershell | Select-Object -First 1

            Write-warning "hello$count Warning info"

            Write-debug "hello$count debug info"

            $obj += [ABCLeaf]::new($ps.Name + $count, $ps);

            Write-verbose "hello$count verbose info"
        }

        Write-Progress -Id $id -Activity $activityMessage -Completed

        return $obj;
    }
}

class ABCLeaf : SHiPSLeaf
{
    Hidden [object]$data = $null


    ABCLeaf ([string]$name, [object]$data) : base ($name)
    {
        $this.data = $data
    }
}
