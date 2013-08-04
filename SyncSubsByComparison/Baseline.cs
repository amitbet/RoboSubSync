using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncSubsByComparison
{
    public class Baseline
    {
        public List<double> Averages { get; set; }
        public HashSet<long> AbnormalPoints { get; set; }
    }
}
