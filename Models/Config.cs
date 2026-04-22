using System;
using System.Collections.Generic;
using System.Text;

namespace Linac_QA_Software.Models
{
    public class Config
    {
        public List<string>? Machines { get; set; }
        public List<string>? Physicists { get; set; }
        public List<TestConfig>? Tests { get; set; }
    }

    public class TestConfig
    {
        public string? Name { get; set; }

        // Level 1: Global Defaults (Works for Linearity)
        public float Baseline { get; set; }
        public float Caution { get; set; }
        public float Fail { get; set; }

        // Level 2: Field-specific but energy-independent
        public Dictionary<string, FieldSizeThreshold> FieldSizeThresholds { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        // Level 3: Energy-specific nested thresholds
        public Dictionary<string, Dictionary<string, FieldSizeThreshold>> EnergyThresholds { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }

    public class FieldSizeThreshold
    {
        public float Baseline { get; set; }
        public float Caution { get; set; }
        public float Fail { get; set; }
    }
}
