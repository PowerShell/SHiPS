<#
    Assuming you have done git clone and run build.ps1, cd to your git clone folder and try the following.

    Import-Module  SHiPS
    Import-Module  .\test\adhoc\Cache-NoCach-CacheTest.psm1

    new-psdrive -name n -psprovider SHiPS -root Cache-NoCach-CacheTest#UseCacheTest


    Test:
    cd n:\
    dir                  # you will see progress bar.
    dir                  # no progress bar
    cd UseNoCacheTest
    dir                  # you will see the progress bar
    dir                  # you will see the progress bar
    cd ChildWithCache    # you will see the progress bar
    dir                  # you will see the progress bar
    cd ChildWithCache2   # you will see the progress bar
    dir                  # you will see the progress bar

    cd n:\
    dir                  # no progress
    dir -force           # you will see the progress bar
#>

using namespace Microsoft.PowerShell.SHiPS


[SHiPSProvider(UseCache=$true)]
class UseCacheTest : SHiPSDirectory
{
    Hidden [object]$data = $null

    UseCacheTest([string]$name): base($name)
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

        While($count -lt 2)
        {
            $count++
            Write-Progress -Id $id -ParentId $parentId -Activity $activityMessage -PercentComplete $count

            Start-Sleep -Seconds 1

            $ps=get-process powershell | Select-Object -First 1

            #Write-warning "hello$count Warning info"

            Write-debug "hello$count debug info"

            $obj += [UseNoCacheTest]::new("UseNoCacheTest");

            Write-verbose "hello$count verbose info"
        }

        Write-Progress -Id $id  -ParentId $parentId -Activity $activityMessage -Completed

        return $obj;
    }
}


[SHiPSProvider(UseCache=$false)]
class UseNoCacheTest : SHiPSDirectory
{
    Hidden [object]$data = $null

    UseNoCacheTest([string]$name): base($name)
    {
    }



    [object[]] GetChildItem()
    {
        $obj =  @()

        Write-verbose "hello, you specified -verbose."
        #Write-warning "hello, this is the Warning information."
        Write-debug "hello, this is the debug information."

        $activityMessage ="This is a demo to show you how to use Write-Progress for long running operations"

        $count=1
        $id = 50      # random number
        $parentId = 1 # random number

        While($count -lt 2)
        {
            $count++
            Write-Progress -Id $id -Activity $activityMessage -PercentComplete $count -ParentId $parentId

            Start-Sleep -Seconds 1

            $ps=get-process powershell | Select-Object -First 1

            #Write-warning "hello$count Warning info"

            #Write-debug "hello$count debug info"

            $obj += [ChildWithCache]::new("ChildWithCache");

            Write-verbose "hello$count verbose info"
        }

        Write-Progress -Id $id -Activity $activityMessage -Completed

        return $obj;
    }
}

[SHiPSProvider(UseCache=$true)]
class ChildWithCache : SHiPSDirectory
{
    Hidden [object]$data = $null

    ChildWithCache([string]$name): base($name)
    {
    }



    [object[]] GetChildItem()
    {
        $obj =  @()

        Write-verbose "hello, you specified -verbose."
       # Write-warning "hello, this is the Warning information."
        Write-debug "hello, this is the debug information."

        $activityMessage ="This is a demo to show you how to use Write-Progress for long running operations"

        $count=1
        $id = 50      # random number
        $parentId = 1 # random number

        While($count -lt 2)
        {
            $count++
            Write-Progress -Id $id -Activity $activityMessage -PercentComplete $count -ParentId $parentId

            Start-Sleep -Seconds 1

            $ps=get-process powershell | Select-Object -First 1

            #Write-warning "hello$count Warning info"

            #Write-debug "hello$count debug info"

            $obj += [ChildWithCache2]::new("ChildWithCache2");

            Write-verbose "hello$count verbose info"
        }

        Write-Progress -Id $id -Activity $activityMessage -Completed

        return $obj;
    }
}

[SHiPSProvider(UseCache=$true)]
class ChildWithCache2 : SHiPSDirectory
{
    Hidden [object]$data = $null

    ChildWithCache2([string]$name): base($name)
    {
    }



    [object[]] GetChildItem()
    {
        $obj =  @()

        Write-verbose "hello, you specified -verbose."
       # Write-warning "hello, this is the Warning information."
        Write-debug "hello, this is the debug information."

        $activityMessage ="This is a demo to show you how to use Write-Progress for long running operations"

        $count=1
        $id = 50      # random number
        $parentId = 1 # random number

        While($count -lt 2)
        {
            $count++
            Write-Progress -Id $id -Activity $activityMessage -PercentComplete $count -ParentId $parentId

            Start-Sleep -Seconds 1

            $ps=get-process powershell | Select-Object -First 1

            #Write-warning "hello$count Warning info"

            #Write-debug "hello$count debug info"

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
