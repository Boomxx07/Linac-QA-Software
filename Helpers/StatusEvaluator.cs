using System;

namespace Linac_QA_Software.Helpers
{
    /// <summary>
    /// Defines the possible outcomes of a QA test evaluation.
    /// </summary>
    public enum StatusText
    {
        OK,
        CAUTIONARY,
        FAIL
    }

    /// <summary>
    /// Logic for evaluating clinical measurements against physics tolerances.
    /// </summary>
    public static class StatusEvaluator
    {
        /// <summary>
        /// Evaluates a value relative to a baseline, using symmetric cautionary and fail bands.
        /// </summary>
        /// <param name="value">The measured value (e.g., Output, TPR, or Linearity point).</param>
        /// <param name="baseline">The reference or expected value.</param>
        /// <param name="failDiff">The maximum allowed deviation before a FAIL is triggered.</param>
        /// <param name="cautionaryDiff">The deviation that triggers a CAUTIONARY status (optional).</param>
        /// <returns>StatusText (OK, CAUTIONARY, or FAIL)</returns>
        public static StatusText EvaluateRelative(
            double value,
            double baseline,
            double failDiff,
            double? cautionaryDiff = null)
        {
            // 1. Calculate absolute deviation (Symmetric check)
            double diff = Math.Abs(value - baseline);

            // 2. Logic Protection: If the cautionary limit is incorrectly set 
            // higher than the fail limit, we treat the test as a binary Pass/Fail.
            if (cautionaryDiff.HasValue && cautionaryDiff.Value >= failDiff)
            {
                cautionaryDiff = null;
            }

            // 3. Evaluation 
            // We use >= because if a value hits the limit exactly, 
            // clinical protocol usually dictates it enters the more restrictive status.

            if (diff >= failDiff)
                return StatusText.FAIL;

            if (cautionaryDiff.HasValue && diff >= cautionaryDiff.Value)
                return StatusText.CAUTIONARY;

            return StatusText.OK;
        }

        /// <summary>
        /// Evaluates a value based on absolute percentage difference.
        /// Useful for Output constancy where tolerances are expressed as % (e.g. 2%).
        /// </summary>
        public static StatusText EvaluatePercent(
            double value,
            double baseline,
            double failPercent,
            double? cautionaryPercent = null)
        {
            if (baseline == 0) return StatusText.FAIL;

            double percentDiff = Math.Abs((value - baseline) / baseline) * 100.0;
            return EvaluateRelative(percentDiff, 0, failPercent, cautionaryPercent);
        }
    }
}