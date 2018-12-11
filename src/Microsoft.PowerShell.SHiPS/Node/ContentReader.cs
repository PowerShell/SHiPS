using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// The content stream class for the SHiPS provider. It implements the IContentReader interfaces.
    /// It is used in Get-Content cmdlet.
    /// </summary>
    internal class ContentReader : IContentReader
    {
        private readonly CmdletProvider _provider;
        private StreamReader _reader;
        private StreamWriter _writer;
        private MemoryStream _stream;

        /// <summary>
        /// Constructor for the content stream.
        /// </summary>
        /// <param name="objects">Text content sent by SHiPS based provider.</param>
        /// <param name="context">A provider context</param>
        public ContentReader(ICollection<object> objects, IProviderContext context)
        {
            _provider = context.CmdletProvider;
            CreateStream(objects);
        }

        /// <summary>
        /// Reads the specified number of characters or a lines from the MemoryStream.
        /// </summary>
        /// <param name="readCount"> The readCount seems to be applied to FileSystem Only. Ignored for all other providers.</param>
        /// <returns> An array of strings representing the character(s) or line(s) read from targeted source. </returns>
        public IList Read(long readCount)
        {
            var blocks = new List<string>();

            // It is observed that displaying content can be slow especially on xterm cases.
            // Thus by default, we read them all and return all once. This means the user cannot use
            // Get-Content -TotalCount feature, which is fine comparing the speed of displaying content output.
            var content = _reader.ReadToEnd();
            if (content.Length > 0)
            {
                // For some reason ReadToEnd() or Readline() inserts LF at the end. So trim them off here.
                blocks.Add(content.TrimEnd(Environment.NewLine.ToCharArray()));
            }

            return blocks;
        }

        private bool ReadByLine(ArrayList blocks)
        {
            // Reading lines as strings
            var line = _reader.ReadLine();

            if (line != null)
            {
                blocks.Add(line);
            }

            var peekResult = _reader.Peek();
            return peekResult != -1;
        }

        private void CreateStream(ICollection<object> objects)
        {
            _stream = new MemoryStream();
            if (objects != null && objects.Any())
            {
                _writer = new StreamWriter(_stream);
                foreach (var obj in objects)
                {
                    _writer.WriteLine(obj.ToArgString());
                }

                _writer.Flush();
            }

            // Set to beginning of the stream.
            _stream.Seek(0, SeekOrigin.Begin);
            _reader = new StreamReader(_stream);
        }

        /// <summary>
        /// Moves the current stream position.
        /// </summary>
        ///
        /// <param name="offset"> The offset from the origin to move the position to. </param>
        /// <param name="origin"> The origin from which the offset is calculated. </param>
        public void Seek(long offset, SeekOrigin origin)
        {
            _writer.Flush();
            _stream.Seek(offset, origin);
            _writer.Flush();
            _reader.DiscardBufferedData();
        }

        /// <summary>
        /// Closes the stream.
        /// </summary>
        public void Close()
        {
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
                _reader?.Dispose();
                _stream?.Flush();
                _stream?.Dispose();
            }
            finally
            {
                _writer = null;
                _reader = null;
                _stream = null;
            }
        }

        /// <summary>
        /// Dipose the stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool isDisposing)
        {
            if (!isDisposing) { return; }

            _stream?.Dispose();
            _reader?.Dispose();
        }
    }
}
