<#
    Modeling a tree for example:

    Nature
          - Plant
                - Birch
                - Maple
                - Oak
          - Creature
                - Cat
                - Dog
                    - Husky
                    - Bulldog



    Import-Module  SHiPS
    Import-Module  .\samples\DynamicParameterSample.psm1

    new-psdrive -name n -psprovider SHiPS -root 'DynamicParameterSample#Nature'
    cd n:
    dir

    dir -Type Plant

    dir -Type Creature

    dir -Filter p* -recurse

#>

using namespace Microsoft.PowerShell.SHiPS

# A data structure holding data for Plants or Creatures
class NatureData
{
    [string]$Name;
    [string]$Type;

    NatureData([string]$name, [string]$type)
    {
        $this.Name = $name
        $this.Type = $type
    }
}


# Define dynamic parameters
class SampleDynamicParameter
{
    [Parameter()]
    [ValidateSet("Plant","Creature")]
    [Alias("t")]
    [string]$Type
}



# Define the base class so that the derived class does not need to define GetChildItemDynamicParameters.
class NatureBase : SHiPSDirectory
{
    # Make sure this constructor is defined so that a derived class, Nature, can be created as a psdrive root
    NatureBase([string]$name): base($name)
    {
    }

    # Define dynamic parameters for Get-ChildItem
    [object] GetChildItemDynamicParameters()
    {
        return [SampleDynamicParameter]::new()
    }

    # Assuming we have a backend database and support searching with wildcards.
    [object] FuzzySearch([string] $queryString)
    {
        $queryString = $this.ProviderContext.Filter

        if($queryString)
        {
            # Connecting to a database and searching records. For demo purpose, assume we find a few and return them.
            return @("Pointer", "Poodle", "Pug", "Pomeranian", "Pui", "Pumi")
        }
        else
        {
            return "No record found."
        }
    }
}


# Define the hierarchy
class Nature : NatureBase
{
    Nature([string]$name): base($name)
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()

        $dp = $this.ProviderContext.DynamicParameters -as [SampleDynamicParameter]

        # As we defined the GetChildItemDynamicParameters() in the base class, $dp should not be null
        # However if a user does not use -Type, then $dp.Type will be null
        if((-not $dp) -or (-not $dp.Type))
        {
            $obj += [Plant]::new();
            $obj += [Creature]::new();
        }
        elseif ($dp.Type -eq "Plant")
        {
            $obj += [Plant]::new();
        }
        else
        {
            $obj += [Creature]::new();
        }
        return $obj;
    }
}

class Plant : NatureBase
{
    Plant () : base($this.GetType())
    {
    }

    [object[]] GetChildItem()
    {
        $dp = $this.ProviderContext.DynamicParameters -as [SampleDynamicParameter]

        $obj =  @()

        if((-not $dp) -or (-not $dp.Type) -or ($dp.Type -eq "Plant"))
        {
            $obj += [Birch]::new();
            $obj += [Maple]::new();
            $obj += [Oak]::new();
        }
        return $obj;
    }

}

class Creature : NatureBase
{
    Creature () : base($this.GetType())
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()
        $dp = $this.ProviderContext.DynamicParameters -as [SampleDynamicParameter]
        if((-not $dp) -or (-not $dp.Type) -or ($dp.Type -eq "Creature"))
        {
            $obj += [Cat]::new();
            $obj += [Dog]::new();
        }

        return $obj;
    }
}

class Dog : NatureBase
{
    static $data =[NatureData]::new("Dog", "Creature");
    $Properties = [Dog]::data

    Dog () : base($this.GetType())
    {
    }

    [object[]] GetChildItem()
    {
        $obj =  @()

        # Check if a user wants to do server-side searching
        $queryString = $this.ProviderContext.Filter

        if($queryString)
        {
            # Connecting to DB and searching for data
            return $this.FuzzySearch($queryString)
        }

        # Check if a user uses dynamic parameters
        $dp = $this.ProviderContext.DynamicParameters -as [SampleDynamicParameter]

        if((-not $dp) -or (-not $dp.Type) -or ($dp.Type -eq "Creature"))
        {
            $obj += [Husky]::new();
            $obj += [Bulldog]::new();
        }
        return $obj;
    }
}


class Birch : SHiPSLeaf
{
    static $data =[NatureData]::new("Birch", "Plant");
    $Properties = [Birch]::data

    Birch() : base($this.GetType())
    {
    }
}
class Maple : SHiPSLeaf
{
    static $data =[NatureData]::new("Maple", "Plant");
    $Properties = [Maple]::data

    Maple() : base($this.GetType())
    {
    }
}
class Oak : SHiPSLeaf
{
    static $data =[NatureData]::new("Oak", "Plant");
    $Properties = [Oak]::data

    Oak() : base($this.GetType())
    {
    }
}


class Cat : SHiPSLeaf
{
    static $data =[NatureData]::new("Cat", "Creature");
    $Properties = [Cat]::data

    Cat() : base($this.GetType())
    {
    }
}

class Husky : SHiPSLeaf
{
    static $data =[NatureData]::new("Husky", "Creature");
    $Properties = [Husky]::data

    Husky() : base($this.GetType())
    {
    }
}

class Bulldog : SHiPSLeaf
{
    static $data =[NatureData]::new("Bulldog", "Creature");
    $Properties = [Bulldog]::data

    Bulldog() : base($this.GetType())
    {
    }
}