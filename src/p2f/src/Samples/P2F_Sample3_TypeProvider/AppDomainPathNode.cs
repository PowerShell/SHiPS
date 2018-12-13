using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace ProviderFramework_3_TypeProvider
{
    class AppDomainPathNode : PathNodeBase, INewItem
    {
        #region unchanged code from previous version
        public override IPathValue GetNodeValue()
        {
            return new ContainerPathValue( AppDomain.CurrentDomain, Name );
        }

        public override string Name
        {
            get { return "AppDomain"; }
        }

        public override IEnumerable<IPathNode> GetNodeChildren(CodeOwls.PowerShell.Provider.PathNodeProcessors.IProviderContext providerContext)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   select new AssemblyPathNode(assembly) as IPathNode;
        }
        #endregion

        /// <summary>
        /// returns a list of valid type names for new items
        ///
        /// if provided the P2F uses this list to validate the value supplied
        /// to the new-item -itemType parameter
        ///
        /// because this provider does not support item types, this
        /// implementation simply returns null
        /// </summary>
        public IEnumerable<string> NewItemTypeNames
        {
            get { return null; }
        }

        /// <summary>
        /// returns an object defining custom parameters for the new-item cmdlet.
        ///
        /// because this provider does not support custom parameters, this
        /// implementation simply returns null
        /// </summary>
        public object NewItemParameters
        {
            get { return null; }
        }

        /// <summary>
        /// processes the new-item cmdlet for paths that resolve to the appdomain value factory
        ///
        /// adds an assembly to the current appdomain.  assemblies can be specified using
        /// their (partial) names or a file path.
        ///
        /// this implementation essentially delegates all work to to the add-type built-in
        /// powershell cmdlet to load an assembly from a partial name or file path.
        /// </summary>
        /// <param name="providerContext">the cmdlet providerContext providing powershell and framework services</param>
        /// <param name="path">the relative path supplied to the new-item cmdlet, either as the child element of the -path parameter, or the value of the -name parameter</param>
        /// <param name="itemTypeName">the type of the item to create; unused in this value</param>
        /// <param name="newItemValue">the value of the new item to create; unused in this value</param>
        /// <returns></returns>
        public IPathValue NewItem(IProviderContext providerContext, string path, string itemTypeName, object newItemValue)
        {
            IEnumerable<PSObject> results = new PSObject[]{};

            // first try to load the new assembly from an assembly name
            //  note that the invoked script will return a single assembly object if successful
            try
            {
                results = providerContext.InvokeCommand.InvokeScript(
                    String.Format("add-type -assemblyname '{0}' -passthru | select -first 1 -exp Assembly", path),
                    null);
            }
            catch
            {
            }

            // ... if that fails, try and load the assembly from a file path
            //  note that the invoked script will return a single assembly object if successful
            if( ! results.Any() )
            {
                try
                {
                    results = providerContext.InvokeCommand.InvokeScript(
                        String.Format("add-type -path '{0}' -passthru | select -first 1 -exp Assembly", path),
                        null);
                }
                catch
                {
                }
            }

            // raise any errors
            if( ! results.Any() )
            {
                throw new ArgumentException( "the specified name is not recognized as a valid assembly name or path");
            }

            // return the path value value to write to the pipeline
            var assembly = results.First().BaseObject as Assembly;

            // to maintain consistency I find it easier to leverage the value factory classes rather than return
            //  an IPathValue instance directly.
            var nodeFactory = new AssemblyPathNode(assembly);
            return nodeFactory.GetNodeValue();
        }
    }
}
