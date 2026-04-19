using System;
using System.Collections.Generic;
using System.Text;

namespace Linac_QA_Software.Models
{
    public class Config
    {
        public List<string> Machines { get; set; }
        public List<string> Physicists { get; set; }
        public List<TestConfig> Tests { get; set; }
    }

    public class TestConfig
    {
        public string Name { get; set; }
        public float Baseline { get; set; }
        public float Caution { get; set; }
        public float Fail { get; set; }

        /// <summary>
        /// Optional field-size-specific thresholds for tests like OutputFactor.
        /// Key format: "3x3", "10x10", "3x5", etc.
        /// If a field size is not found, falls back to the default Baseline/Caution/Fail.
        /// </summary>
        public Dictionary<string, FieldSizeThreshold> FieldSizeThresholds { get; set; }
    }

    public class FieldSizeThreshold
    {
        public float Baseline { get; set; }
        public float Caution { get; set; }
        public float Fail { get; set; }
    }
}
