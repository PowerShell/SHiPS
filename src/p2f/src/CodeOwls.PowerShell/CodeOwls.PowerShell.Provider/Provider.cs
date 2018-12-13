/*
	Copyright (c) 2014 Code Owls LLC

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to
	deal in the Software without restriction, including without limitation the
	rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
	sell copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
	IN THE SOFTWARE.
*/




using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Paths.Exceptions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.Attributes;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;


namespace CodeOwls.PowerShell.Provider
{
    //[CmdletProvider("YourProviderName", ProviderCapabilities.ShouldProcess)]

    public abstract class Provider : NavigationCmdletProvider,
        IPropertyCmdletProvider,
        ICmdletProviderSupportsHelp,
        IContentCmdletProvider
    {
        private static readonly Dictionary<string, Regex> FilterRegexMap = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);

        internal Drive DefaultDrive
        {
            get
            {
                var drive = PSDriveInfo as Drive;

                if (null == drive)
                {
                    drive = ProviderInfo.Drives.FirstOrDefault() as Drive;
                }

                return drive;
            }
        }

        internal Drive GetDriveForPath( string path )
        {
            var name = Drive.GetDriveName(path);
            return (from drive in ProviderInfo.Drives
                    where StringComparer.OrdinalIgnoreCase.Equals(drive.Name, name)
                    select drive).FirstOrDefault() as Drive;
        }

        protected abstract IPathResolver PathResolver { get; }

        /// <summary>
        /// When multiple drives are registered with the same provider and a user 'cd' among them, ProviderInfo.Drives will
        /// contains their corresponding dirves.
        /// In addition, when a user types "cd FooBarProvider\FooBarProvider::Test\, PSDriveInfo as FooBarProvider can be null.
        /// Thus the derived class need to get the right drive object from ProviderInfo.Drives based on given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected virtual IPathResolver PathResolver2(string path)
        {
            return null;
        }

        IEnumerable<IPathNode> ResolvePath( string path )
        {
            path = EnsurePathIsRooted(path);
            var pathResolver = PathResolver ?? PathResolver2(path);
            return pathResolver != null ? pathResolver.ResolvePath(CreateContext(path), path) : Enumerable.Empty<IPathNode>();
        }

        protected string EnsurePathIsRooted(string path)
        {
            if (null != PSDriveInfo &&
                !String.IsNullOrEmpty(PSDriveInfo.Root))
            {
                if (!path.StartsWith(PSDriveInfo.Root))
                {
                    path = Path.Combine(PSDriveInfo.Root, path);
                }
            }

            return path;
        }

        protected virtual IProviderContext CreateContext( string path )
        {
            return CreateContext(path, false, true);
        }

        protected virtual IProviderContext CreateContext(string path, bool recurse )
        {
            return CreateContext(path, recurse, false );
        }

        protected virtual IProviderContext CreateContext(string path, bool recurse, bool resolveFinalNodeFilterItems)
        {
            var context = new ProviderContext(this, path, PSDriveInfo, PathResolver ?? PathResolver2(path), DynamicParameters,recurse);
            context.ResolveFinalNodeFilterItems = resolveFinalNodeFilterItems;
            return context;
        }

        #region Implementation of IPropertyCmdletProvider

        public void GetProperty(string path, Collection<string> providerSpecificPickList)
        {
            Action a = ()=> DoGetProperty(path, providerSpecificPickList);
            ExecuteAndLog(a, "GetProperty", path, providerSpecificPickList.ToArgList());
        }

        private void DoGetProperty(string path, Collection<string> providerSpecificPickList)
        {
            var factories = GetNodeFactoryFromPath(path);
            factories.ToList().ForEach(f => GetProperty(path, f, providerSpecificPickList));
        }

        void GetProperty(string path, IPathNode factory, Collection<string> providerSpecificPickList)
        {
            var node = factory.GetNodeValue();
            PSObject values = null;

            if (null == providerSpecificPickList || 0 == providerSpecificPickList.Count)
            {
                values = PSObject.AsPSObject(node.Item);
            }
            else
            {
                values = new PSObject();
                var value = node.Item;
                var propDescs = TypeDescriptor.GetProperties(value);
                var props = (from PropertyDescriptor prop in propDescs
                             where (providerSpecificPickList.Contains(prop.Name,
                                                                      StringComparer.OrdinalIgnoreCase))
                             select prop);

                props.ToList().ForEach(p =>
                                           {
                                               var iv = p.GetValue(value);
                                               if (null != iv)
                                               {
                                                   values.Properties.Add(new PSNoteProperty(p.Name, p.GetValue(value)));
                                               }
                                           });
            }
            WritePropertyObject(values, path);
        }

        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
        {
            return null;
        }

        public void SetProperty(string path, PSObject propertyValue)
        {
            Action a = () => DoSetProperty(path, propertyValue);
            ExecuteAndLog( a, "SetProperty", path, propertyValue.ToArgString() );
        }

        private void DoSetProperty(string path, PSObject propertyValue)
        {
            var factories = GetNodeFactoryFromPath(path);
            factories.ToList().ForEach(f => SetProperty(path, f, propertyValue));
        }

        void SetProperty(string path, IPathNode factory, PSObject propertyValue)
        {
            var node = factory.GetNodeValue();
            var value = node.Item;
            var propDescs = TypeDescriptor.GetProperties(value);
            var psoDesc = propertyValue.Properties;
            var props = (from PropertyDescriptor prop in propDescs
                         let psod = (from pso in psoDesc
                                     where StringComparer.OrdinalIgnoreCase.Equals(pso.Name, prop.Name)
                                     select pso).FirstOrDefault()
                         where null != psod
                         select new {PSProperty = psod, Property = prop});


            props.ToList().ForEach(p => p.Property.SetValue(value, p.PSProperty.Value));
        }

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
        {
            return null;
        }

        public void ClearProperty(string path, Collection<string> propertyToClear)
        {
            WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.ClearItemProperty, ClearItemPropertyNotsupportedErrorID);
        }

        public object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
        {
            return null;
        }

        #endregion


        #region Implementation of ICmdletProviderSupportsHelp

        public string GetHelpMaml(string helpItemName, string path)
        {
            Func<string> a = ()=>DoGetHelpMaml(helpItemName, path);
            return ExecuteAndLog(a, "GetHelpMaml", helpItemName, path);
        }

        private string DoGetHelpMaml(string helpItemName, string path)
        {
            if (String.IsNullOrEmpty(helpItemName) || String.IsNullOrEmpty(path))
            {
                return String.Empty;
            }

            var parts = helpItemName.Split(new[] {'-'});
            if (2 != parts.Length)
            {
                return String.Empty;
            }

            var nodeFactory = GetFirstNodeFactoryFromPath(path);
            if (null == nodeFactory)
            {
                return String.Empty;
            }

            XmlDocument document = new XmlDocument();

            string filename = GetExistingHelpDocumentFilename();

            if (String.IsNullOrEmpty(filename)
                || !File.Exists(filename))
            {
                return String.Empty;
            }

            try
            {
                //XmlDocument.Load(string) is not supported in CoreCRL. Changed it to load from XmlReader.
                var reader = XmlReader.Create(filename, new XmlReaderSettings());
                document.Load(reader);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(
                               new MamlHelpDocumentExistsButCannotBeLoadedException(filename, e),
                               GetHelpCustomMamlErrorID,
                               ErrorCategory.ParserError,
                               filename
                               ));

                return string.Empty;
            }

            List<string> keys = GetCmdletHelpKeysForNodeFactory(nodeFactory);

            string verb = parts[0];
            string noun = parts[1];
            string maml = (from key in keys
                           let m = GetHelpMaml(document, key, verb, noun)
                           where !String.IsNullOrEmpty(m)
                           select m).FirstOrDefault();

            if (String.IsNullOrEmpty(maml))
            {
                maml = GetHelpMaml(document, NotSupportedCmdletHelpID, verb, noun);
            }
            return maml ?? String.Empty;
        }

        private List<string> GetCmdletHelpKeysForNodeFactory(IPathNode pathNode)
        {
            var nodeFactoryType = pathNode.GetType();
            var idsFromAttributes =
                from CmdletHelpPathIDAttribute attr in
                    nodeFactoryType.GetTypeInfo().GetCustomAttributes(typeof (CmdletHelpPathIDAttribute), true)
                select attr.ID;

            List<string> keys = new List<string>(idsFromAttributes);
            keys.AddRange(new[]
                              {
                                  nodeFactoryType.FullName,
                                  nodeFactoryType.Name,
                                  nodeFactoryType.Name.Replace("NodeFactory", ""),
                              });
            return keys;
        }

        private string GetExistingHelpDocumentFilename()
        {
            CultureInfo currentUICulture = Host.CurrentUICulture;
            string moduleLocation = this.ProviderInfo.Module.ModuleBase;
            string filename = null;
            while (currentUICulture != null && currentUICulture != currentUICulture.Parent)
            {
                string helpFilePath = GetHelpPathForCultureUI(currentUICulture.Name, moduleLocation);

                if (File.Exists(helpFilePath))
                {
                    filename = helpFilePath;
                    break;
                }
                currentUICulture = currentUICulture.Parent;
            }

            if (String.IsNullOrEmpty(filename))
            {
                string helpFilePath = GetHelpPathForCultureUI("en-US", moduleLocation);

                if (File.Exists(helpFilePath))
                {
                    filename = helpFilePath;
                }
            }

            LogDebug( "Existing help document filename: {0}", filename);
            return filename;
        }

        private string GetHelpPathForCultureUI(string cultureName, string moduleLocation)
        {
            string documentationDirectory = Path.Combine(moduleLocation, cultureName);
            var path = Path.Combine(documentationDirectory, ProviderInfo.HelpFile);

            return path;
        }

        private string GetHelpMaml(XmlDocument document, string key, string verb, string noun)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("cmd", "http://schemas.microsoft.com/maml/dev/command/2004/10");

            string xpath = String.Format(
                "/helpItems/providerHelp/CmdletHelpPaths/CmdletHelpPath[@ID='{0}']/cmd:command[ ./cmd:details[ (cmd:verb='{1}') and (cmd:noun='{2}') ] ]",
                key,
                verb,
                noun);

            XmlNode node = null;
            try
            {
                node = document.SelectSingleNode(xpath, nsmgr);
            }
            catch (XPathException)
            {
                return string.Empty;
            }

            if (node == null)
            {
                return String.Empty;
            }

            return node.OuterXml;
        }

        #endregion

        private void WriteCmdletNotSupportedAtNodeError(string path, string cmdlet, string errorId)
        {
            var exception = new NodeDoesNotSupportCmdletException(path, cmdlet);
            var error = new ErrorRecord(exception, errorId, ErrorCategory.NotImplemented, path);
            WriteError(error);
        }

        private void WriteGeneralCmdletError(Exception exception, string errorId, string path)
        {
            WriteError(
                new ErrorRecord(
                    exception,
                    errorId,
                    ErrorCategory.NotSpecified,
                    path
                    ));
        }

        protected override bool IsItemContainer(string path)
        {
            Func<bool> a = ()=> DoIsItemContainer(path);
            return ExecuteAndLog(a, "IsItemContainer", path);
        }

        private bool DoIsItemContainer(string path)
        {
            if (IsRootPath(path))
            {
                return true;
            }

            var node = GetFirstNodeFactoryFromPath(path);
            if (null == node)
            {
                return false;
            }

            var value = node.GetNodeValue();

            if (null == value)
            {
                return false;
            }

            return value.IsCollection;
        }

        protected override object MoveItemDynamicParameters(string path, string destination)
        {
            return null;
        }

        protected override void MoveItem(string path, string destination)
        {
            Action a = ()=>DoMoveItem(path, destination);
            ExecuteAndLog(a, "MoveItem", path, destination);
        }

        private void DoMoveItem(string path, string destination)
        {
            var sourceNodes = GetNodeFactoryFromPath(path);
            sourceNodes.ToList().ForEach(n => MoveItem(path, n, destination));
        }

        void MoveItem(string path, IPathNode sourcePathNode, string destination)
        {
            var copy = GetCopyItem(sourcePathNode);
            var move = sourcePathNode as IMoveItem;
            var remove = copy as IRemoveItem;
            if (null == move && (null == copy || null == remove))
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.MoveItem, MoveItemNotSupportedErrorID);
                return;
            }

            if (!ShouldProcess(path, ProviderCmdlet.MoveItem ))
            {
                return;
            }

            try
            {
                if (null != move)
                {
                    DoMoveItem(path, destination, move);
                }
                else
                {
                    DoCopyItem(path, destination, true, copy);
                    DoRemoveItem(path, true, remove);
                }
            }
            catch( Exception e )
            {
                WriteGeneralCmdletError( e, MoveItemInvokeErrorID, path);
            }
        }

        private void DoMoveItem(string path, string destinationPath, IMoveItem moveItem)
        {
            bool targetNodeIsParentNode;
            var targetNodes = GetNodeFactoryFromPathOrParent(destinationPath, out targetNodeIsParentNode);
            var targetNode = targetNodes.FirstOrDefault();

            var sourceName = GetChildName(path);
            var finalDestinationPath = targetNodeIsParentNode ? GetChildName(destinationPath) : null;

            if (null == targetNode)
            {
                WriteError(new ErrorRecord(new CopyOrMoveToNonexistentContainerException(destinationPath),
                    CopyItemDestinationContainerDoesNotExistErrorID, ErrorCategory.WriteError, destinationPath));
                return;
            }

            var movedItem = moveItem.MoveItem(CreateContext(path), sourceName, finalDestinationPath, targetNode.GetNodeValue());
            if (null != movedItem)
            {
                WritePathNode(destinationPath, movedItem);
            }
        }

        protected override string MakePath(string parent, string child)
        {
            Func<string> a = ()=> DoMakePath(parent, child);
            return ExecuteAndLog(a, "MakePath", parent, child);
        }

        private string DoMakePath(string parent, string child)
        {
            //trim the end slash for the consistent experience to other built-in providers such as FileSystem.
            //Show: drive:\A\B\C>
            //Not:  drive:\A\B\C\>
            var newChild = child.TrimEnd('/', '\\');
            var newPath = base.MakePath(parent, newChild);
            return newPath;
        }

        protected override string GetParentPath(string path, string root)
        {
            Func<string> a = ()=>DoGetParentPath(path, root);
            return ExecuteAndLog(a, "GetParentPath", path, root);
        }

        private string DoGetParentPath(string path, string root)
        {
            if (! path.Any())
            {
                return path;
            }

            path = base.GetParentPath(path, root);
            return path;
        }

        protected override string NormalizeRelativePath(string path, string basePath)
        {

            Func<string> a = () => base.NormalizeRelativePath(path, basePath);
            return ExecuteAndLog(a, "NormalizeRelativePath", path, basePath);
        }

        protected override string GetChildName(string path)
        {
            Func<string> a = ()=>DoGetChildName(path);
            return ExecuteAndLog(a, "GetChildName", path);
        }

        private string DoGetChildName(string path)
        {
            return path.Split(Path.DirectorySeparatorChar).Last();
        }

        protected void GetItem( string path, IPathNode factory )
        {
            try
            {
                WritePathNode(path, factory);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, GetItemInvokeErrorID, path);
            }
        }
        protected override void GetItem(string path)
        {
            Action a = ()=>DoGetItem(path);
            ExecuteAndLog(a, "GetItem", path);
        }

        private void DoGetItem(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                return;
            }

            factories.ToList().ForEach(f => GetItem(path, f));
            FilterRegexMap.Clear();
        }

        void SetItem( string path, IPathNode factory, object value )
        {
            var @set = factory as ISetItem;
            if (null == factory || null == @set)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.SetItem, SetItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;
            path = GetChildName(path);

            if (!ShouldProcess(fullPath, ProviderCmdlet.SetItem))
            {
                return;
            }

            try
            {
                var result = @set.SetItem(CreateContext(fullPath), path, value);
                if (null != result)
                {
                    WritePathNode(fullPath, result);
                }
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, SetItemInvokeErrorID, fullPath);
            }
        }

        protected override void SetItem(string path, object value)
        {
            Action a = ()=>DoSetItem(path, value);
            ExecuteAndLog(a, "SetItem", path, value.ToArgString());
        }

        private void DoSetItem(string path, object value)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                WriteError(
                    new ErrorRecord(
                        new ItemNotFoundException(path),
                        SetItemTargetDoesNotExistErrorID,
                        ErrorCategory.ObjectNotFound,
                        path
                        )
                    );
                return;
            }

            factories.ToList().ForEach(f => SetItem(path, f, value));
        }

        protected override object SetItemDynamicParameters(string path, object value)
        {
            Func<object> a = ()=>DoSetItemDynamicParameters(path);
            return ExecuteAndLog(a, "SetItemDynamicParameters", path, value.ToArgString());
        }

        private object DoSetItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var @set = factory as ISetItem;
            if (null == factory || null == @set)
            {
                return null;
            }

            return @set.SetItemParameters;
        }

        protected override object ClearItemDynamicParameters(string path)
        {
            Func<object> a = ()=> DoClearItemDynamicParameters(path);
            return ExecuteAndLog(a, "ClearItemDynamicParameters", path);
        }

        private object DoClearItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var clear = factory as IClearItem;
            if (null == factory || null == clear)
            {
                return null;
            }

            return clear.ClearItemDynamicParamters;
        }

        protected override void ClearItem(string path)
        {
            Action a = ()=>DoClearItem(path);
            ExecuteAndLog(a, "ClearItem", path);
        }

        private void DoClearItem(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                return;
            }

            factories.ToList().ForEach(f => ClearItem(path, f));
        }

        void ClearItem( string path, IPathNode factory )
        {
            var clear = factory as IClearItem;
            if (null == factory || null == clear)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.ClearItem, ClearItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;
            path = GetChildName(path);

            if (!ShouldProcess(fullPath, ProviderCmdlet.ClearItem))
            {
                return;
            }

            try
            {
                clear.ClearItem(CreateContext(fullPath), path);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, ClearItemInvokeErrorID, fullPath);
            }
        }

        private bool ForceOrShouldContinue(string itemName, string fullPath, string op)
        {
            if (Force || !ShouldContinue(ShouldContinuePrompt, String.Format("{2} {0} ({1})", itemName, fullPath, op)))
            {
                return false;
            }
            return true;
        }

        protected override object InvokeDefaultActionDynamicParameters(string path)
        {
            Func<object> a = ()=>DoInvokeDefaultActionDynamicParameters(path);
            return ExecuteAndLog(a, "InvokeDefaultActionDynamicParameters", path);
        }

        private object DoInvokeDefaultActionDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var invoke = factory as IInvokeItem;
            if (null == factory || null == invoke)
            {
                return null;
            }

            return invoke.InvokeItemParameters;
        }

        protected override void InvokeDefaultAction(string path)
        {
            Action a = ()=>DoInvokeDefaultAction(path);
            ExecuteAndLog(a, "InvokeDefaultAction", path);
        }

        private void DoInvokeDefaultAction(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                return;
            }

            factories.ToList().ForEach(f => InvokeDefaultAction(path, f));
        }

        void InvokeDefaultAction( string path, IPathNode factory )
        {
            var invoke = factory as IInvokeItem;
            if (null == factory || null == invoke)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.InvokeItem, InvokeItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;

            if (!ShouldProcess(fullPath, ProviderCmdlet.InvokeItem))
            {
                return;
            }

            path = GetChildName(path);
            try
            {
                var results = invoke.InvokeItem(CreateContext(fullPath), path);
                if (null == results)
                {
                    return;
                }

                // TODO: determine what exactly to return here
                //  http://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=1&ved=0CCAQFjAA&url=http%3A%2F%2Fmsdn.microsoft.com%2Fen-us%2Flibrary%2Fsystem.management.automation.provider.itemcmdletprovider.invokedefaultaction(v%3Dvs.85).aspx&ei=28vLTpyrJ42utwfUo6WYAQ&usg=AFQjCNFto_ye_BBjxxWfzBFGfNxw3eEgTw
                //  docs tell me to return the item being invoked... but I'm not sure.
                //  is there any way for the target of the invoke to return data to the runspace??
                //results.ToList().ForEach(r => this.WriteObject(r));
            }
            catch( Exception e )
            {
                WriteGeneralCmdletError(e, InvokeItemInvokeErrorID, fullPath);
            }
        }


        protected override bool ItemExists(string path)
        {
            Func<bool> a = ()=>DoItemExists(path);
            return ExecuteAndLog(a, "ItemExists", path);
        }

        private bool DoItemExists(string path)
        {
            if (IsRootPath(path))
            {
                return true;
            }
            // item does not exist if the path is null or empty
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            var nodes = GetNodeFactoryFromPathForItemExists(path);
            return ((null != nodes) && nodes.Any()) ;
        }
        protected virtual IEnumerable<IPathNode> GetNodeFactoryFromPathForItemExists(string path)
        {
            return GetNodeFactoryFromPath(path);
        }
        protected override bool IsValidPath(string path)
        {
            return ExecuteAndLog(() => true, "IsValidPath", path);
        }

#if PS3
        protected void GetChildItems(string path, bool recurse, uint depth)
#else
        protected override void GetChildItems(string path, bool recurse, uint depth)
#endif
        {
            Action a= ()=>DoGetChildItems(path, recurse, depth);
            ExecuteAndLog(a, "GetChildItems", path, recurse.ToString());
        }

        private void DoGetChildItems(string path, bool recurse, uint depth)
        {
            var nodeFactory = GetNodeFactoryFromPath(path, false);
            if (null == nodeFactory)
            {
                return;
            }

            nodeFactory.ToList().ForEach(f => GetChildItems(path, f, recurse, depth));
            FilterRegexMap.Clear();
        }

        void GetChildItems(string path, IPathNode pathNode, bool recurse, uint depth)
        {
            var context = CreateContext(path, recurse );
            var children = pathNode.GetNodeChildren(context);
            WriteChildItem(path, recurse, depth, children);
        }

        private void WriteChildItem(string path, bool recurse, uint depth, IEnumerable<IPathNode> children)
        {
            if (null == children )
            {
                return;
            }

            var exit = false;
            children.ToList().ForEach(
                f =>
                    {
                        try
                        {
                            // Making sure to obey the StopProcessing
                            if (Stopping || exit)
                            {
                                return;
                            }

                            var i = f.GetNodeValue();
                            if (null == i)
                            {
                                return;
                            }
                            var childPath = MakePath(path, i.Name);
                            WritePathNode(childPath, f);
                            if (recurse)
                            {
                                // Limiter for recursion. E.g., dir -depth 2
                                if (depth > 0)
                                {
                                    var context = CreateContext(childPath, recurse);
                                    var kids = f.GetNodeChildren(context);
                                    WriteChildItem(childPath, recurse, depth - 1, kids);
                                }
                            }
                        }
                        catch (PipelineStoppedException )
                        {
                            // For a case like 'dir | select -First 3', we can get PipelineStoppedException
                            // from the PowerShell engine. That's ok because it just means that we're done.
                            // Note that in a provider, by swallowing the PipelineStoppedException exception may
                            // cause the PowerShell instance exit early without properly rendering outputs.
                            // Also we should not call PowerShell APIs such as WriteError when receiving PipelineStoppedException.
                            // This is because the PowerShell may throw a new PipelineStoppedException exception directly to a user.

                            // Refer to https://msdn.microsoft.com/en-us/library/system.management.automation.pipelinestoppedexception(v=vs.85).aspx
                            // cmdlets or providers ... allow this exception to propagate up, and the Windows PowerShell
                            // runtime will catch the exception.
                            exit = true;
                            throw;
                        }
                        catch (Exception e)
                        {
                            WriteWarning(e.Message);
                        }
                    });
        }


        private bool IsRootPath(string path)
        {
            //
            // e.g., MyProvider\MyProvider::JT:\
            // cd ..
            //should return false if the path is null or empty
            if (string.IsNullOrWhiteSpace(path))  { return false; }
            path = Regex.Replace(path.ToLower(), @"[a-z0-9_]+:[/\\]?", "");
            return String.IsNullOrEmpty(path);
        }

        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            Func<object> a = ()=> DoGetChildItemsDynamicParameters(path);
            return ExecuteAndLog(a, "GetChildItemsDynamicParameters", path, recurse.ToString());
        }

        private object DoGetChildItemsDynamicParameters(string path)
        {
            IPathNode pathNode = GetFirstNodeFactoryFromPath(path);
            if (null == pathNode)
            {
                return null;
            }

            return pathNode.GetNodeChildrenParameters;
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            Action a = ()=>DoGetChildNames(path, returnContainers);
            ExecuteAndLog(a, "GetChildNames", path, returnContainers.ToString());
        }

        private void DoGetChildNames(string path, ReturnContainers returnContainers)
        {
            var nodeFactory = GetNodeFactoryFromPath(path, false);
            if (null == nodeFactory)
            {
                return;
            }

            nodeFactory.ToList().ForEach(f => GetChildNames(path, f, returnContainers));
        }

        void GetChildNames(string path, IPathNode pathNode, ReturnContainers returnContainers)
        {
            //removed ToList() so that it can be async. Therefore results can be shown up to a user sooner.
            var pathNodes = pathNode.GetNodeChildren(CreateContext(path));
            if (pathNodes == null) return;

            GetChildNames(path, pathNodes);
        }

        protected void GetChildNames(string path, IEnumerable<IPathNode> pathNodes)
        {
            foreach (var node in pathNodes)
            {
                var i = node.GetNodeValue();
                if (null == i)
                {
                    return;
                }

                WriteItemObject(i.Name, Path.Combine(path, i.Name), i.IsCollection);
            }
        }

        protected override object GetChildNamesDynamicParameters(string path)
        {
            Func<object> a=()=> DoGetChildNamesDynamicParameters(path);
            return ExecuteAndLog(a, "GetChildNamesDynamicParameters", path);
        }

        private object DoGetChildNamesDynamicParameters(string path)
        {
            IPathNode pathNode = GetFirstNodeFactoryFromPath(path);
            if (null == pathNode)
            {
                return null;
            }

            return pathNode.GetNodeChildrenParameters;
        }

        protected override void RenameItem(string path, string newName)
        {
            Action a = ()=>DoRenameItem(path, newName);
            ExecuteAndLog(a, "RenameItem", path, newName);
        }

        private void DoRenameItem(string path, string newName)
        {
            var factory = GetNodeFactoryFromPath(path);
            if (null == factory || ! factory.Any())
            {
                return;
            }

            factory.ToList().ForEach(a => RenameItem(path, newName, a));
        }

        void RenameItem(string path, string newName, IPathNode factory)
        {
            var rename = factory as IRenameItem;
            if (null == factory || null == rename)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.RenameItem, RenameItemNotsupportedErrorID);
                return;
            }

            var fullPath = path;

            if (!ShouldProcess(fullPath, ProviderCmdlet.RenameItem))
            {
                return;
            }

            var child = GetChildName(path);
            try
            {
                rename.RenameItem(CreateContext(fullPath), child, newName);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, RenameItemInvokeErrorID, fullPath);
            }
        }

        protected override object RenameItemDynamicParameters(string path, string newName)
        {
            Func<object> a=()=> DoRenameItemDynamicParameters(path);
            return ExecuteAndLog(a, "RenameItemDynamicParameters", path);
        }

        private object DoRenameItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var rename = factory as IRenameItem;
            if (null == factory || null == rename)
            {
                return null;
            }
            return rename.RenameItemParameters;
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            Action a = ()=>DoNewItem(path, itemTypeName, newItemValue);
            ExecuteAndLog(a, "NewItem", itemTypeName, newItemValue.ToArgString());
        }

        private void DoNewItem(string path, string itemTypeName, object newItemValue)
        {
            bool isParentOfPath;
            var factories = GetNodeFactoryFromPathOrParent(path, out isParentOfPath);
            if (null == factories)
            {
                return;
            }

            factories.ToList().ForEach(f => NewItem(path, isParentOfPath, f, itemTypeName, newItemValue));
        }

        void NewItem( string path, bool isParentPathNodeFactory, IPathNode factory, string itemTypeName, object newItemValue )
        {
            var @new = factory as INewItem;
            if (null == factory || null == @new)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.NewItem, NewItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;
            var parentPath = fullPath;
            var child = isParentPathNodeFactory ? GetChildName(path) : null;
            if( null != child )
            {
                parentPath = GetParentPath(fullPath, GetRootPath());
            }

            if (!ShouldProcess(fullPath, ProviderCmdlet.NewItem))
            {
                return;
            }

            try
            {
                var item = @new.NewItem(CreateContext(fullPath), child, itemTypeName, newItemValue);
                PathValue value = item as PathValue;

                WritePathNode(parentPath, value);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, NewItemInvokeErrorID, fullPath);
            }
        }

        protected string GetRootPath()
        {
            if (null != PSDriveInfo)
            {
                return PSDriveInfo.Root;
            }
            return String.Empty;
        }

        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            Func<object> a = ()=>DoNewItemDynamicParameters(path);
            return ExecuteAndLog(a, "NewItemDynamicParameters", path, itemTypeName, newItemValue.ToArgString());
        }

        private object DoNewItemDynamicParameters(string path)
        {
            // Added conditional access because GetNodeFactoryFromPathOrParent() can be null
            var factory = GetNodeFactoryFromPathOrParent(path)?.FirstOrDefault();
            var @new = factory as INewItem;
            if (null == factory || null == @new)
            {
                return null;
            }

            return @new.NewItemParameters;
        }

        private static Regex GetRegexFromFilter(string filter)
        {
            //no -Filter is used
            if (string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }
            if (FilterRegexMap.ContainsKey(filter))
            {
                return FilterRegexMap[filter];
            }

            //Support '?' and '*', just like FileSystem provider or
            //DirectoryInfo.EnumerateDirectories https://msdn.microsoft.com/en-us/library/dd383690(v=vs.110).aspx
            var tempFilter = "^" + filter.Replace("*", ".*").Replace("?", ".?") + "$";
            var tempRegex = new Regex(tempFilter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            FilterRegexMap.Add(filter, tempRegex);

            return tempRegex;
        }

        private void WritePathNode(string nodeContainerPath, IPathNode factory)
        {
            var value = factory.GetNodeValue();
            if (null == value)
            {
                return;
            }

            //nodeContainerPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(nodeContainerPath);

            Regex regex = GetRegexFromFilter(Filter);
            if (regex != null && !regex.IsMatch(value.Name))
            {
                return;
            }
            PSObject pso = PSObject.AsPSObject(value.Item);
            pso.Properties.Add(new PSNoteProperty(ItemModePropertyName, factory.ItemMode));
            // PowerShell issue? we need to trim backslash here to make get-item .\foo\ and get-item .\foo to work
            WriteItemObject(pso, nodeContainerPath?.TrimEnd('/', '\\'), value.IsCollection);
        }

        private void WritePathNode(string nodeContainerPath, IPathValue value)
        {
            if (null != value)
            {
                WriteItemObject(value.Item, MakePath( nodeContainerPath, value.Name ), value.IsCollection);
            }
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            Action a = ()=>DoRemoveItem(path, recurse);
            ExecuteAndLog(a, "RemoveItem", path, recurse.ToString());
        }

        private void DoRemoveItem(string path, bool recurse)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                WriteError(
                 new ErrorRecord(
                     new ItemNotFoundException(path),
                     RemoveItemTargetDoesNotExistErrorID,
                     ErrorCategory.ObjectNotFound,
                     path
                    )
                );
                return;
            }
            factories.ToList().ForEach(f => RemoveItem(path, f, recurse));
        }

        void RemoveItem( string path, IPathNode factory, bool recurse)
        {
            var remove = factory as IRemoveItem;
            if (null == factory || null == remove)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.RemoveItem, RemoveItemNotSupportedErrorID);
                return;
            }

            if (!ShouldProcess(path, ProviderCmdlet.RemoveItem))
            {
                return;
            }

            try
            {
                DoRemoveItem(path, recurse, remove);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, RemoveItemInvokeErrorID, path);
            }
        }

        private void DoRemoveItem(string path, bool recurse, IRemoveItem remove)
        {
            var fullPath = path;
            path = this.GetChildName(path);
            remove.RemoveItem(CreateContext(fullPath), path, recurse);
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            Func<object> a = ()=> DoRemoveItemDynamicParameters(path);
            return ExecuteAndLog(a, "RemoveItemDynamicParameters", path, recurse.ToString());
        }

        private object DoRemoveItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var remove = factory as IRemoveItem;
            if (null == factory || null == remove)
            {
                return null;
            }

            return remove.RemoveItemParameters;
        }

        private IPathNode GetFirstNodeFactoryFromPath( string path )
        {
            var factories = GetNodeFactoryFromPath(path);
            if( null == factories )
            {
                return null;
            }

            return factories.FirstOrDefault();
        }

        IEnumerable<IPathNode> GetNodeFactoryFromPath(string path)
        {
            return GetNodeFactoryFromPath(path, true);
        }

        private IEnumerable<IPathNode> GetNodeFactoryFromPath(string path, bool resolveFinalFilter)
        {
            IEnumerable<IPathNode> factories = null;
            factories = ResolvePath(path);

            // hmm... this does not look right to me by returning the First() only.
            // For example, "dir -Filter si* -force " will not show the child items. Instead will show parent
            // nodes for m times where m= number of child nodes.
            // In addition, Resolve()/PathNodeBase.cs calls GetNodeChildren(). This can be expensive
            // because getting child items again and again.
            // Therefore moving Filter action to WritePathNode().
            //if ( resolveFinalFilter && !String.IsNullOrEmpty(Filter))
            //{
            //    factories = factories.First().Resolve(CreateContext(path), null);
            //}

            return factories;
        }

        private IEnumerable<IPathNode> GetNodeFactoryFromPathOrParent(string path)
        {
            bool r;
            return GetNodeFactoryFromPathOrParent(path, out r);
        }

        private IEnumerable<IPathNode> GetNodeFactoryFromPathOrParent(string path, out bool isParentOfPath)
        {
            isParentOfPath = false;
            IEnumerable<IPathNode> factory = null;
            factory = ResolvePath(path);

            if (null == factory || ! factory.Any())
            {
                path = GetParentPath(path, null);
                factory = ResolvePath(path);

                if (null == factory || ! factory.Any())
                {
                    return null;
                }

                isParentOfPath = true;
            }

            if (!String.IsNullOrEmpty(Filter))
            {
                factory = factory.First().Resolve(CreateContext(null), null);
            }

            return factory;
        }

        protected override bool HasChildItems(string path)
        {
            Func<bool> a = ()=> DoHasChildItems(path);
            return ExecuteAndLog(a, "HasChildItems", path);
        }

        private bool DoHasChildItems(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            if (null == factory)
            {
                return false;
            }
            var nodes = factory.GetNodeChildren(CreateContext(path));
            if (null == nodes)
            {
                return false;
            }
            return nodes.Any();
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            Action a = ()=>DoCopyItem(path, copyPath, recurse);
            ExecuteAndLog(a, "CopyItem", path, copyPath, recurse.ToString());
        }

        private void DoCopyItem(string path, string copyPath, bool recurse)
        {
            var sourceNodes = GetNodeFactoryFromPath(path);
            if (null == sourceNodes)
            {
                WriteError(
                    new ErrorRecord(
                         new ItemNotFoundException(path),
                         CopyItemSourceDoesNotExistErrorID,
                         ErrorCategory.ObjectNotFound,
                         path
                    )
                );
                return;
            }

            sourceNodes.ToList().ForEach(n => CopyItem(path, n, copyPath, recurse));
        }

        void CopyItem( string path, IPathNode sourcePathNode, string copyPath, bool recurse )
        {
            ICopyItem copyItem = GetCopyItem(sourcePathNode);
            if (null == copyItem)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.CopyItem, CopyItemNotSupportedErrorID);
                return;
            }

            if (!ShouldProcess(path, ProviderCmdlet.CopyItem ))
            {
                return;
            }

            try
            {
                IPathValue value = DoCopyItem(path, copyPath, recurse, copyItem);
                WritePathNode(copyPath, value);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, CopyItemInvokeErrorID, path);
            }
        }

        private ICopyItem GetCopyItem(IPathNode sourcePathNode)
        {
            return sourcePathNode as ICopyItem;
        }

        private IPathValue DoCopyItem(string path, string copyPath, bool recurse, ICopyItem copyItem)
        {
            bool targetNodeIsParentNode;
            var targetNodes = GetNodeFactoryFromPathOrParent(copyPath, out targetNodeIsParentNode);
            var targetNode = targetNodes.FirstOrDefault();

            var sourceName = GetChildName(path);
            var copyName = targetNodeIsParentNode ? GetChildName(copyPath) : null;

            if (null == targetNode)
            {
                WriteError(
                    new ErrorRecord(
                        new CopyOrMoveToNonexistentContainerException(copyPath),
                        CopyItemDestinationContainerDoesNotExistErrorID,
                        ErrorCategory.WriteError,
                        copyPath
                        )
                    );
                return null;
            }

            return copyItem.CopyItem(CreateContext(path), sourceName, copyName, targetNode.GetNodeValue(), recurse);
        }

        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        {
            Func<object> a = ()=> DoCopyItemDynamicParameters(path);
            return ExecuteAndLog(a, "CopyItemDynamicParameters", path, destination, recurse.ToString());
        }

        private object DoCopyItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var copy = factory as ICopyItem;
            if (null == factory || null == copy)
            {
                return null;
            }

            return copy.CopyItemParameters;
        }

        public IContentReader GetContentReader(string path)
        {
            Func<IContentReader> a = ()=> DoGetContentReader(path);
            return ExecuteAndLog(a, "GetContentReader", path);
        }

        private IContentReader DoGetContentReader(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                WriteError(
                    new ErrorRecord(
                        new ItemNotFoundException(path),
                        GetContentTargetDoesNotExistErrorID,
                        ErrorCategory.ObjectNotFound,
                        path
                    )
                );

                return null;
            }

            return GetContentReader(path, factories.FirstOrDefault(a => a is IGetItemContent));
        }

        private IContentReader GetContentReader(string path, IPathNode pathNode)
        {
            var getContentReader = pathNode as IGetItemContent;
            if( null == getContentReader )
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.GetContent, GetContentNotSupportedErrorId);
                return null;
            }

            return getContentReader.GetContentReader( CreateContext(path));
        }

        public object GetContentReaderDynamicParameters(string path)
        {
            Func<object> a=  ()=>DoGetContentReaderDynamicParameters(path);
            return ExecuteAndLog(a, "GetContentReaderDynamicParameters", path);
        }

        private object DoGetContentReaderDynamicParameters(string path)
        {
            var factories = GetFirstNodeFactoryFromPath(path);
            if (null == factories)
            {
                return null;
            }

            var getContentReader = factories as IGetItemContent;
            if (null == getContentReader)
            {
                return null;
            }

            return getContentReader.GetContentReaderDynamicParameters(CreateContext(path));
        }

        public IContentWriter GetContentWriter(string path)
        {
            Func<IContentWriter> a = () => DoGetContentWriter(path);
            return ExecuteAndLog(a, "GetContentWriter", path);

        }

        private IContentWriter DoGetContentWriter(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            var pathNodes = factories as IPathNode[] ?? factories.ToArray();
            if (!pathNodes.Any())
            {
                // For a case like Set-Content .\foo\bar\baz.ps1 where baz.ps1 possibly does not exist, so try its parent node.
                var dir = Path.GetDirectoryName(path);
                factories = GetNodeFactoryFromPath(dir);
                pathNodes = factories as IPathNode[] ?? factories.ToArray();
                if (!pathNodes.Any())
                {
                    WriteError(
                        new ErrorRecord(
                            new ItemNotFoundException(path),
                            GetContentTargetDoesNotExistErrorID,
                            ErrorCategory.ObjectNotFound,
                            path
                        )
                    );
                    return null;
                }
            }

            return GetContentWriter(path, pathNodes.FirstOrDefault(a => a is ISetItemContent));
        }

        private IContentWriter GetContentWriter(string path, IPathNode pathNode)
        {
            var getContentWriter = pathNode as ISetItemContent;
            if (null == getContentWriter)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.SetContent, SetContentNotSupportedErrorId);
                return null;
            }

            return getContentWriter.GetContentWriter(CreateContext(path));
        }

        public object GetContentWriterDynamicParameters(string path)
        {
            Func<object> a = () => DoGetContentWriterDynamicParameters(path);
            return ExecuteAndLog(a, "GetContentWriterDynamicParameters", path);
        }

        private object DoGetContentWriterDynamicParameters(string path)
        {
            var factories = GetFirstNodeFactoryFromPath(path);
            if (null == factories)
            {
                return null;
            }

            var getContentWriter = factories as ISetItemContent;
            if (null == getContentWriter)
            {
                return null;
            }

            return getContentWriter.GetContentWriterDynamicParameters(CreateContext(path));
        }

        public void ClearContent(string path)
        {
            Action a = () => DoClearContent(path);
            ExecuteAndLog(a, "ClearContent");
        }

        private void DoClearContent(string path)
        {
            var clear = GetFirstNodeFactoryFromPath(path) as IClearItemContent;
            clear?.ClearContent(CreateContext(path));
        }

        public object ClearContentDynamicParameters(string path)
        {
            Func<object> a = () => DoClearContentDynamicParameters(path);
            return ExecuteAndLog(a, "ClearContentDynamicParameters", path);
        }

        private object DoClearContentDynamicParameters(string path)
        {
            var clear = GetFirstNodeFactoryFromPath(path) as IClearItemContent;
            if (null == clear)
            {
                return null;
            }

            return clear.ClearContentDynamicParameters(CreateContext(path));
        }

        protected void LogDebug(string format, params object[] args)
        {
            try
            {
                WriteDebug(String.Format(format, args));
            }
            catch
            {
            }
        }

        void ExecuteAndLog(Action action, string methodName, params string[] args)
        {
            var argsList = String.Join(", ", args);
            try
            {
                LogDebug(">> {0}([{1}])", methodName, argsList);
                action();
            }
            catch (PipelineStoppedException)
            {
                // For a case like 'dir | select -First 3', we can get PipelineStoppedException
                // from the PowerShell engine. That's ok because it just means that we're done.
                // Note that in a provider, by swallowing the PipelineStoppedException exception may
                // cause the PowerShell instance exit early without properly rendering outputs.
                // Also we should not call PowerShell APIs such as WriteError when receiving PipelineStoppedException.
                // This is because the PowerShell may throw a new PipelineStoppedException exception directly to a user.

                throw;
            }
            catch (Exception e)
            {
                LogDebug("!! {0}([{1}]) EXCEPTION: {2}", methodName, argsList, e.ToString());
                if (e is System.Management.Automation.RuntimeException)
                {
                    var scritpStackTrace = (e as System.Management.Automation.RuntimeException).ErrorRecord.ScriptStackTrace;
                    if (scritpStackTrace != null)
                    {
                        WriteWarning(scritpStackTrace);
                    }
                }
                if (e.InnerException != null)
                {
                    WriteError(new ErrorRecord(e.InnerException, methodName, ErrorCategory.InvalidResult, this));
                }
                WriteError(new ErrorRecord(e, methodName, ErrorCategory.InvalidResult, this));

                throw;
            }
            finally
            {
                LogDebug("<< {0}([{1}])", methodName, argsList);
            }
        }

        T ExecuteAndLog<T>(Func<T> action, string methodName, params string[] args)
        {
            var argsList = String.Join(", ", args);
            try
            {
                LogDebug(">> {0}([{1}])", methodName, argsList);
                return action();
            }
            catch (PipelineStoppedException)
            {
                // For a case like 'dir | select -First 3', we can get PipelineStoppedException
                // from the PowerShell engine. That's ok because it just means that we're done.
                // Note that in a provider, by swallowing the PipelineStoppedException exception may
                // cause the PowerShell instance exit early without properly rendering outputs.
                // Also we should not call PowerShell APIs such as WriteError when receiving PipelineStoppedException.
                // This is because the PowerShell may throw a new PipelineStoppedException exception directly to a user.
                throw;
            }
            catch (Exception e)
            {
                LogDebug("!! {0}([{1}]) EXCEPTION: {2}", methodName, argsList, e.ToString());

                if (e is System.Management.Automation.RuntimeException)
                {
                    var scritpStackTrace = (e as System.Management.Automation.RuntimeException).ErrorRecord.ScriptStackTrace;
                    if (scritpStackTrace != null)
                    {
                        WriteWarning(scritpStackTrace);
                    }
                }
                if (e.InnerException != null)
                {
                    WriteError(new ErrorRecord(e.InnerException, methodName, ErrorCategory.InvalidResult, this));
                }
                WriteError(new ErrorRecord(e, methodName, ErrorCategory.InvalidResult, this));
                throw;
            }
            finally
            {
                LogDebug("<< {0}([{1}])", methodName, argsList);
            }
        }

        private const string NotSupportedCmdletHelpID = "__NotSupported__";
        private const string RemoveItemTargetDoesNotExistErrorID = "RemoveItem.TargetDoesNotExist";
        private const string CopyItemSourceDoesNotExistErrorID = "CopyItem.SourceDoesNotExist";
        private const string SetItemTargetDoesNotExistErrorID = "SetItem.TargetDoesNotExist";
        private const string GetContentTargetDoesNotExistErrorID = "GetContent.TargetDoesNotExist";
        private const string SetContentTargetDoesNotExistErrorID = "SetContent.TargetDoesNotExist";
        private const string RenameItemNotsupportedErrorID = "RenameItem.NotSupported";
        private const string RenameItemInvokeErrorID = "RenameItem.Invoke";
        private const string NewItemNotSupportedErrorID = "NewItem.NotSupported";
        private const string NewItemInvokeErrorID = "NewItem.Invoke";
        private const string ItemModePropertyName = "SSItemMode";
        private const string RemoveItemNotSupportedErrorID = "RemoveItem.NotSupported";
        private const string RemoveItemInvokeErrorID = "RemoveItem.Invoke";
        private const string CopyItemNotSupportedErrorID = "CopyItem.NotSupported";
        private const string CopyItemInvokeErrorID = "CopyItem.Invoke";
        private const string CopyItemDestinationContainerDoesNotExistErrorID = "CopyItem.DestinationContainerDoesNotExist";
        private const string ClearItemPropertyNotsupportedErrorID = "ClearItemProperty.NotSupported";
        private const string GetHelpCustomMamlErrorID = "GetHelp.CustomMaml";
        private const string GetItemInvokeErrorID = "GetItem.Invoke";
        private const string SetItemNotSupportedErrorID = "SetItem.NotSupported";
        private const string SetItemInvokeErrorID = "SetItem.Invoke";
        private const string ClearItemNotSupportedErrorID = "ClearItem.NotSupported";
        private const string ClearItemInvokeErrorID = "ClearItem.Invoke";
        private const string InvokeItemNotSupportedErrorID = "InvokeItem.NotSupported";
        private const string InvokeItemInvokeErrorID = "InvokeItem.Invoke";
        private const string MoveItemNotSupportedErrorID = "MoveItem.NotSupported";
        private const string MoveItemInvokeErrorID = "MoveItem.Invoke";
        private const string ShouldContinuePrompt = "Are you sure?";
        private const string GetContentNotSupportedErrorId = "GetContent.NotSupported";
        private const string SetContentNotSupportedErrorId = "SetContent.NotSupported";

        private const string ClearContentNotSupportedErrorId = "ClearContent.NotSupported";



    }
}
