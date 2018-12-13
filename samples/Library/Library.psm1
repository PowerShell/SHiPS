<#
    Modeling a Music Tree for example:

        M:
          - Classic
            - SwanLack
            - BlueDanube
          - Rock
            - Turnstile, Generator
            -Imagine Dragons, Thunder



    Assuming you have run Install-Module SHiPS

    Import-Module  SHiPS
    Import-Module  .\samples\Library\Library.psm1

    new-psdrive -name M -psprovider SHiPS -root Library#Music
    cd M:
    dir
    Get-Content .\Classic\SwanLake
    Set-Content .\Classic\SwanLake -Value 'Cool!'
    Get-Content .\Classic\SwanLake
#>

using namespace Microsoft.PowerShell.SHiPS


[SHiPSProvider(UseCache=$true)]
class Music : SHiPSDirectory
{
    # Must define this c'tor if it can be used as a drive root, e.g.
    # new-psdrive -name abc -psprovider SHiPS -root module#type
    # Also it is good practice to define this c'tor so that you can create a drive and test it in isolation fashion.
    Music([string]$name): base($name)
    {
    }

    # Mandatory it gets called by SHiPS while a user does 'dir'
    [object[]] GetChildItem()
    {
        $obj =  @()
        $obj += [Classic]::new()
        $obj += [Rock]::new()

        return $obj;
    }
}
class Classic : SHiPSDirectory
{
    # Mimicking file storage, database, store, etc
    hidden [object]$children = @()
    hidden static [string]$SwanLake = "
    Tchaikovsky's magical ballet tells the story of the doomed love of Prince Siegfried and Princess Odette,
    Prince Siegfried goes out hunting one night and chases a group of swans - one of them transforms into a
    young woman, Odette, who explains that she and her companions were turned into swans by the evil Baron Von Rothbart."

    hidden static [string]$Donau = "
    Johann Strauss Jr.'s status as an internationally recognized Austrian icon began with the success
    of his waltz, An der schönen, blauen Donau (The Blue Danube Waltz), at the Paris Exhibition of 1867."


    Classic() : base ($this.GetType())
    {
        $this.children += [MusicLeaf]::new('SwanLake', [Classic]::SwanLake)
        $this.children += [MusicLeaf]::new('BlueDanube', [Classic]::Donau)
    }

    [object[]] GetChildItem()
    {
        # fetch data from our storage and return it
        return $this.children
    }

    # Directory supports SetContent and GetContent because the path may not exist yet, equivalent to new-item in this case.
    # Leaf supports GetContent only.
    [object] SetContent([string]$value, [string]$path)
    {
        # 'value' is the string content. We can save it to a file, database, upload it to Azure, etc.
        # But for an this example, we just store it into the memory.

        $newPath = Microsoft.PowerShell.Management\Split-Path $Path -Leaf
        $child = [MusicLeaf]::new($newPath, $value)
        $this.children +=$child
        return $child
    }
}


class MusicLeaf : SHiPSLeaf
{
    hidden [string]$data = $null

    MusicLeaf([string]$name) : base ($name)
    {
    }

    MusicLeaf ([string]$name, [string]$content) : base ($name)
    {
        $this.data = $content
    }

    [string] GetContent()
    {
        $bp = $this.ProviderContext.BoundParameters

        if ($bp)
        {
            # Get-Content -TotalCount
            if ($bp.TotalCount)
            {
                # Making up lines artificially
                $text = @()
                $array = $this.data -split '\s+' -match '\S'
                $count = [Math]::Min($bp.TotalCount, $array.Length)
                for ($i = 0; $i -lt $count; $i++) {
                    $text+=$array[$i]
                }
                return $text
            }
        }

        return $this.data
    }

    [object] SetContent([string]$value, [string]$path)
    {
        $this.data = $value
        return $this
    }
}


class Rock : SHiPSDirectory
{
    Rock () : base ($this.GetType())
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
        $obj += "Turnstile, Generator"
        $obj += "Imagine Dragons, Thunder"

        return $obj
    }
}
