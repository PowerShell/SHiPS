<#
    Assuming you have done clone. Now cd to SHiPS\test\automation folder. Try the following.

    Import-Module  ..\..\src\out\SHiPS\SHiPS
    Import-Module  .\sampleRecursion.psm1

    new-psdrive -name JT -psprovider SHiPS -root sampleRecursion#Root

    cd JT:
    dir
#>

<#

For debugging, you can do the following:

 Assuming you clone to E:\azure\
 cd e:\azure\PSCloudConsole\test

Import-Module  .\SHiPS.psd1
Import-Module  .\sampleRecursion.psm1

$mod=get-module 'sampleRecursion';
$t1=&($mod){[root]::new("root")}
$a=$t1.GetChildItem()
$a | %{$_.Name}

$b=$a[0].GetChildItem()
$b | %{$_.Name}

#>


using namespace Microsoft.PowerShell.SHiPS
$script:PowerShellProcessName = if($IsCoreCLR) {'pwsh'} else{ 'PowerShell'}

class DataHolder
{
    [string] $Name
    [string[]]$ProcessName
    [int]$Number

    DataHolder(){}

    DataHolder ([string]$name, [string]$ProcessName, [int]$number)
    {
        $this.Name = $name;
        $this.ProcessName = $ProcessName;
        $this.Number = $number;
    }
}


class root: SHiPSDirectory
{
    Root([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
        return [Austin]::new("Austin", [DataHolder]::new("Process", $script:PowerShellProcessName, 1));
    }
}



class Austin : SHiPSDirectory
{
    Hidden [DataHolder]$data = $null
    [object]$Properties

    Austin ([string]$name, [object]$data) : base ($name)
    {
        $this.data=$data
    }


    [object[]] GetChildItem()
    {

        $n=$this.data.Number
        $m=$this.data.name
        Write-Verbose "$n, $m"

        if($this.data.Number -ge 5)
        {
            $n= $this.data.Number
            $message = "Current iteration number is $n. I have done enough. Exiting..."
            Write-Warning $message
            return $message
        }
        else
        {
            $this.Properties = (get-process $this.data.ProcessName);

            $next = [DataHolder]::New();

            switch ($this.data.Number)
            {
             "1"  {  $next.ProcessName=$script:PowerShellProcessName; $next.Name ="explorer1"; $next.Number =2; break}
             "2"  {  $next.ProcessName=@($script:PowerShellProcessName); $next.Name ="explorer2"; $next.Number =3;  break}
             "3"  {  $next.ProcessName=@($script:PowerShellProcessName); $next.Name ="explorer3"; $next.Number =4;  break}
             "4"  {  $next.ProcessName=@($script:PowerShellProcessName); $next.Name ="explorer4"; $next.Number =5; break}
             "5"  {  $next.ProcessName=@($script:PowerShellProcessName); $next.Name ="explorer5"; $next.Number =6; break}
            }

            $n=$next.Number
            $m=$next.name

            Write-Verbose "$n, $m"

            if($this.data.Number -ge 5)
            {
                return [AlexLeaf]::new($($next.Name))
            }
            else
            {
                return [Austin]::new($($next.Name), $next)
            }
            return $child;
        }

    }

}

class AlexLeaf : SHiPSLeaf
{
    AlexLeaf([string]$name): base($name)
    {
    }
}
