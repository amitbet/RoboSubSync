using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncSubsByComparison
{
    public class TimeStamp
    {
        public long FromTime { get; set; }
        public long Duration { get; set; }
        public List<LineInfo> Lines { get; set; }
        public bool IsOffsetCorrected { get; set; }
        public long Correction { get; set; }
        public TimeSpan FromTimeAsTimeSpan { get { return TimeSpan.FromMilliseconds(FromTime); } }
        public TimeStamp()
        {
            Lines = new List<LineInfo>();
        }
    }
}
