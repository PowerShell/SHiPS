using System.Collections.Generic;
using Microsoft.PowerShell.SHiPS;

namespace FamilyTreeInCSharp
{

    /// <summary>
    /// A class defines the family tree root.
    /// </summary>
    public class Root : SHiPSDirectory
    {
        public Root(string name) : base(name)
        {
        }

        public override object[] GetChildItem()
        {
            return new List<object>
            {
                new Erin("Erin"),
                new Ethen("Ethen")
            }.ToArray();
        }
    }

    /// <summary>
    /// Defines a node with children.
    /// </summary>
    public class Erin : SHiPSDirectory
    {
        public Erin(string name) : base(name)
        {
        }

        public override object[] GetChildItem()
        {
             return new object[] { new Mike("Erin's kid")};
        }
    }

    /// <summary>
    /// Defines a node with children.
    /// </summary>
    public class Mike : SHiPSDirectory
    {
        public Mike(string name) : base(name)
        {
        }

        public override object[] GetChildItem()
        {
            return new object[] {"Hello I am Mike."};
        }
    }

    /// <summary>
    /// Defines a leaf node.
    /// </summary>
    public class Ethen : SHiPSLeaf
    {
        public Ethen(string name) : base(name)
        {
        }
    }
}
