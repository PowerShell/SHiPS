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
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Security.AccessControl;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.Provider.PathNodeProcessors
{

    public interface IProviderContext
    {
        PSDriveInfo Drive { get; }
        string GetResourceString(string baseName, string resourceId);
        void ThrowTerminatingError(ErrorRecord errorRecord);
        bool ShouldProcess(string target);
        bool ShouldProcess(string target, string action);
        bool ShouldProcess(string verboseDescription, string verboseWarning, string caption);
        bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason);
        bool ShouldContinue(string query, string caption);
        bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);
        bool TransactionAvailable();
        void WriteVerbose(string text);
        void WriteWarning(string text);
        void WriteProgress(ProgressRecord progressRecord);
        void WriteDebug(string text);
        void WriteItemObject(object item, string path, bool isContainer);
        void WritePropertyObject(object propertyValue, string path);
        void WriteSecurityDescriptorObject(ObjectSecurity securityDescriptor, string path);
        void WriteError(ErrorRecord errorRecord);
        bool Stopping { get; }
        IPathNode ResolvePath(string path);
        SessionState SessionState { get; }
        ProviderIntrinsics InvokeProvider { get; }
        CommandInvocationIntrinsics InvokeCommand { get; }
        PSCredential Credential { get; }
        bool Force { get; }
        bool Recurse { get; }
        bool ResolveFinalNodeFilterItems { get;  }
        string Filter { get; }
        IEnumerable<string> Include { get; }
        IEnumerable<string> Exclude { get; }
        object DynamicParameters { get; }
        Version PathTopologyVersion { get; }
        string Path { get; }

        CmdletProvider CmdletProvider { get; }
    }
}
