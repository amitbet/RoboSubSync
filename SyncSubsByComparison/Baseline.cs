using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncSubsByComparison
{
    public class Baseline
    {
        public List<double> Averages { get; set; }
        public HashSet<long> AbnormalPoints { get; set; }

        static double CalcSumOfSquares(IEnumerable<long> series, double average)
        {
            double sumOfSquares = 0;
            foreach (var item in series)
            {
                sumOfSquares += Math.Pow(item - average, 2);
            }
            return sumOfSquares;
        }

        static double CalcAverage(IEnumerable<long> series)
        {
            return ((double)series.Aggregate((a, b) => a + b) / (double)series.Count());
        }


        ///Note: Stddev[t] = sqrt(sumOfSquares[t] – average[t]^2)
        static private double CalcStddev(double sumOfSquares, double average)
        {
            return Math.Sqrt(sumOfSquares - Math.Pow(average, 2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="numErrorsToBacktrackOn"></param>
        /// <param name="initialSectionLength"></param>
        /// <param name="algAlpha">the forgetfullness constant, controls the impact of every new sample on the average and deviation, as well as the (much smaller) impact of abnormal samples (should be between 0.01 and 0.05)</param>
        /// <param name="normalcyAmplitudeConstant">the number of standart deviations from the average line in which a sample is considered ok to include in calculation, this creates a zone surrounding the average from above and below which is the allowed zone for samples, should usually be 3</param>
        /// <returns></returns>
        public static Baseline CreateBaseline(List<long> series, int numErrorsToBacktrackOn, int initialSectionLength, double algAlpha = 0.02d, double normalcyAmplitudeConstant = 3)
        {
            List<double> averageValues = new List<double>();
            var leadIn = series.Take(initialSectionLength);
            double average = CalcAverage(leadIn);
            double sumOfSquares = CalcSumOfSquares(leadIn, average);
            for (int i = 0; i < initialSectionLength; i++)
                averageValues.Add(average);

            //algAlpha = 0.02d;
            double alpha = algAlpha;
            int errorCount = 0;

            List<long> abnormalValuePositions = new List<long>();

            for (int i = initialSectionLength; i < series.Count(); ++i)
            {
                var sample = series[i];
                var stdDeviation = CalcStddev(sumOfSquares, average);
                double distanceFromAvg = Math.Abs(sample - average);

                //an abnormal value is found
                if (distanceFromAvg > normalcyAmplitudeConstant * stdDeviation)
                {
                    alpha = Math.Pow(algAlpha, (distanceFromAvg / (stdDeviation)) - (normalcyAmplitudeConstant - 1));

                    ++errorCount;
                    abnormalValuePositions.Add(i);

                    //if too many errors occured, go back to where we lost it, and clear the averages, so we start fresh.
                    if (errorCount >= numErrorsToBacktrackOn)
                    {
                        //release all errors tags in list until the rollback point
                        abnormalValuePositions.RemoveRange(abnormalValuePositions.Count - numErrorsToBacktrackOn, numErrorsToBacktrackOn);
                        averageValues.RemoveRange(averageValues.Count - numErrorsToBacktrackOn, numErrorsToBacktrackOn);
                        errorCount = 0;

                        //rollback i
                        i -= (numErrorsToBacktrackOn + 1);

                        var errSamples = series.Where((x, idx) => idx > i && idx <= numErrorsToBacktrackOn + i);
                        //reset values
                        average = CalcAverage(errSamples);
                        sumOfSquares = CalcSumOfSquares(errSamples, average);

                        continue;
                    }
                }
                //normal values
                else
                {
                    errorCount = 0;
                    alpha = algAlpha;
                }
                averageValues.Add(average);

                average = alpha * sample + (1 - alpha) * average;
                sumOfSquares = alpha * Math.Pow(sample, 2) + (1 - alpha) * sumOfSquares;
            }
            var abnormalPointsHash = new HashSet<long>();
            abnormalValuePositions.ForEach(a => abnormalPointsHash.Add(a));
            return new Baseline() { AbnormalPoints = abnormalPointsHash, Averages = averageValues };
        }

    }
}