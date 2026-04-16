using System;
using System.Collections.Generic;
using System.Linq;

namespace Linac_QA_Software.Models
{
    public class LinearityPoint
    {
        public double MU { get; set; }
        public double CorrectedReading { get; set; }
    }

    public class RegressionResult
    {
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double RSquared { get; set; }
        public string Equation => $"y = {Slope:F5}x + {Intercept:F5}";
    }

    public static class LinearityPhysicsCalculator
    {
        public static RegressionResult CalculateRegression(List<LinearityPoint> points)
        {
            if (points.Count < 2) return null;

            int n = points.Count;
            double sumX = points.Sum(p => p.MU);
            double sumY = points.Sum(p => p.CorrectedReading);
            double sumXY = points.Sum(p => p.MU * p.CorrectedReading);
            double sumXX = points.Sum(p => p.MU * p.MU);
            double sumYY = points.Sum(p => p.CorrectedReading * p.CorrectedReading);

            double denominator = (n * sumXX - sumX * sumX);
            if (Math.Abs(denominator) < 1e-10) return null;

            double slope = (n * sumXY - sumX * sumY) / denominator;
            double intercept = (sumY - slope * sumX) / n;

            double rNum = (n * sumXY - sumX * sumY);
            double rDen = Math.Sqrt((n * sumXX - sumX * sumX) * (n * sumYY - sumY * sumY));
            double rSq = rDen != 0 ? Math.Pow(rNum / rDen, 2) : 0;

            return new RegressionResult { Slope = slope, Intercept = intercept, RSquared = rSq };
        }
    }
}