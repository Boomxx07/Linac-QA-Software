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
    }
}
