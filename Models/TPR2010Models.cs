// Purpose: Data models and physics calculations for the TPR2010 QA test.
//
// TPR2010 measures the Tissue-Phantom Ratio at two depths (10cm and 20cm)
// at two SSDs (80cm and 90cm) for each beam energy.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Linac_QA_Software.Models
{
    /// <summary>
    /// Represents a single TPR2010 measurement at a specific depth and SSD.
    /// </summary>
    public class TPR2010Measurement
    {
        public string EnergyName { get; set; }
        public float SSD { get; set; }  // 80 or 90 cm
        public float Depth { get; set; }  // 10 or 20 cm
        public float? Average { get; set; }
    }

    /// <summary>
    /// Static helper that performs physics calculations for the TPR2010 test.
    /// </summary>
    public static class TPR2010Calculator
    {
        /// <summary>
        /// Calculates TPR2010 as the ratio of readings at 20cm depth to 10cm depth.
        /// </summary>
        /// <param name="average20cm">Average reading at 20cm depth</param>
        /// <param name="average10cm">Average reading at 10cm depth</param>
        /// <returns>TPR2010 value (dimensionless ratio)</returns>
        public static float? CalculateTPR2010(float? average20cm, float? average10cm)
        {
            if (!average20cm.HasValue || !average10cm.HasValue)
                return null;

            if (Math.Abs(average10cm.Value) < 1e-10)
                return null; // Avoid division by zero

            return average20cm.Value / average10cm.Value;
        }
    }
}
