using System;
using System.Collections.Generic;
using System.Management.Automation;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace ProviderFramework_3_TypeProvider
{
    class TypePathNode : PathNodeBase, IInvokeItem
    {
        #region unchanged code
        private readonly Type _type;

        public TypePathNode( Type type )
        {
            _type = type;
        }

        public override IPathValue GetNodeValue()
        {
            return new LeafPathValue( _type, Name );
        }

        public override string Name
        {
            get { return _type.FullName; }
        }
        #endregion

        /// <summary>
        /// returns an object defining custom parameters for the new-item cmdlet.
        ///
        /// because this provider requires a custom parameter for invoke-item, this
        /// implementation return an instance object describing those parameters
        /// </summary>
        public object InvokeItemParameters
        {
            get { return new CustomInvokeItemParameters(); }
        }

        public class CustomInvokeItemParameters
        {
            [Parameter(
                Mandatory = true,
                HelpMessage = "the name of the variable to hold the new instance")]
            public string VariableName {get; set; }
        }

        /// <summary>
        /// processes the invoke-item cmdlet when invoked on a path that
        /// resolves to a type value.
        ///
        /// this implementation attempts to create an instance of the type
        /// represented by the path.  the type must have a default constructor
        /// for this to work.
        ///
        /// once an instance is created, it is stored in the variable indicated
        /// by the custom parameter
        /// </summary>
        /// <param name="providerContext">the cmdlet providerContext, providing powershell and framework services</param>
        /// <param name="path">the relative path to the item being invoked</param>
        /// <returns></returns>
        public IEnumerable<object> InvokeItem(IProviderContext providerContext, string path)
        {
            // retrieve our custom parameter object
            var param = providerContext.DynamicParameters as CustomInvokeItemParameters;

            // create an instance of the type at this path value
            var item = Activator.CreateInstance(_type);

            // set the specified variable value to the created instance
            providerContext.SessionState.PSVariable.Set( param.VariableName, item );

            // return the object created
            return new[] {item};
        }
    }
}
