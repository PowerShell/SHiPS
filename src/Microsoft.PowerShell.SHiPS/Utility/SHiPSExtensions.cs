using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using CodeOwls.PowerShell.Provider.PathNodes;
using System.Globalization;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Threading;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{
    internal static class StringExtensions
    {
        internal static bool EqualsIgnoreCase(this string str, string str2)
        {
            if (str == null && str2 == null)
            {
                return true;
            }

            if (str == null || str2 == null)
            {
                return false;
            }

            return str.Equals(str2, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ContainsIgnoreCase(this IEnumerable<string> collection, string value)
        {
            if (collection == null)
            {
                return false;
            }
            return collection.Any(s => s.EqualsIgnoreCase(value));
        }

        internal static bool ContainsIgnoreCase(this PSObject obj, string value)
        {
            if (obj == null)
            {
                return false;
            }

            var o = obj.BaseObject as PSVariable;
            return (o != null) && o.Name.EqualsIgnoreCase(value);
        }

        internal static bool ContainsIgnoreCase(this IEnumerable<string> collection, PSObject value)
        {
            if (collection == null || value == null)
            {
                return false;
            }

            return collection.Any(each =>
            {
                var o = value.BaseObject as PSVariable;
                return (o != null) && o.Name != null && o.Name.EqualsIgnoreCase(each);
            });
        }

        internal static bool ContainsIgnoreCase(this IEnumerable<PSObject> collection, string value)
        {
            if (collection == null)
            {
                return false;
            }

            return collection.Any(each =>
            {
                var o = each.BaseObject as PSVariable;
                return (o != null) && o.Name.EqualsIgnoreCase(value);
            });
        }

        internal static bool ContainsIgnoreCase(this IEnumerable<PSObject> collection, PSObject value)
        {
            if (collection == null)
            {
                return false;
            }

            return collection.Any(each =>
            {
                var o = each.BaseObject as PSVariable;
                var v = value.BaseObject as PSVariable;
                return (o != null) && (o.Name != null) && (v != null) && o.Name.EqualsIgnoreCase(v.Name);
            });
        }

        internal static string StringFormat(this string formatString, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return formatString;
            }

            // if it doesn't look like we have the correct number of parameters
            // let's fix the format string.
            var c = formatString.ToCharArray().Where(each => each == '{').Count();
            if (c != args.Length)
            {
                //at least don't crash the formating string.
                return args.Aggregate(formatString.Replace('{', '\u00ab').Replace('}', '\u00bb'), (current, arg) => current + string.Format(CultureInfo.CurrentCulture, " \u00ab{0}\u00bb", arg));
            }

            return string.Format(CultureInfo.CurrentCulture, formatString, args);
        }

        internal static string TrimDrive(this string path, SHiPSDrive drive)
        {
            if (string.IsNullOrWhiteSpace(path) || drive == null) { return path;}

            //Donot strip end slash here before the match. Otherwise, we cannot make match to work and 'dir' will show nothing.
            var path1 = path;

            //match our current drive style like JT:\\  or C:\foo.ps1:\\  wheren 'C:\foo.ps1' is a drive root?
            var match = drive.DriveTrimRegex.Match(path1);
            if (match.Success)
            {
                path1 = match.Groups[2].Value;
            }
            else
            {
                // match regular drive style like E:\ or JT:?
                // This is needed when cd MyProvider\MyProvider::JT:\A\B\C
                // cd .. to B; cd .. to A; and then cd tab, show A
                var match3 = SHiPSProvider.WellFormatPathRegex.Match(path1);
                if (match3.Success)
                {
                    path1 = match3.Groups[2].Value;
                }
            }

            return path1.TrimEnd('\\', '/');
        }

        internal static object HomePath(this string name, SHiPSProvider provider)
        {
            if(string.IsNullOrWhiteSpace(name) || provider == null) { return null;}

            //TODO: We should change the following to RuntimeInformation.IsOSPlatform to optimize the operation
            // when we move to .netstandard build (netcoreapp 2.0 and .net 4.7.1)
            var command = "Get-Variable {0}".StringFormat(name);
            var varObject = provider.SessionState.InvokeCommand.InvokeScript(command, null).FirstOrDefault();
            return (varObject?.BaseObject as PSVariable)?.Value;
        }
    }

    internal static class CollectionExtensions
    {
        internal static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null ? Enumerable.Empty<T>() : enumerable.Where(each => (object)each != null);
        }
    }

    internal static class PsObjectExtensions
    {
        internal static bool NeedRefresh(this IProviderContext context, SHiPSDirectory node, SHiPSDrive drive)
        {
            var myInvocationInfo = GetMyInvocationInfo(context.CmdletProvider);
            var verbose = false;
            var debug = false;
            var force = false;
            var usedDynamicParameter = false;

            if (myInvocationInfo == null) { return false;}

            var boundParameters =  myInvocationInfo.BoundParameters;
            if (boundParameters.Any())
            {
                object argument;

                if (boundParameters.TryGetValue("Force", out argument))
                {
                    force = ((SwitchParameter)argument).IsPresent;
                }
                if (!force && boundParameters.TryGetValue("Verbose", out argument))
                {
                    verbose = ((SwitchParameter)argument).IsPresent;
                }
                if (!verbose && !force && boundParameters.TryGetValue("Debug", out argument))
                {
                    debug = ((SwitchParameter)argument).IsPresent;
                }
                if (!verbose && !force && !debug)
                {
                    usedDynamicParameter = UsingDynamicParameter(boundParameters, node, drive);
                }
            }

            // If a user specify -force, -debug -verbose -filter or dynamic parameters, we do not use cache.
            return force || debug || verbose || usedDynamicParameter;
        }


        internal static bool UsingDynamicParameter(this Dictionary<string, object> boundParameters, SHiPSDirectory node, SHiPSDrive drive)
        {
            if (node.SHiPSProviderContext.DynamicParameters == null) {return false;}

            var usingDynamicParameter = false;
            var filter = false;

            if (boundParameters != null && boundParameters.Any())
            {
                var defaultParameters = drive.GetChildItemDefaultParameters;
                usingDynamicParameter = boundParameters.Keys.Any(each => !defaultParameters.ContainsIgnoreCase(each));

                object argument;
                if (!usingDynamicParameter && boundParameters.TryGetValue("Filter", out argument))
                {
                    filter = !string.IsNullOrWhiteSpace((string) argument);
                }
            }

            return usingDynamicParameter || filter;
        }

        internal static bool IsProviderDefinedCommand(this IProviderContext context)
        {
            var myInvocationInfo = GetMyInvocationInfo(context.CmdletProvider);
            return myInvocationInfo?.MyCommand != null && Constants.DefinedCommands.ContainsIgnoreCase(myInvocationInfo.MyCommand.Name);
        }

        internal static SHiPSParameters GetSHiPSParameters(this IProviderContext context)
        {
            var myInvocationInfo = GetMyInvocationInfo(context.CmdletProvider);
            var verbose = false;
            var debug = false;
            var force = false;

            if (myInvocationInfo == null)
            {
                return new SHiPSParameters();
            }

            var boundParameters = myInvocationInfo.BoundParameters;

            if (boundParameters.Any())
            {
                object argument;

                if (boundParameters.TryGetValue("Verbose", out argument))
                {
                    verbose = ((SwitchParameter) argument).IsPresent;
                }
                if (boundParameters.TryGetValue("Debug", out argument))
                {
                    debug = ((SwitchParameter) argument).IsPresent;
                }
                if (boundParameters.TryGetValue("Force", out argument))
                {
                    force = ((SwitchParameter) argument).IsPresent;
                }
            }

            var parameterBag = new SHiPSParameters()
            {
                Verbose = verbose,
                Debug = debug,
                Force = force,
                BoundParameters = myInvocationInfo.BoundParameters
            };

            return parameterBag;
        }

        internal static IPathNode AddAsChildNode(this SHiPSDirectory parent, object child, SHiPSDrive drive, bool addNodeOnly, List<IPathNode> list)
        {
            var pNode = GetChildPNode(parent, child, drive, addNodeOnly, cache: true);

            if (pNode == null) { return null;}

            //add to its child list
            parent.Children.GetOrCreateEntryIfDefault(pNode.Name, () => new List<IPathNode>()).Add(pNode);
            list.Add(pNode);
            return pNode;
        }

        internal static IPathNode GetChildPNode(this SHiPSDirectory parent, object child, SHiPSDrive drive, bool addNodeOnly, bool cache)
        {
            if (child == null || parent == null) return null;

            var pNode = child.ToPathNode(drive, parent);
            if (pNode != null)
            {
                if (string.IsNullOrWhiteSpace(pNode.Name))
                {
                    //let the program continue by logging warning only because one bad node should not stop the navigation.
                    drive.SHiPS.WriteWarning(Resources.Resource.NameWithNullOrEmpty.StringFormat(parent.Name));
                   // throw new InvalidDataException(Resources.Resource.NameWithNullOrEmpty.StringFormat(parent.Name));
                    return null;
                }

                if (!cache)
                {
                    return pNode;
                }
                //warning if nodes have the same name because "directory" cannot be the same name
                var existNode = parent.Children.Get(pNode.Name);
                var first = existNode?.FirstOrDefault();
                if (first != null)
                {
                    if (addNodeOnly)
                    {
                        // replace the existing node
                        parent.Children.RemoveSafe(pNode.Name);
                        return pNode;
                    }
                    if (first.GetNodeValue().IsCollection ||
                         (pNode is ContainerNodeService &&
                          !((ContainerNodeService) pNode).ContainerNode.IsLeaf))
                    {
                        drive.SHiPS.WriteWarning(
                            Resources.Resource.NodesWithSameName.StringFormat(
                                parent.Name.EqualsIgnoreCase(drive.RootNode.Name) ? "root" : parent.Name, pNode.Name));
                        return null;
                    }
                }

                return pNode;
            }

            return null;
        }

        private static InvocationInfo GetMyInvocationInfo(CmdletProvider provider)
        {
            var context = GetProperty(provider, typeof(CmdletProvider), "Context");
            var t = context.GetType();

            var obj = GetProperty(context, t, "MyInvocation");
            return obj as InvocationInfo;

        }

        //The object whose property value will be returned.
        private static object GetProperty(object objContainProperty, Type typeOfObject, string propertyName)
        {
            PropertyInfo mi = typeOfObject.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);

            return mi.GetValue(objContainProperty, null);
        }

        internal static void ReportError(this IProviderContext context, string errorId, string errorMessage, ErrorCategory errorCategory, object targetobject)
        {
            context.WriteError(new ErrorRecord(new Exception(errorMessage), errorId,  errorCategory, targetobject));
        }

        internal static PSModuleInfo ImportModule(this System.Management.Automation.PowerShell powershell, string name, bool force = false)
        {
            if (powershell != null)
            {
                powershell.Clear().AddCommand("Import-Module");
                powershell.AddParameter("Name", name);
                powershell.AddParameter("PassThru");

                if (force)
                {
                    powershell.AddParameter("Force");
                }
                return powershell.Invoke<PSModuleInfo>().ToArray().FirstOrDefault();
            }
            return null;
        }

        internal static System.Management.Automation.PowerShell Clear(this System.Management.Automation.PowerShell powershell)
        {
            if (powershell != null)
            {
                powershell.WaitForReady();
                powershell.Commands = new PSCommand();
            }
            return powershell;
        }
        internal static System.Management.Automation.PowerShell WaitForReady(this System.Management.Automation.PowerShell powershell)
        {
            if (powershell == null) return powershell;

            switch (powershell.InvocationStateInfo.State)
            {
                case PSInvocationState.Stopping:
                    while (powershell.InvocationStateInfo.State == PSInvocationState.Stopping)
                    {
                        Thread.Sleep(10);
                    }
                    break;

                case PSInvocationState.Running:
                    powershell.Stop();
                    while (powershell.InvocationStateInfo.State == PSInvocationState.Stopping)
                    {
                        Thread.Sleep(10);
                    }
                    break;

                case PSInvocationState.Failed:
                case PSInvocationState.Completed:
                case PSInvocationState.Stopped:
                case PSInvocationState.NotStarted:
                case PSInvocationState.Disconnected:
                    break;
            }

            return powershell;
        }
        internal static T SafeGetPropertyValue<T>(this PSObject pso, string name, T defaultValue)
        {
            return pso.SafeGetPropertyValue(name, () => defaultValue);
        }

        internal static T SafeGetPropertyValue<T>(this PSObject pso, string name, Func<T> defaultValue)
        {
            T t = default(T);
            if (null == pso)
            {
                return defaultValue();
            }
            try
            {
                var m = pso.Properties.Match(name);
                if (0 == m.Count)
                {
                    return defaultValue();
                }

                var value = m[0].Value;

                try
                {
                    t = (T)value;
                }
                catch (InvalidCastException)
                {
                    t = defaultValue();
                }

                return t;
            }
            catch
            {
                return defaultValue();
            }
        }

        internal static T SafeGetPropertyValue<T>(this PSObject pso, string name)
        {
            return pso.SafeGetPropertyValue<T>(name, default(T));
        }


        internal static IPathNode ToPathNode(this object input, SHiPSDrive drive, SHiPSDirectory parent)
        {
            if (input == null)
            {
                return null;
            }
            if (input is SHiPSDirectory)
            {
                return new ContainerNodeService(drive, input, parent);
            }

            if (input is SHiPSLeaf)
            {
                return new LeafNodeService(input, drive, parent);
            }

            // get or create a psobject on input
            var psobject = PSObject.AsPSObject(input);
            if (psobject == null) { return null;}

            if (psobject.ImmediateBaseObject is SHiPSDirectory)
            {
                return new ContainerNodeService(drive, psobject.ImmediateBaseObject, parent);
            }

            if (psobject.ImmediateBaseObject is SHiPSLeaf)
            {
                return new LeafNodeService(psobject.ImmediateBaseObject, drive, parent);
            }

            return new PSObjectNodeService(psobject);
        }

        internal static SHiPSDirectory ToNode(this PSObject input)
        {

            if (input.BaseObject is SHiPSDirectory)
            {
                return (SHiPSDirectory) input.BaseObject;
            }

            return null;
        }
    }

    internal static class PathExtensions
    {
        internal static T Pop<T>(this IList<T> items)
        {
            if (null == items || items.Count == 0)
            {
                return default(T);
            }

            T item = items.Last();
            items.RemoveAt(items.Count - 1);
            return item;
        }

    }

    internal static class DictionaryExtensions
    {
        internal static TValue AddOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }
            return value;
        }

        internal static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default(TValue);
        }

        internal static TValue GetOrCreateEntryIfDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFunction)
        {
            lock (dictionary)
            {
                return dictionary.ContainsKey(key) ? dictionary[key] : dictionary.AddOrSet(key, valueFunction());
            }
        }

        internal static void RemoveSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary.Remove(key);
                }
            }
        }
    }
}
