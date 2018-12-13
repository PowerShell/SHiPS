using System;
using System.Collections;
using System.IO;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{
    /// <summary>
    /// The content stream class for the SHiPS provider. It implements the IContentWriter interfaces.
    /// It is used in Set-Content cmdlet.
    /// </summary>
    internal class ContentWriter : IContentWriter
    {
        private MemoryStream _stream;
        private StreamWriter _writer;
        private readonly SHiPSBase _node;
        private readonly SHiPSDrive _drive;
        private readonly IProviderContext _context;

        /// <summary>
        /// Constructor for the content stream.
        /// </summary>
        /// <param name="context">A provider context.</param>
        /// <param name="drive">SHiPS based provider drive.</param>
        /// <param name="node">An object that is corresponding to the current node path.</param>
        public ContentWriter(IProviderContext context, SHiPSDrive drive, SHiPSBase node)
        {
            _drive = drive;
            _node = node;
            _context = context;
            _stream = new MemoryStream();
            _writer = new StreamWriter(_stream);
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
        }

        /// <summary>
        /// Closes the stream.
        /// </summary>
        public void Close()
        {
            if (_writer == null || _stream == null) { return; }

            try
            {
                _writer.Flush();
                _stream.Flush();

                // By now the PS engine completes reading from its pipleline and have the stream ready for SetConent()
                _stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(_stream))
                {
                    // Read the entire content from the stream as a string text
                    var content = reader.ReadToEnd();
                    // Invoke SetContent and pass in content and path to the script
                    // Calling SHiPS based PowerShell provider '[object] SetContent([string]$content, [string]$path)'
                    var script = Constants.ScriptBlockWithParam3.StringFormat(Constants.SetContent);
                    PSScriptRunner.InvokeScriptBlock(_context, _node, _drive, script, PSScriptRunner.SetContentNotSupported, content, _context.Path);
                }
            }
            finally
            {
                _writer.Dispose();
                _stream.Dispose();

                _writer = null;
                _stream = null;
            }
        }

        /// <summary>
        /// Writes the specified object to the stream.
        /// </summary>
        /// <param name="content"> The objects to write to the file </param>
        /// <returns> The objects written to the file. </returns>
        public IList Write(IList content)
        {
            foreach (var line in content)
            {
                var contentArray = line as object[];
                if (contentArray != null)
                {
                    foreach (var obj in contentArray)
                    {
                        WriteObject(obj);
                    }
                }
                else
                {
                    WriteObject(line);
                }
            }
            return content;
        }

        private void WriteObject(object content)
        {
            if (content != null)
            {
                _writer.WriteLine(content.ToString());
            }
        }

        /// <summary>
        /// Disposes the stream.
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
            _writer?.Dispose();
        }
    }
}
