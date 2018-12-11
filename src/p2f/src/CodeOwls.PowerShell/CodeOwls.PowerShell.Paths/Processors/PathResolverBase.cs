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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.PowerShell.Paths.Processors
{
    public abstract class PathResolverBase : IPathResolver
    {
        protected abstract IPathNode Root { get; }

        public virtual IEnumerable<IPathNode> ResolvePath(IProviderContext providerContext, string path)
        {
            Regex re = new Regex(@"^[-_a-z0-9:]+:/?");
            path = path.ToLowerInvariant().Replace('\\', '/');
            path = re.Replace(path, "");

            var factory = Root;

            var nodeMonikers = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<IPathNode> factories = new[] {factory};

            foreach (var nodeMoniker in nodeMonikers )
            {
                factories = factory.Resolve(providerContext, nodeMoniker);
                if (null == factories || !factories.Any())
                {
                    return null;
                }

                factory = factories.First();
            }

            return factories;
        }
    }
}
