param
(
    [Parameter()]
    $RunCount = 10,
    $SelectSubscription = 'CI Automation Demo'
)

$ProgressPreference = 'SilentlyContinue'

Import-Module SHiPS
Import-Module SHiPS.AzureRM
Import-Module $PSScriptRoot\..\test\AzureResourceNavigation.psm1 -force

Push-Location

#$null = Login-AzureRmAccount -ErrorAction Stop

# Remove already existing drive, if it exists
Get-PSDrive -Name Azure -PSProvider SHiPS -ErrorAction SilentlyContinue | Remove-PSDrive
$null = New-PSDrive -name Azure -PSProvider SHiPS -Root AzureResourceNavigation#Azure;

#==============================================================
$scenario = "`nSelecting a Subscription `n"
Write-Output $scenario

$result = (Measure-Command {1..$RunCount | %{ az account set --subscription $SelectSubscription}}).TotalSeconds
Write-Output "CLI Total Time:   $result"
Write-Output "CLI Average Time: $($result/$RunCount)"

$result = (Measure-Command {1..$RunCount | %{ dir Azure:\$SelectSubscription -force }}).TotalSeconds
Write-Output "PCC Total Time:   $result"
Write-Output "PCC Average Time: $($result/$RunCount)"
Write-Output "================================================ `n"

#==============================================================
$scenario = "List the VMs from already selected subscription `n"
Write-Output $scenario

az account set --subscription $SelectSubscription
$result = (Measure-Command {1..$RunCount | %{ az vm list }}).TotalSeconds
Write-Output "CLI Total Time:   $result"
Write-Output "CLI Average Time: $($result/$RunCount)"

cd Azure:\$SelectSubscription
$result = (Measure-Command {1..$RunCount | %{ dir .\Compute\virtualMachines -force -WarningAction SilentlyContinue}}).TotalSeconds
Write-Output "PCC Total Time:   $result"
Write-Output "PCC Average Time: $($result/$RunCount)"
Write-Output "================================================ `n"

#==============================================================
$scenario =	"Create the drive and list the VMs as first experience (no caching) `n"
Write-Output $scenario

$result = (Measure-Command {1..$RunCount | %{ New-PSDrive -Name "Azure$_" -PSProvider SHiPS -Root AzureResourceNavigation#Azure
                                        dir "Azure$($_):\$SelectSubscription\Compute\virtualMachines" -WarningAction SilentlyContinue}}).TotalSeconds
Write-Output "PCC Total Time:   $result"
Write-Output "PCC Average Time: $($result/$RunCount)"
Write-Output "================================================ `n"

#==============================================================
$scenario =	"List the Storage accounts from already selected subscription `n"
Write-Output $scenario

az account set --subscription $SelectSubscription
$result = (Measure-Command {1..$RunCount | %{ az storage account list}}).TotalSeconds
Write-Output "CLI Total Time:   $result"
Write-Output "CLI Average Time: $($result/$RunCount)"

cd "Azure:\$SelectSubscription\Storage\StorageAccount"

$result = (Measure-Command {1..$RunCount | %{ dir  -force }}).TotalSeconds
Write-Output "PCC Total Time:   $result"
Write-Output "PCC Average Time: $($result/$RunCount)"
Write-Output "================================================ `n"
Write-Output "************************************************ `n"


# Remove already existing drive, if it exists
$driveName = 'Az'

Get-PSDrive -Name $driveName -PSProvider SHiPS -ErrorAction SilentlyContinue | Remove-PSDrive
$null = New-PSDrive -name $driveName -PSProvider SHiPS -Root SHiPS.AzureRM#Azure
cd $driveName":"

$scenario = "`nSHiPS.AzureRM: Selecting a Subscription (no caching)`n"
Write-Output $scenario


$result = (Measure-Command {1..$RunCount | %{ dir -force }}).TotalSeconds
Write-Output "PCC SHiPS.AzureRM Total Time:   $result"
Write-Output "PCC SHiPS.AzureRM Average Time: $($result/$RunCount)"
Write-Output "================================================ `n"

$scenario = "`nSHiPS.AzureRM: List Azure ResourceGroup (no caching)`n"
Write-Output $scenario

$SelectSubscription = "AutomationTeam"
cd Azure:\$SelectSubscription
$result = (Measure-Command {1..$RunCount | %{ dir az:\AutomationTeam -force -WarningAction SilentlyContinue}}).TotalSeconds
Write-Output "PCC SHiPS.AzureRM Total Time:   $result"
Write-Output "PCC SHiPS.AzureRM Average Time: $($result/$RunCount)"
Write-Output "================================================ `n"

#==============================================================

Pop-Location
