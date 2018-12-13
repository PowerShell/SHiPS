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




using CodeOwls.PowerShell.Paths.Extensions;

namespace CodeOwls.PowerShell.Provider.PathNodes
{
    public class ContainerPathValue : PathValue
    {
        public ContainerPathValue(object item, string name) : base(item, name, true)
        {
        }
    }

    public class LeafPathValue : PathValue
    {
        public LeafPathValue(object item, string name) : base(item, name, false)
        {
        }
    }

    public class PathValue : IPathValue
	{
		public PathValue( object item, string name, bool isContainer )
		{
			Item = item;
			Name = name;
			IsCollection = isContainer;
		}
		public object Item
		{
			get;
			private set;
		}

        private string _name;

        public string Name
        {
            get { return _name.MakeSafeForPath(); }
            private set { _name = value; }
        }

        public bool IsCollection
		{
			get;
			private set;
		}
	}
}
