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
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Provider;
using System.Security.AccessControl;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.Provider.PathNodeProcessors
{
    public class ProviderContext : IProviderContext
    {
        private readonly string _path;
        private readonly CmdletProvider _provider;
        private PSDriveInfo _drive;
        private readonly bool _recurse;
        private IPathResolver _pathProcessor;

        public ProviderContext(CmdletProvider provider, string path, PSDriveInfo drive, IPathResolver pathProcessor, object dynamicParameters)
            : this( provider, path, drive, pathProcessor,dynamicParameters, new Version(1,0))
        {

        }

        public ProviderContext(CmdletProvider provider, string path, PSDriveInfo drive, IPathResolver pathProcessor, object dynamicParameters, Version topology)
            : this(provider, path, drive, pathProcessor, dynamicParameters, topology, false)
        {

        }

        public ProviderContext(CmdletProvider provider, string path, PSDriveInfo drive, IPathResolver pathProcessor, object dynamicParameters, bool recurse)
            : this(provider, path, drive, pathProcessor, dynamicParameters, new Version(1,0), recurse)
        {

        }
        public ProviderContext(CmdletProvider provider, string path, PSDriveInfo drive, IPathResolver pathProcessor, object dynamicParameters, Version topology, bool recurse)
        {
            _pathProcessor = pathProcessor;
            DynamicParameters = dynamicParameters;
            _provider = provider;
            _path = path;
            _drive = drive;
            _recurse = recurse;
            PathTopologyVersion = topology;
        }

        public ProviderContext( IProviderContext providerContext, object dynamicParameters )
        {
            ProviderContext c = providerContext as ProviderContext;
            if( null == c )
            {
                throw new ArgumentException( "the providerContext provided is of an incompatible type");
            }

            _provider = c._provider;
            _drive = c._drive;
            _path = c._path;
            _pathProcessor = c._pathProcessor;
            DynamicParameters = dynamicParameters;
            _recurse = c.Recurse;
            PathTopologyVersion = c.PathTopologyVersion;
        }

        public bool ResolveFinalNodeFilterItems { get; set; }

        public IPathNode ResolvePath(string path)
        {
            var items = _pathProcessor.ResolvePath(this, path);
            if( null != items && items.Any() )
            {
                return items.First();
            }

            return null;
        }

        public PSDriveInfo Drive
        {
            get { return _drive; }
        }

        public string GetResourceString(string baseName, string resourceId)
        {
            return _provider.GetResourceString(baseName, resourceId);
        }

        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            _provider.ThrowTerminatingError(errorRecord);
        }

        public bool ShouldProcess(string target)
        {
            return _provider.ShouldProcess(target);
        }

        public bool ShouldProcess(string target, string action)
        {
            return _provider.ShouldProcess(target, action);
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
        {
            return _provider.ShouldProcess(verboseDescription, verboseWarning, caption);
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason)
        {
            return _provider.ShouldProcess(verboseDescription, verboseWarning, caption, out shouldProcessReason);
        }

        public bool ShouldContinue(string query, string caption)
        {
            return _provider.ShouldContinue(query, caption);
        }

        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            return _provider.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
        }

        public bool TransactionAvailable()
        {
            return _provider.TransactionAvailable();
        }

        public void WriteVerbose(string text)
        {
            _provider.WriteVerbose(text);
        }

        public void WriteWarning(string text)
        {
            _provider.WriteWarning(text);
        }

        public void WriteProgress(ProgressRecord progressRecord)
        {
            _provider.WriteProgress(progressRecord);
        }

        public void WriteDebug(string text)
        {
            _provider.WriteDebug(text);
        }

        public void WriteItemObject(object item, string path, bool isContainer)
        {
            _provider.WriteItemObject(item, path, isContainer);
        }

        public void WritePropertyObject(object propertyValue, string path)
        {
            _provider.WritePropertyObject(propertyValue, path);
        }

        public void WriteSecurityDescriptorObject(ObjectSecurity securityDescriptor, string path)
        {
            _provider.WriteSecurityDescriptorObject(securityDescriptor, path);
        }

        public void WriteError(ErrorRecord errorRecord)
        {
            _provider.WriteError(errorRecord);
        }

        public bool Stopping
        {
            get { return _provider.Stopping; }
        }

        public SessionState SessionState
        {
            get { return _provider.SessionState; }
        }

        public ProviderIntrinsics InvokeProvider
        {
            get { return _provider.InvokeProvider; }
        }

        public CommandInvocationIntrinsics InvokeCommand
        {
            get { return _provider.InvokeCommand; }
        }

        public PSCredential Credential
        {
            get { return _provider.Credential; }
        }

        public bool Force
        {
            get { return _provider.Force.IsPresent; }
        }

        public bool Recurse
        {
            get { return _recurse; }
        }

        public string Filter
        {
            get { return _provider.Filter; }
        }

        public IEnumerable<string> Include
        {
            get { return _provider.Include; }
        }

        public IEnumerable<string> Exclude
        {
            get { return _provider.Exclude; }
        }

        public object DynamicParameters
        {
            get;
            private set;
        }

        public Version PathTopologyVersion { get; set; }

        public string Path { get { return _path; } }

        public CmdletProvider CmdletProvider { get { return _provider;} }
    }
}
