using System;
using System.Collections.Generic;
using System.Linq;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.Models
{
    public class LinearityPoint
    {
        // --- Inputs ---
        public double MU { get; set; }
        public double RawAverageReading { get; set; }
        public double LeakageValue { get; set; }

        // --- Reference Data (Set by Calculator) ---
        /// <summary>The Reading/MU of the 200 MU reference point.</summary>
        public double ReferenceBaseline { get; set; }

        // --- Automatic Calculations (The "Live" Math) ---
        public double CorrectedReading => RawAverageReading - LeakageValue;
        public double ReadingPerMU => MU > 0 ? CorrectedReading / MU : 0;

        public double PercentDiff => ReferenceBaseline > 0
            ? ((ReadingPerMU - ReferenceBaseline) / ReferenceBaseline) * 100.0
            : 0;

        public StatusText Status { get; set; } = StatusText.OK;
    }

    public class LinearityData
    {
        public string MachineName { get; set; } = string.Empty;
        public DateTime TestDate { get; set; } = DateTime.Now;
        public string EnergyName { get; set; } = string.Empty;

        public List<LinearityPoint> Points { get; set; } = new();

        // Regression Metadata (Set by Calculator)
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double RSquared { get; set; }
        public StatusText OverallStatus { get; set; } = StatusText.OK;
    }

    // --- The Logic Section (Essential for "Global" calculations) ---
    public static class PhysicsCalculator
    {
        /// <summary>
        /// Returns the nominal dose rate in MU per second for a given energy.
        /// Used to estimate beam-on time for leakage corrections.
        /// </summary>
        public static double DoseRateMuPerSec(string energyName)
        {
            if (string.IsNullOrEmpty(energyName)) return 10.0; // Fallback

            // Typical Varian/Elekta rates:
            // Standard: 600 MU/min -> 10 MU/sec
            // FFF: 1400 MU/min -> 23.33 MU/sec
            if (energyName.Contains("FFF", StringComparison.OrdinalIgnoreCase))
            {
                return 1400.0 / 60.0;
            }

            return 600.0 / 60.0;
        }

        /// <summary>
        /// Calculates leakage rate (nC/s) based on two background measurements.
        /// </summary>
        public static double? CalculateLeakage(double? r1, double? t1, double? r2, double? t2)
        {
            if (r1.HasValue && t1.HasValue && r2.HasValue && t2.HasValue)
            {
                double deltaR = r2.Value - r1.Value;
                double deltaT = t2.Value - t1.Value;

                if (Math.Abs(deltaT) > 1e-6)
                    return deltaR / deltaT;
            }
            return null;
        }

        public static void FinalizeLinearity(LinearityData data)
        {
            if (data.Points == null || data.Points.Count == 0) return;

            // 1. Find the Reference (usually the 200 MU point)
            var refPoint = data.Points.OrderBy(p => Math.Abs(p.MU - 200)).FirstOrDefault();
            double baseline = refPoint?.ReadingPerMU ?? 0;

            // 2. Distribute the baseline to all points so they can calculate their own % Diff
            foreach (var p in data.Points)
            {
                p.ReferenceBaseline = baseline;
            }

            // 3. Perform the Regression for the trendline
            var points = data.Points;
            int n = points.Count;
            if (n < 2) return;

            double sumX = points.Sum(p => p.MU);
            double sumY = points.Sum(p => p.CorrectedReading);
            double sumXY = points.Sum(p => p.MU * p.CorrectedReading);
            double sumXX = points.Sum(p => p.MU * p.MU);
            double sumYY = points.Sum(p => p.CorrectedReading * p.CorrectedReading);

            double det = (n * sumXX) - (sumX * sumX);
            if (Math.Abs(det) < 1e-12) return;

            data.Slope = (n * sumXY - sumX * sumY) / det;
            data.Intercept = (sumY - data.Slope * sumX) / n;

            // R-Squared
            double rNum = (n * sumXY) - (sumX * sumY);
            double rDen = Math.Sqrt(((n * sumXX) - (sumX * sumX)) * ((n * sumYY) - (sumY * sumY)));
            data.RSquared = rDen > 1e-12 ? Math.Pow(rNum / rDen, 2) : 0;
        }
    }
}