using System;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.Models
{
    public class TPR2010Data
    {
        // Audit & Traceability
        public string MachineName { get; set; } = string.Empty;
        public DateTime TestDate { get; set; } = DateTime.Now;
        public string EnergyName { get; set; } = string.Empty;

        // Results
        public double Average10cm { get; set; }
        public double Average20cm { get; set; }
        public double MeasuredTPR2010 => Average10cm > 0 ? Average20cm / Average10cm : 0;

        public double BaselineTPR2010 { get; set; }
        public double PercentDiff => BaselineTPR2010 > 0
            ? ((MeasuredTPR2010 - BaselineTPR2010) / BaselineTPR2010) * 100.0
            : 0;

        public StatusText Status { get; set; } = StatusText.OK;
    }
}