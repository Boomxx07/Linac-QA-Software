// Purpose: Data models and physics calculations for the Output Factor QA test.
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
    /// A single output factor measurement with field size and readings.
    /// </summary>
    public class OutputFactorPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float OutputFactor { get; set; }
        public bool IsSymmetric => Math.Abs(X - Y) < 0.01f;
        public bool IsMLC { get; set; }

        /// <summary>Square field size in cm (for symmetric fields).</summary>
        public float SquareFieldSize => (X + Y) / 2f;
    }

    // -------------------------------------------------------------------------
    // Physics Calculator
    // -------------------------------------------------------------------------

    /// <summary>
    /// Static helper that performs physics and statistics calculations
    /// for the output factor test.
    /// </summary>
    public static class OutputFactorCalculator
    {
        /// <summary>
        /// Calculates the output factor as the ratio of the average reading
        /// to the reference (10x10) average.
        /// </summary>
        /// <param name="measurementAverage">Average reading for this field size</param>
        /// <param name="referenceAverage">Average reading from 10x10 reference measurements</param>
        /// <returns>Output factor (dimensionless ratio)</returns>
        public static float? CalculateOutputFactor(float? measurementAverage, float? referenceAverage)
        {
            if (!measurementAverage.HasValue || !referenceAverage.HasValue)
                return null;

            if (Math.Abs(referenceAverage.Value) < 1e-10)
                return null; // Avoid division by zero

            return measurementAverage.Value / referenceAverage.Value;
        }
    }
}
