using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace CodeOwls.PowerShell.Provider
{
    public static class CollectionExtensions
    {
        public static string ToArgList(this Collection<string> items)
        {
            if (null == items)
            {
                return "(<null>)";
            }

            return "(" + String.Join("), (", items.ToArray()) + ")";
        }

        public static string ToArgString(this PSObject o)
        {
            if (null == o)
            {
                return "<null>";
            }
            return o.BaseObject.ToString();
        }
        public static string ToArgString(this object o)
        {
            if (null == o)
            {
                return "<null>";
            }
            return o.ToString();
        }



    }
}
