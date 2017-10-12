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

using System.Management.Automation;

namespace CodeOwls.PowerShell.Provider
{
    public abstract class ProviderWithTransactions : ProviderWithDisposableSession 
    {
        protected override object GetItemDynamicParameters(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.GetItemDynamicParameters(path);
                }
            }
            return base.GetItemDynamicParameters(path); 
        }

        protected override object ItemExistsDynamicParameters(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.ItemExistsDynamicParameters(path);
                }
            }
            return base.ItemExistsDynamicParameters(path);
        }

        protected override bool IsItemContainer(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.IsItemContainer(path);
                }
            } 
            return base.IsItemContainer(path);
        }

        protected override object MoveItemDynamicParameters(string path, string destination)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.MoveItemDynamicParameters(path, destination);
                }
            } 
            return base.MoveItemDynamicParameters(path, destination);
        }

        protected override void MoveItem(string path, string destination)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.MoveItem(path, destination);
                    return;
                }
            } 
            base.MoveItem(path, destination);
        }

        protected override void GetItem(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.GetItem(path);
                    return;
                }
            } 
            base.GetItem(path);
        }

        protected override void SetItem(string path, object value)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.SetItem(path, value);
                    return;
                }
            } 
            base.SetItem(path, value);
        }

        protected override object SetItemDynamicParameters(string path, object value)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.SetItemDynamicParameters(path, value);
                }
            } 
            return base.SetItemDynamicParameters(path, value);
        }

        protected override object ClearItemDynamicParameters(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.ClearItemDynamicParameters(path);
                }
            } 
            return base.ClearItemDynamicParameters(path);
        }

        protected override void ClearItem(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.ClearItem(path);
                    return;
                }
            }
            base.ClearItem(path);
        }

        protected override object InvokeDefaultActionDynamicParameters(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.ItemExistsDynamicParameters(path);
                }
            } return base.InvokeDefaultActionDynamicParameters(path);
        }

        protected override void InvokeDefaultAction(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.InvokeDefaultAction(path);
                    return;
                }
            } 
            base.InvokeDefaultAction(path);
        }

        protected override bool ItemExists(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.ItemExists(path);
                }
            } 
            return base.ItemExists(path);
        }

        protected override bool IsValidPath(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.IsValidPath(path);
                }
            } 
            return base.IsValidPath(path);
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.GetChildItems(path, recurse);
                    return;
                }
            } 
            base.GetChildItems(path, recurse);
        }

        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.GetChildItemsDynamicParameters(path, recurse);
                }
            } 
            return base.GetChildItemsDynamicParameters(path, recurse);
        }

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.GetChildNames(path, returnContainers);
                    return;
                }
            } 
            base.GetChildNames(path, returnContainers);
        }

        protected override object GetChildNamesDynamicParameters(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.GetChildNamesDynamicParameters(path);
                }
            } 
            return base.GetChildNamesDynamicParameters(path);
        }

        protected override void RenameItem(string path, string newName)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.RenameItem(path, newName);
                    return;
                }
            } 
            base.RenameItem(path, newName);
        }

        protected override object RenameItemDynamicParameters(string path, string newName)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.RenameItemDynamicParameters(path, newName);
                }
            } 
            return base.RenameItemDynamicParameters(path, newName);
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.NewItem(path, itemTypeName, newItemValue);
                    return;
                }
            } 
            base.NewItem(path, itemTypeName, newItemValue);
        }

        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.ItemExistsDynamicParameters(path);
                }
            } return base.NewItemDynamicParameters(path, itemTypeName, newItemValue);
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.RemoveItem(path, recurse);
                    return;
                }
            } 
            base.RemoveItem(path, recurse);
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.RemoveItemDynamicParameters(path, recurse);
                }
            } 
            return base.RemoveItemDynamicParameters(path, recurse);
        }

        protected override bool HasChildItems(string path)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.HasChildItems(path);
                }
            } 
            return base.HasChildItems(path);
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    base.CopyItem(path, copyPath, recurse);
                    return;
                }
            } 
            base.CopyItem(path, copyPath, recurse);
        }

        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        {
            if (TransactionAvailable())
            {
                using (CurrentPSTransaction)
                {
                    return base.CopyItemDynamicParameters(path, destination, recurse);
                }
            } 
            return base.CopyItemDynamicParameters(path, destination, recurse);
        }
    }
}
