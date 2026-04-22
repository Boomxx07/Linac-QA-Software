using System;
using System.Collections.Generic;
using System.Linq;

namespace Linac_QA_Software.Models
{
    /// <summary>
    /// Root configuration object mapped to config.json.
    /// </summary>
    public class Config
    {
        public List<string>? Machines { get; set; }
        public List<string>? Physicists { get; set; }
        public List<TestConfig>? Tests { get; set; }
    }

    /// <summary>
    /// Represents configuration and threshold data for a specific QA test.
    /// Supports multiple levels of lookup complexity.
    /// </summary>
    public class TestConfig
    {
        public string? Name { get; set; }

        // --- Level 1: Global ---
        // Used for Linearity or simple mechanical tests
        public ThresholdSet GlobalThreshold { get; set; } = new();

        // --- Level 2: Single Key (e.g., "6MV") ---
        // Used for TPR20,10 or similar energy-only checks
        public Dictionary<string, ThresholdSet> SingleKeyThresholds { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        // --- Level 3: Nested Keys (e.g., "6MV" -> "15") ---
        // Used for EDW Output Factors or Jaw Output Factors
        public Dictionary<string, Dictionary<string, ThresholdSet>> NestedThresholds { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The "Clean" Accessor. 
        /// Automatically finds the most specific threshold set available.
        /// </summary>
        public ThresholdSet GetThresholds(string? primaryKey = null, string? secondaryKey = null)
        {
            // 1. Try Nested (Level 3)
            if (primaryKey != null && secondaryKey != null &&
                NestedThresholds.TryGetValue(primaryKey, out var subDict) &&
                subDict.TryGetValue(secondaryKey, out var nestedResult))
            {
                return nestedResult;
            }

            // 2. Try Single Key (Level 2)
            if (primaryKey != null &&
                SingleKeyThresholds.TryGetValue(primaryKey, out var keyedResult))
            {
                return keyedResult;
            }

            // 3. Fallback to Global (Level 1)
            return GlobalThreshold;
        }
    }

    /// <summary>
    /// Container for the baseline and the clinical tolerances.
    /// </summary>
    public class ThresholdSet
    {
        public float Baseline { get; set; }
        public float Caution { get; set; }
        public float Fail { get; set; }
    }
}