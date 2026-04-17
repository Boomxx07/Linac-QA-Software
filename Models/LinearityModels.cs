// Purpose: Data models and physics calculations for the Linearity QA test.
//
// Keeping the math here (Model layer) means the ViewModel only has to
// prepare inputs and display outputs — it does not need to understand
// the physics or statistics involved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
        public float MU { get; set; }
        public float CorrectedReading { get; set; }
    }

    /// <summary>
    /// The output of a linear regression — slope, intercept, R² and a
    /// pre-formatted equation string ready for display.
    /// </summary>
    public class RegressionResult
    {
        public float Slope { get; set; }
        public float Intercept { get; set; }
        public float RSquared { get; set; }

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
        public static float DoseRateMuPerSec(string energyName)
        {
            if (energyName == "6FFF")
            {
                return 1400 / 60; // Convert to MU/sec
            }
            else
                return 600 / 60; // Convert to MU/sec for all other beams
        }

        /// <summary>
        /// Calculates the average leakage rate (nC/s) from up to two
        /// background measurements.  Returns null if no valid data is supplied.
        /// </summary>
        /// <param name="reading1">Electrometer reading for measurement 1 (nC)</param>
        /// <param name="time1">Collection time for measurement 1 (seconds)</param>
        /// <param name="reading2">Electrometer reading for measurement 2 (nC)</param>
        /// <param name="time2">Collection time for measurement 2 (seconds)</param>
        public static float? CalculateLeakageRate(
            float? reading1, float? time1,
            float? reading2, float? time2)
        {
            float leakage = 0;

            // Only include a measurement if both times and readings exist (and time 2 > time 1).
            if (time1 >= 0 && time2 > 1e-10 && time2 > time1 && reading1.HasValue && reading2.HasValue) {
                leakage = (reading2.Value - reading1.Value) / (time2.Value - time1.Value);
            }

            return leakage;
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
            float sumX = points.Sum(p => p.MU);
            float sumY = points.Sum(p => p.CorrectedReading);
            float sumXY = points.Sum(p => p.MU * p.CorrectedReading);
            float sumXX = points.Sum(p => p.MU * p.MU);
            float sumYY = points.Sum(p => p.CorrectedReading * p.CorrectedReading);

            float denominator = n * sumXX - sumX * sumX;
            if (Math.Abs(denominator) < 1e-10) return null; // All MU values are identical — can't fit.

            float slope = (n * sumXY - sumX * sumY) / denominator;
            float intercept = (sumY - slope * sumX) / n;

            // Pearson r, then squared to give R²
            float rNum = n * sumXY - sumX * sumY;
            float rDen = (float)Math.Sqrt((n * sumXX - sumX * sumX) * (n * sumYY - sumY * sumY));
            float rSq = (float)(rDen > 1e-10 ? Math.Pow(rNum / rDen, 2) : 0);

            return new RegressionResult { Slope = slope, Intercept = intercept, RSquared = rSq };
        }
    }
}