using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using CodeOwls.PowerShell.Provider;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// Defines a drive that stores information that a provider needs.
    /// </summary>
    public class SHiPSDrive : Drive, IDisposable
    {
        private readonly string _rootInfo;
        private readonly SHiPSProvider _provider;
        private PathResolver _pathResolver = null;
        private Dictionary<string, ParameterMetadata>.KeyCollection _getChildItemDefaultParameters;
        private readonly InitialSessionState _sessionstate = InitialSessionState.CreateDefault();
        private static string _baseFolder;
        private static string _SHiPSModulePath;
        private static readonly string _SHiPSModuleName = "SHiPS.psd1";
        private Runspace _runspace;
        //Support pattern: Module#Type
        private static readonly Regex ModuleAndTypeRegex = new Regex(@"^(.[^#]+)#(.[^\\]*)\\*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal const string _homePath = "HOME";

        internal SHiPSDrive(PSDriveInfo driveInfo, string rootInfo, SHiPSProvider provider)
            : base(driveInfo)
        {
            _rootInfo = rootInfo;
            _provider = provider;
            Provider.Home = _homePath.HomePath(provider) as string;
            DriveTrimRegex = new Regex("^*?(" + Regex.Escape(Root) + ")(.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            InitializeRoot();
        }

        private void InitializeRoot()
        {
            // Module.Type
            Match match = ModuleAndTypeRegex.Match(_rootInfo);
            if (!match.Success)
            {
                // Handling a case like: myModule#Test\FamilyTree#Austin
                match = TryExcludeFQProviderPath(_rootInfo);
                if (match == null)
                {
                    _provider.ThrowError(Resources.Resource.InvalidRootFormat.StringFormat(_rootInfo), ErrorId.InvalidRootFormat);
                    return;
                }
            }

            //first we need to make sure the module is loaded
            var moduleCommand = "import-module {0}; get-module {0} -verbose".StringFormat(match.Groups[1],
                match.Groups[1]);
            _provider.WriteVerbose(moduleCommand);


            //get the module
            var module = _provider.SessionState.InvokeCommand.InvokeScript(moduleCommand, null).FirstOrDefault();
            if (module == null || !(module.BaseObject is PSModuleInfo))
            {
                // The reason that not using _provider.WriteError() here is because it is not terminatoring process
                // at this time, mearning the drive is actually gets created but not usable.
                //_provider.WriteError()
                _provider.ThrowError(Resources.Resource.CannotGetModule.StringFormat(match.Groups[1]), ErrorId.CannotGetModule);
                return;
            }

            //create powershell instance and load the modules currently referenced
            var moduleBaseObj = (PSModuleInfo) module.BaseObject;
            var modulePath = Path.Combine((moduleBaseObj).ModuleBase, (moduleBaseObj).Name.TrimEnd() + ".psd1");
            if(!File.Exists(modulePath))
            {
                _provider.WriteDebug(Resources.Resource.FileNotExist.StringFormat(modulePath));
                modulePath = moduleBaseObj.Path;
                _provider.WriteDebug(Resources.Resource.Trying.StringFormat(modulePath));
            }

            _provider.WriteVerbose(modulePath);

            //create the instance of root module
            var createRootInstance = string.Format(CultureInfo.InvariantCulture,
                @"using module '{0}'; $mod=get-module {1}; &($mod){{[{2}]::new('{2}')}}", modulePath, match.Groups[1], match.Groups[2]);

            _provider.WriteVerbose(createRootInstance);

            //ifdef #newrunspace
            PowerShellInstance = CreatePowerShellObject(_provider.Host, modulePath);
            PowerShellInstance.AddScript(createRootInstance);

            var errors = new ConcurrentBag<ErrorRecord>();

            var rootPsObject = PSScriptRunner.CallPowerShellScript(createRootInstance, PowerShellInstance, (sender, e) => PSScriptRunner.error_DataAdded(sender, e, errors));
            //var rootPsObject = PowerShellInstance.Invoke().FirstOrDefault();
            if (rootPsObject == null || !rootPsObject.Any())
            {

                var message = Resources.Resource.CannotCreateInstance.StringFormat(match.Groups[2], modulePath, match.Groups[2], match.Groups[2]);
                if (errors.WhereNotNull().Any())
                {
                    var error = errors.FirstOrDefault();
                    message += Environment.NewLine;
                    message += error.ErrorDetails == null ? error.Exception.Message : error.ErrorDetails.Message;
                }

                _provider.ThrowError(message, ErrorId.CannotCreateInstance);

                return;
            }

            var node = rootPsObject.FirstOrDefault().ToNode();
            if (node == null)
            {
                _provider.ThrowError(Resources.Resource.InvalidRootNode, ErrorId.NotContainerNode);
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                _provider.ThrowError(Resources.Resource.NameWithNullOrEmpty.StringFormat(string.IsNullOrWhiteSpace(node.Name) ? "root" : node.Name), ErrorId.NodeNameIsNullOrEmpty);
            }

            if (node.IsLeaf)
            {
                _provider.ThrowError(Resources.Resource.InvalidRootNodeType.StringFormat(Constants.Leaf), ErrorId.RootNodeTypeMustBeContainer);
            }

            RootNode = node;

            RootPathNode = new ContainerNodeService(this, node, null);

            // Getting the Get-ChildItem default parameters before running any commands. It will be used for checking
            // whether a user is passing in any dynamic parameters.
            _getChildItemDefaultParameters = GetChildItemDefaultParameters;
        }

        internal Dictionary<string, ParameterMetadata>.KeyCollection GetChildItemDefaultParameters
        {
            get { return _getChildItemDefaultParameters??GetCommandParameters("Get-ChildItem"); }
        }
        internal System.Management.Automation.PowerShell PowerShellInstance { get; set; }

        internal PathResolver PathResolver
        {
            get { return _pathResolver ?? (_pathResolver = new PathResolver(_provider, this)); }
        }

        internal SHiPSDirectory RootNode { get; set; }

        internal IPathNode RootPathNode { get; set; }

        internal Regex DriveTrimRegex { get; set; }

        internal SHiPSProvider SHiPS { get { return _provider; } }

        internal static string BaseFolder
        {
            get
            {
                if (_baseFolder == null)
                {
#if CORECLR
                    _baseFolder = Path.GetDirectoryName(Path.GetFullPath(typeof(SHiPSDrive).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName));
#else
                    _baseFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
#endif
                    if (_baseFolder == null || !Directory.Exists(_baseFolder))
                    {
                        throw new Exception(Resources.Resource.CantFindBaseModuleFolder);
                    }


                }
                return _baseFolder;
            }
        }

        internal static string SHiPSModule
        {
            get
            {
                if (_SHiPSModulePath == null)
                {
                    _SHiPSModulePath = Path.Combine(BaseFolder, _SHiPSModuleName);
                    if (!File.Exists(_SHiPSModulePath))
                    {
                        if (!File.Exists(_SHiPSModulePath))
                        {
                            // oh-oh
                            throw new Exception(Resources.Resource.UnableToFindModule.StringFormat(_SHiPSModuleName, _SHiPSModulePath));
                        }
                    }
                }

                return _SHiPSModulePath;
            }
        }

        private System.Management.Automation.PowerShell CreatePowerShellObject(PSHost host, string modulePath)
        {
            System.Management.Automation.PowerShell ps = null;
            try
            {
                //loading SHiPS module
                _sessionstate.ImportPSModule(new[] { SHiPSModule });

                //loading dsl module
                if (!string.IsNullOrWhiteSpace(modulePath))
                {
                    _sessionstate.ImportPSModule(new[] { modulePath });
                }
                _runspace = RunspaceFactory.CreateRunspace(host, _sessionstate);

                if (_runspace == null)
                {
                    return null;
                }
                else
                {
                    _runspace.Open();
                    var error = _runspace.SessionStateProxy.PSVariable.GetValue("Error") as ArrayList;
                    if (error != null && error.Count > 0)
                    {
                        _provider.ThrowTerminatingError(error[0] as ErrorRecord);
                        return null;
                    }
                    ps = System.Management.Automation.PowerShell.Create();

                    //Cannot use ps.Runspace = Runspace.DefaultRunspace, it will give you an error
                    //Pipelines cannot be run concurrently
                    ps.Runspace = _runspace;
                    return ps;
                }
            }
            catch (Exception)
            {
                // didn't create or import correctly.
                if (ps != null)
                {
                    ps.Dispose();
                }
                if (_runspace != null)
                {
                    _runspace.Close();
                }

                throw;
            }
        }

        private Dictionary<string, ParameterMetadata>.KeyCollection GetCommandParameters(string command)
        {
            var commandInfo = _provider.SessionState.InvokeCommand.GetCommand(command, CommandTypes.All);

            return commandInfo != null ? commandInfo.Parameters.Keys : null;
        }

        private static Match TryExcludeFQProviderPath(string rootPath)
        {
            // Handling a case like: CurrentModule#CurrentType\NewModule#NewType
            var index = rootPath.IndexOfAny(new [] { '\\', '/'});
            var leaf = index > 0 ? rootPath.Substring(index + 1) : null;

            if (string.IsNullOrWhiteSpace(leaf)) { return null; }

            var match = ModuleAndTypeRegex.Match(leaf);
            return match.Success ? match : null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_runspace != null)
                {
                    _runspace.Close();
                    _runspace.Dispose();
                }

                if (PowerShellInstance != null)
                {
                    PowerShellInstance.Dispose();
                }
                PowerShellInstance = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
