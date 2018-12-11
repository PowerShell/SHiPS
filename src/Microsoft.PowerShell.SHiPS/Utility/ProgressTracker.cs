using System;
using System.Management.Automation;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace Microsoft.PowerShell.SHiPS
{
    internal class ProgressTracker
    {
        private readonly int _progressId;
        private readonly string _activity;
        private readonly string _description;
        private bool _builtinProgress;
        private ProgressRecord _progressRecord;

        internal ProgressTracker(int progressId, string activity, string description, bool builtinProgress)
        {
            _progressId = progressId;
            _activity = activity;
            _description = description;
            _builtinProgress = builtinProgress;
        }

        internal void Start(IProviderContext context)
        {
            if(!_builtinProgress) { return; }

            _progressRecord = new ProgressRecord(_progressId, _activity, _description)
            {
                PercentComplete = 0,
                RecordType = ProgressRecordType.Processing
            };
            context.WriteProgress(_progressRecord);
        }

        internal void Update(int percentComplete, IProviderContext context)
        {
            if (!_builtinProgress) { return; }
            if (_progressRecord != null)
            {
                _progressRecord.PercentComplete = Math.Min(percentComplete, 95);
                context.WriteProgress(_progressRecord);
            }
        }

        internal void End(IProviderContext context)
        {
            if (!_builtinProgress) { return; }

            try
            {
                if (_progressRecord != null)
                {
                    _progressRecord.PercentComplete = 100;
                    _progressRecord.RecordType = ProgressRecordType.Completed;
                    context.WriteProgress(_progressRecord);
                    _builtinProgress = false;
                }
            }
            catch (PipelineStoppedException)
            {
                //noop
            }
        }
    }
}
