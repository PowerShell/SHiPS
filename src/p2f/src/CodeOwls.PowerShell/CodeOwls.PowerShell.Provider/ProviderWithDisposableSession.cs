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
using System.Text;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.Attributes;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Provider
{
    internal interface IProvideNewSession
    {
        IDisposable NewSession();
    }

    public abstract class ProviderWithDisposableSession : Provider, IProvideNewSession
    {
        public abstract IDisposable NewSession();

        protected override object GetItemDynamicParameters(string path)
        {
            using (NewSession())
            {
                return base.GetItemDynamicParameters(path);
            }
        }

        protected override object ItemExistsDynamicParameters(string path)
        {
            using (NewSession())
            {
                return base.ItemExistsDynamicParameters(path);
            }
        }

        protected override bool IsItemContainer(string path)
        {
            using (NewSession())
            {
                return base.IsItemContainer(path);
            }
        }

        protected override object MoveItemDynamicParameters(string path, string destination)
        {
            using (NewSession())
            {
                return base.MoveItemDynamicParameters(path, destination);
            }
        }

        protected override void MoveItem(string path, string destination)
        {
            using (NewSession())
            {
                base.MoveItem(path, destination);
            }
        }

        protected override void GetItem(string path)
        {
            using (NewSession())
            {
                base.GetItem(path);
            }
        }

        protected override void SetItem(string path, object value)
        {
            using (NewSession())
            {
                base.SetItem(path, value);
            }
        }

        protected override object SetItemDynamicParameters(string path, object value)
        {
            using (NewSession())
            {
                return base.SetItemDynamicParameters(path, value);
            }
        }

        protected override object ClearItemDynamicParameters(string path)
        {
            using (NewSession())
            {
                return base.ClearItemDynamicParameters(path);
            }
        }

        protected override void ClearItem(string path)
        {
            using (NewSession())
            {
                base.ClearItem(path);
            }
        }

        protected override object InvokeDefaultActionDynamicParameters(string path)
        {
            using (NewSession())
            {
                return base.InvokeDefaultActionDynamicParameters(path);
            }
        }

        protected override void InvokeDefaultAction(string path)
        {
            using (NewSession())
            {
                base.InvokeDefaultAction(path);
            }
        }

        protected override bool ItemExists(string path)
        {
            using (NewSession())
            {
                return base.ItemExists(path);
            }
        }

        protected override bool IsValidPath(string path)
        {
            using (NewSession())
            {
                return base.IsValidPath(path);
            }
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            using (NewSession())
            {
                base.GetChildItems(path, recurse);
            }
        }

        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            using (NewSession())
            {
                return base.GetChildItemsDynamicParameters(path, recurse);
            }
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            using (NewSession())
            {
                base.GetChildNames(path, returnContainers);
            }
        }

        protected override object GetChildNamesDynamicParameters(string path)
        {
            using (NewSession())
            {
                return base.GetChildNamesDynamicParameters(path);
            }
        }

        protected override void RenameItem(string path, string newName)
        {
            using (NewSession())
            {
                base.RenameItem(path, newName);
            }
        }

        protected override object RenameItemDynamicParameters(string path, string newName)
        {
            using (NewSession())
            {
                return base.RenameItemDynamicParameters(path, newName);
            }
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            using (NewSession())
            {
                base.NewItem(path, itemTypeName, newItemValue);
            }
        }

        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            using (NewSession())
            {
                return base.NewItemDynamicParameters(path, itemTypeName, newItemValue);
            }
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            using (NewSession())
            {
                base.RemoveItem(path, recurse);
            }
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            using (NewSession())
            {
                return base.RemoveItemDynamicParameters(path, recurse);
            }
        }

        protected override bool HasChildItems(string path)
        {
            using (NewSession())
            {
                return base.HasChildItems(path);
            }
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            using (NewSession())
            {
                base.CopyItem(path, copyPath, recurse);
            }
        }

        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        {
            using (NewSession())
            {
                return base.CopyItemDynamicParameters(path, destination, recurse);
            }
        }
    }
}
