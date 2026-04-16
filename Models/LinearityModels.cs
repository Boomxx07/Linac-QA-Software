// Purpose: Data models and physics calculations for the Linearity QA test.
//
// Keeping the math here (Model layer) means the ViewModel only has to
// prepare inputs and display outputs — it does not need to understand
// the physics or statistics involved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Linac_QA_Software.Models
{
    // -------------------------------------------------------------------------
    // Data Transfer Objects
    // -------------------------------------------------------------------------

    /// <summary>
    /// A single data point: the number of MU delivered and the corresponding
    /// leakage-corrected electrometer reading in nC.
    /// </summary>
    public class LinearityPoint
    {
        public double MU { get; set; }
        public double CorrectedReading { get; set; }
    }

    /// <summary>
    /// The output of a linear regression — slope, intercept, R² and a
    /// pre-formatted equation string ready for display.
    /// </summary>
    public class RegressionResult
    {
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double RSquared { get; set; }

        /// <summary>Human-readable equation, e.g. "y = 0.01234x + 0.00056"</summary>
        public string Equation => $"y = {Slope:F5}x + {Intercept:F5}";
    }

    // -------------------------------------------------------------------------
    // Physics Calculator
    // -------------------------------------------------------------------------

    /// <summary>
    /// Static helper that performs the physics and statistics calculations
    /// for the linearity test.  All methods are pure functions with no
    /// side-effects, making them easy to unit-test in isolation.
    /// </summary>
    public static class PhysicsCalculator
    {
        /// <summary>
        /// Assumed linac dose rate used to convert MU → delivery time for
        /// leakage correction.  10 MU/s is standard for most clinical beams.
        /// Change this constant if your machines operate at a different rate.
        /// </summary>
        public const double DoseRateMuPerSec = 600.0;

        /// <summary>
        /// Tolerance for the reading-per-MU percent difference, expressed as
        /// a percentage.  Results outside this band are flagged as FAIL.
        /// </summary>
        public const double PercentDiffTolerance = 2.0;

        /// <summary>
        /// Calculates the average leakage rate (nC/s) from up to two
        /// background measurements.  Returns null if no valid data is supplied.
        /// </summary>
        /// <param name="reading1">Electrometer reading for measurement 1 (nC)</param>
        /// <param name="time1">Collection time for measurement 1 (seconds)</param>
        /// <param name="reading2">Electrometer reading for measurement 2 (nC)</param>
        /// <param name="time2">Collection time for measurement 2 (seconds)</param>
        public static double? CalculateLeakageRate(
            double? reading1, double? time1,
            double? reading2, double? time2)
        {
            double total = 0;
            int count = 0;

            // Only include a measurement if a positive time interval was given.
            if (time1 > 1e-10 && reading1.HasValue) { total += reading1.Value / time1.Value; count++; }
            if (time2 > 1e-10 && reading2.HasValue) { total += reading2.Value / time2.Value; count++; }

            return count > 0 ? total / count : null;
        }

        /// <summary>
        /// Fits a straight line y = mx + c to the supplied data points using
        /// ordinary least squares regression.  Returns null if fewer than two
        /// points are given or if the calculation is geometrically degenerate.
        /// </summary>
        public static RegressionResult CalculateRegression(IList<LinearityPoint> points)
        {
            if (points == null || points.Count < 2) return null;

            int n = points.Count;
            double sumX = points.Sum(p => p.MU);
            double sumY = points.Sum(p => p.CorrectedReading);
            double sumXY = points.Sum(p => p.MU * p.CorrectedReading);
            double sumXX = points.Sum(p => p.MU * p.MU);
            double sumYY = points.Sum(p => p.CorrectedReading * p.CorrectedReading);

            double denominator = n * sumXX - sumX * sumX;
            if (Math.Abs(denominator) < 1e-10) return null; // All MU values are identical — can't fit.

            double slope = (n * sumXY - sumX * sumY) / denominator;
            double intercept = (sumY - slope * sumX) / n;

            // Pearson r, then squared to give R²
            double rNum = n * sumXY - sumX * sumY;
            double rDen = Math.Sqrt((n * sumXX - sumX * sumX) * (n * sumYY - sumY * sumY));
            double rSq = rDen > 1e-10 ? Math.Pow(rNum / rDen, 2) : 0;

            return new RegressionResult { Slope = slope, Intercept = intercept, RSquared = rSq };
        }
    }
}