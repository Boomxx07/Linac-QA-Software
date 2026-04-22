using System;
using System.Collections.Generic;
using System.Linq;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.Models
{
    /// <summary>
    /// Represents a single field size measurement within an Output Factor test.
    /// </summary>
    public class OutputFactorPoint
    {
        // --- Metadata ---
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsMLC { get; set; }

        /// <summary>The 'Gold Standard' value established during commissioning.</summary>
        public double CommissionedBaseline { get; set; }

        // --- Inputs ---
        /// <summary>The average reading measured for this specific field size.</summary>
        public double RawAverageReading { get; set; }

        // --- Reference Data (Set by Calculator) ---
        /// <summary>The reading measured for the 10x10 reference field.</summary>
        public double ReferenceReading10x10 { get; set; }

        // --- Automatic Calculations ---

        /// <summary>Measured OF = Raw Reading / 10x10 Reading.</summary>
        public double MeasuredOutputFactor => ReferenceReading10x10 > 0
            ? RawAverageReading / ReferenceReading10x10
            : 0;

        /// <summary>% Diff relative to the Commissioned Baseline.</summary>
        public double PercentDiff => CommissionedBaseline > 0
            ? ((MeasuredOutputFactor - CommissionedBaseline) / CommissionedBaseline) * 100.0
            : 0;

        public StatusText Status { get; set; } = StatusText.OK;

        public bool IsSymmetric => Math.Abs(X - Y) < 0.001;
        public double SquareFieldSize => (X + Y) / 2.0;
    }

    /// <summary>
    /// The complete record for an Output Factor QA session.
    /// </summary>
    public class OutputFactorData
    {
        public string MachineName { get; set; } = string.Empty;
        public DateTime TestDate { get; set; } = DateTime.Now;
        public string EnergyName { get; set; } = string.Empty;

        public List<OutputFactorPoint> Points { get; set; } = new();

        public StatusText OverallStatus { get; set; } = StatusText.OK;
    }

    // --- Physics Calculator ---
    public static class OutputFactorCalculator
    {
        /// <summary>
        /// Finalizes the Output Factor session by finding the 10x10 reference 
        /// and updating all measured factors.
        /// </summary>
        public static void FinalizeOutputFactors(OutputFactorData data)
        {
            if (data.Points == null || data.Points.Count == 0) return;

            // 1. Find the 10x10 reference point (the baseline for the ratio)
            // We look for X=10, Y=10 and NOT an MLC field.
            var refPoint = data.Points.FirstOrDefault(p =>
                Math.Abs(p.X - 10.0) < 0.1 &&
                Math.Abs(p.Y - 10.0) < 0.1 &&
                !p.IsMLC);

            double refReading = refPoint?.RawAverageReading ?? 0;

            // 2. Distribute the reference reading to all points
            // This triggers the 'MeasuredOutputFactor' and 'PercentDiff' properties.
            foreach (var p in data.Points)
            {
                p.ReferenceReading10x10 = refReading;
            }
        }
    }
}