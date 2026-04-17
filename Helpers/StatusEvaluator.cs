using System;
using System.Collections.Generic;
using System.Text;

namespace Linac_QA_Software.Helpers
{
    public enum StatusText
    {
        OK,
        CAUTIONARY,
        FAIL
    }

    public static class StatusEvaluator
    {
        /// <summary>
        /// Evaluates a value relative to a baseline, using symmetric or one-sided cautionary/fail bands.
        /// If only failLimit is provided, result can be OK/FAIL.
        /// If both limits provided, result can be OK/CAUTIONARY/FAIL.
        /// </summary>
        /// <param name="value">Value to test</param>
        /// <param name="baseline">Baseline value to compare against</param>
        /// <param name="failDiff">Fail limit (distance from baseline, >= 0)</param>
        /// <param name="cautionaryDiff">Cautionary limit (distance from baseline, >= 0, optional)</param>
        /// <returns>StatusText (OK, CAUTIONARY, FAIL)</returns>
        public static StatusText EvaluateRelative(
            double value,
            double baseline,
            double failDiff,
            double? cautionaryDiff = null)
        {
            double diff = Math.Abs(value - baseline);

            if (cautionaryDiff.HasValue)
            {
                if (diff > failDiff)
                    return StatusText.FAIL;
                if (diff > cautionaryDiff.Value)
                    return StatusText.CAUTIONARY;
                return StatusText.OK;
            }
            else
            {
                if (diff > failDiff)
                    return StatusText.FAIL;
                return StatusText.OK;
            }
        }
    }
}
