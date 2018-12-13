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
using System.Management.Automation.Runspaces;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Provider.PathNodes
{
    public abstract class PathNodeBase : IPathNode
    {
        public virtual IEnumerable<IPathNode> Resolve(IProviderContext providerContext, string nodeName)
        {
            var children = GetNodeChildren( providerContext );
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (null == nodeName || StringComparer.OrdinalIgnoreCase.Equals(nodeName, child.Name))
                    {
                        yield return child;
                    }
                }
            }
        }

        public abstract IPathValue GetNodeValue();
        public virtual object GetNodeChildrenParameters { get { return null; } }
        public virtual IEnumerable<IPathNode> GetNodeChildren(IProviderContext providerContext)
        {
            return null;
        }

        static readonly Dictionary<Type, string> ItemModeCache = new Dictionary<Type, string>();
        protected string EncodedItemMode
        {
            get
            {
                // "dnrgslcmri"
                // "d+~<>0cmri"

                bool canCopy = null != this as ICopyItem;
                bool canRemove = null != this as IRemoveItem;
                bool canMove = canCopy && canRemove;
                var d = " ";
                var containerEncoded = GetNodeValue().IsCollection ? "d" : d;
                var newEncoded = null != this as INewItem ? "+" : d;
                var removeEncoded = null != this as IRemoveItem ? "~" : d;

                var getEncoded = null != GetNodeValue() ? "<" : d;
                var setEncoded = null != this as ISetItem ? ">" : d;
                var clearEncoded = null != this as IClearItem ? "0" : d;

                var copyEncoded = canCopy ? "c" : d;
                var moveEncoded = canMove ? "m" : d; ;
                var renameEncoded = null != this as IRenameItem ? "r" : d;
                var invokeEncoded = null != this as IInvokeItem ? "i" : d;
                return containerEncoded + newEncoded + removeEncoded + getEncoded + setEncoded +
                                      clearEncoded +
                                      copyEncoded + moveEncoded + renameEncoded + invokeEncoded;
            }
        }
        public virtual string ItemMode
        {
            get
            {
                var type = GetType();

                if (!ItemModeCache.ContainsKey(type))
                {
                    ItemModeCache[type] = EncodedItemMode;
                }

                return ItemModeCache[type];
            }
        }

        public abstract string Name
        {
            get;
        }
    }
}
