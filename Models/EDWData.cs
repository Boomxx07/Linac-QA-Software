using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.Models
{
    /// <summary>
    /// Represents the final results and metadata for an Enhanced Dynamic Wedge (EDW) test.
    /// This object contains the audit info, inputs, the measured result, and the clinical status.
    /// </summary>
    public class EDWData
    {
        // --- Audit & Traceability ---

        /// <summary>The name/ID of the Linear Accelerator (e.g., "TrueBeam1").</summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>The date and time the QA test was performed.</summary>
        public DateTime TestDate { get; set; } = DateTime.Now;
        
        // --- Metadata (The 'Context') ---

        /// <summary>The beam energy, e.g., "6MV" or "10MV".</summary>
        public string EnergyName { get; set; } = string.Empty;

        /// <summary>The EDW angle in degrees (10, 15, 20, 25, 30, 45, or 60).</summary>
        public double WedgeAngle { get; set; }

        /// <summary>The expected Output Factor established during commissioning.</summary>
        public double BaselineValue { get; set; }

        // --- Results (The 'Output') ---

        /// <summary>The measured Output Factor calculated from the session readings.</summary>
        public double? OutputFactor { get; set; }

        /// <summary>The calculated percentage deviation from the baseline.</summary>
        public double? PercentDiff { get; set; }

        /// <summary>The clinical status (OK, CAUTIONARY, FAIL) determined by the evaluator.</summary>
        public StatusText Status { get; set; } = StatusText.OK;
    }
}