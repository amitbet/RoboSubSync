using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;


namespace SyncSubsByComparison
{

    public class SampleCollection : IList<MyPoint>
    {

        private List<MyPoint> _points = new List<MyPoint>();

        public SampleCollection() : this(null, null) { }

        public SampleCollection(IEnumerable<MyPoint> points)
        {
            foreach (var point in points)
                Add(point);
        }

        private Dictionary<double, string> _pointDescByXValue;

        public Dictionary<double, string> PointDescByXValue
        {
            get { return _pointDescByXValue; }
            set { _pointDescByXValue = value; }
        }

        public override string ToString()
        {
            string str = _points.Select(p => "(" + p.X + "," + p.Y + ")").Aggregate((y1, y2) => y1 + " " + y2);
            return str;
        }

        public SampleCollection(IEnumerable<double> Xaxis, IEnumerable<double> Yaxis)
        {
            if (Xaxis != null || Yaxis != null)
            {
                if (Xaxis.Count() != Yaxis.Count())
                    throw new Exception("X and Y must be of the same size!");

                for (int i = 0; i < Xaxis.Count(); ++i)
                    Add(new MyPoint(Xaxis.ElementAt(i), Yaxis.ElementAt(i)));
            }
        }

        public int Count { get { return _points.Count; } }
        public bool IsReadOnly { get { return false; } }

        public MyPoint this[int index]
        {
            get { return _points[index]; }
            set
            {
                var p = value;
                var old = _points[index];
                _points[index] = p;

            }
        }

        public void Add(double x, double y)
        {
            Add(new MyPoint(x, y));
        }

        public void Add(MyPoint p)
        {

            var prevPoint = _points.LastOrDefault();
            if (prevPoint == null || prevPoint.X != p.X)
                _points.Add(p);
        }

        public void Clear()
        {
            _points.Clear();
        }
        public SubtitleInfo CreateFixedSubtitle(SubtitleInfo subtitle)
        {

            var resultSub = subtitle.CloneSub();
            foreach (var time in resultSub.TimeMarkers)
            {
                time.IsOffsetCorrected = true;
                time.Correction = (long)ComputeYforXbyInterpolation(time.FromTime);
            }
            return resultSub;

        }

        /// <summary>
        /// creates a step line by clutering the points.
        /// </summary>
        /// <param name="k">the number of steps we will end up with</param>
        /// <param name="numberOfPointsInNextLineToEndCurrentLine">the number of consecutive points which belong to the next group required in order to climb to the next step</param>
        /// <returns></returns>
        public SampleCollection GetStepLineByKMeans(int k, int numberOfPointsInNextLineToEndCurrentLine = 3, bool requireConsecutivePointsToClimb = true, bool removeRedundantStepLines = true)
        {
            var yVals = _points.Select(p => p.Y);
            var kmeans = OneDimentinalKMeans(yVals, k);
            List<MyPoint> stepLinePoints = new List<MyPoint>();
            if (removeRedundantStepLines)
                kmeans = RemoveRedundantClusters(kmeans);

            //var groups = _points.GroupBy(p => groupAssignments[p.Y]);
            var listOfPoints = _points.Select(p => new { Point = p, Cluster = kmeans.GroupAssignments[p.Y] }).ToList();



            int increment = listOfPoints.First().Cluster == 0 ? 1 : -1;
            int startCluster = listOfPoints.First().Cluster == 0 ? 0 : kmeans.Means.Count - 1;
            int start = listOfPoints.First().Cluster == 0 ? 0 : listOfPoints.Count - 1;
            int climbStepCounter = 0;
            int currentCluster = startCluster;

            stepLinePoints.Add(new MyPoint(listOfPoints.First().Point.X, kmeans.Means[startCluster]));

            //estimate an end points for each line
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                //count next cluster points, towards switch
                if (listOfPoints[i].Cluster == currentCluster + increment)
                    ++climbStepCounter;

                //we said consecutive...
                if (listOfPoints[i].Cluster == currentCluster && requireConsecutivePointsToClimb)
                    climbStepCounter = 0;

                //go to next step
                if (climbStepCounter == numberOfPointsInNextLineToEndCurrentLine)
                {
                    stepLinePoints.Add(new MyPoint(listOfPoints[i].Point.X, kmeans.Means[currentCluster]));

                    currentCluster += increment;
                    stepLinePoints.Add(new MyPoint(listOfPoints[i].Point.X + 0.00001, kmeans.Means[currentCluster]));
                    climbStepCounter = 0;
                }
            }
            stepLinePoints.Add(new MyPoint(listOfPoints.Last().Point.X, kmeans.Means[currentCluster]));

            var retVal = new SampleCollection(stepLinePoints);
            return retVal;

        }

        private KMeansResult RemoveRedundantClusters(KMeansResult kmeans, int precentOfSamplesConsideredTooSmall = 5)
        {

            var fivePercent = ((double)kmeans.GroupAssignments.Count()) * ((double)precentOfSamplesConsideredTooSmall / 100d);
            var groups = kmeans.GroupAssignments.GroupBy(p => p.Value);
            var removedGroups = groups.Where(g => g.Count() < fivePercent).Select(g => g.Key);
            var remainingGroups = groups.Where(g => g.Count() >= fivePercent).Select(g => g.Key);
            var switchMap = new Dictionary<int, int>();

            //create a map so we can reassign the points
            foreach (var group in removedGroups)
            {
                for (int i = group - 1; i >= 0; i--)
                    if (remainingGroups.Contains(i))
                    {
                        switchMap.Add(group, i);
                        break;
                    }

                if (!switchMap.ContainsKey(group))
                    for (int i = group + 1; i <= kmeans.Means.Count; i++)
                        if (remainingGroups.Contains(i))
                        {
                            switchMap.Add(group, i);
                            break;
                        }
            }

            int idx = 0;
            foreach (var group in remainingGroups.OrderBy(g => g))
            {
                switchMap.Add(group, idx);
                ++idx;
            }

            //unpdate switchmap with the index changes of the remaining groups
            foreach (var key in switchMap.Keys.ToList())
            {
                if (removedGroups.Contains(key))
                {
                    switchMap[key] = switchMap[switchMap[key]];
                }
            }

            //remove groups from means list
            List<double> newMeans = kmeans.Means.Where((m, i) => remainingGroups.Contains(i)).ToList();

            Dictionary<double, int> gAssignments = new Dictionary<double, int>();
            kmeans.GroupAssignments.ToList().ForEach(a =>
            {
                gAssignments.Add(a.Key, switchMap[a.Value]);
            });

            return new KMeansResult() { Means = newMeans, GroupAssignments = gAssignments };
        }

        public class KMeansResult
        {
            public Dictionary<double, int> GroupAssignments { get; set; }
            public List<double> Means { get; set; }
        }

        private KMeansResult OneDimentinalKMeans(IEnumerable<double> values, int k)
        {

            var ordered = values.OrderBy(x => x);
            var first = ordered.First();
            var last = ordered.Last();
            var sampleRange = last - first;
            var partitionSize = sampleRange / (double)k;
            List<double> means = Enumerable.Repeat(0.0, k).ToList();

            for (int i = 0; i < k; i++)
            {
                means[i] = i * partitionSize + first;
            }

            double iterChange = double.PositiveInfinity;

            //iterate until little has changed
            while (iterChange > 0.001)
            {
                var newMeans = PerformKMeansIteration(values, k, means);

                iterChange = 0;
                for (int i = 0; i < means.Count; ++i)
                {
                    iterChange += Math.Abs(newMeans[i] - means[i]);
                }

                means = newMeans;
            }

            KMeansResult res = new KMeansResult() { GroupAssignments = GetCluseters(values, means), Means = means };

            return res;
        }

        private Dictionary<double, int> GetCluseters(IEnumerable<double> values, List<double> means)
        {
            var groupping = new Dictionary<double, int>();

            //go over all vals 
            foreach (var val in values)
            {
                int bestMeanIdx = 0;
                double minDiff = double.PositiveInfinity;
                //find the best mean for val
                for (int i = 0; i < means.Count; ++i)
                {
                    var mean = means[i];
                    double diff = Math.Abs(mean - val);
                    if (minDiff > diff)
                    {
                        minDiff = diff;
                        bestMeanIdx = i;
                    }
                }
                if (!groupping.ContainsKey(val))
                    groupping.Add(val, bestMeanIdx);
            }

            return groupping;
        }

        private List<double> PerformKMeansIteration(IEnumerable<double> values, int k, List<double> means)
        {
            List<double> newMeans = Enumerable.Repeat(0.0, k).ToList();
            List<double> newMeansCount = Enumerable.Repeat(0.0, k).ToList();

            //go over all vals 
            foreach (var val in values)
            {
                int bestMeanIdx = 0;
                double minDiff = double.PositiveInfinity;
                //find the best mean for val
                for (int i = 0; i < means.Count; ++i)
                {
                    var mean = means[i];
                    double diff = Math.Abs(mean - val);
                    if (minDiff > diff)
                    {
                        minDiff = diff;
                        bestMeanIdx = i;
                    }
                }
                newMeansCount[bestMeanIdx]++;
                newMeans[bestMeanIdx] += val;
            }

            for (int i = 0; i < means.Count; ++i)
            {
                newMeans[i] = newMeans[i] / newMeansCount[i];
            }

            return newMeans;
        }


        private SubtitleInfo xCreateFixedSubtitle(SubtitleInfo subtitle)
        {
            var resultSub = subtitle.CloneSub();
            var correctionsByTimePos = new Dictionary<double, double>();
            _points.ForEach(p => correctionsByTimePos.Add(p.X, p.Y));

            //place all known corrections on the subtitle
            resultSub.TimeMarkers.ForEach(t =>
                {
                    if (correctionsByTimePos.ContainsKey((double)t.FromTime))
                    {
                        var correction = correctionsByTimePos[(double)t.FromTime];
                        t.Correction = (long)correction;
                        t.IsOffsetCorrected = true;
                    }
                });

            //spread correction to all timestamps (including the ones not attached)
            long prevOffset = (long)_points[0].Y;
            TimeStamp prev = null;

            foreach (var time in resultSub.TimeMarkers)
            {
                if (!time.IsOffsetCorrected)
                {
                    var next = subtitle.Lines.Where(p => p.TimeStamp.FromTime > time.FromTime).FirstOrDefault(x => x.TimeStamp.IsOffsetCorrected);
                    var currTime = time;
                    var prevTime = prev;
                    double newOffset = prevOffset;

                    if (prevTime != null && next != null)
                    {
                        var nextTime = next.TimeStamp;

                        //timeAfterPrev / timeInterval (=next-prev) = the precentage of movement in the X axis between the two points
                        double part = ((double)currTime.FromTime - (double)prevTime.FromTime) / ((double)nextTime.FromTime - (double)prevTime.FromTime);

                        //(change in corrections between prev -> next) * (calculated place between them =part) + (the base correction of prev =the stating point of this correction)
                        newOffset = (((double)nextTime.Correction - (double)prev.Correction) * part) + (double)prevTime.Correction;
                    }

                    time.IsOffsetCorrected = true;
                    time.Correction = (long)newOffset;
                }
                else
                {
                    prevOffset = time.Correction;
                    prev = time;
                }

                //extend duration for all subs
                time.Duration = (long)((double)time.Duration);
            }
            return resultSub;

        }

        public SampleCollection FilterAbnormalsByRegression()
        {
            var regLine = ComputeRegressionLine();
            var points = new List<MyPoint>();
            var std = StandardDeviation(_points.Select(p => p.X).ToArray()) / (_points.Count);
            for (int i = 0; i < _points.Count; ++i)
            {
                double x = _points[i].X;
                double y = _points[i].Y;
                var regrY = ComputeRegressionYforX(x, regLine);
                bool isAbnormal = (Math.Abs(regrY - y) > std * 3);

                if (!isAbnormal)
                    points.Add(new MyPoint(_points[i].X, _points[i].Y));
            }
            var col = new SampleCollection(points);
            return col;
        }

        public SampleCollection FilterAbnormalsByBaseline(double alpha, double normalityAmp, int startSectionLength)
        {
            Baseline baseline = Baseline.CreateBaseline(_points.Select(p => (long)p.Y).ToList(), 7, startSectionLength, alpha, normalityAmp);

            var points = new List<MyPoint>();
            for (int i = 0; i < baseline.Averages.Count; ++i)
            {
                if (!baseline.AbnormalPoints.Contains(i))
                    points.Add(new MyPoint(_points[i].X, _points[i].Y));
            }

            var col = new SampleCollection(points);
            col._pointDescByXValue = this._pointDescByXValue;

            return col;
        }

        public SampleCollection GetBaseline(double alpha, double normalityAmp, int startSectionLength)
        {
            Baseline baseline = Baseline.CreateBaseline(_points.Select(p => (long)p.Y).ToList(), 7, startSectionLength, alpha, normalityAmp);

            var points = new List<MyPoint>();
            for (int i = 0; i < baseline.Averages.Count; ++i)
            {
                points.Add(new MyPoint(_points[i].X, baseline.Averages[i]));
            }

            var col = new SampleCollection(points);
            return col;
        }

        public SampleCollection GetLinearRegression()
        {
            var regLine = ComputeRegressionLine();
            List<MyPoint> points = new List<MyPoint>();

            foreach (var point in _points)
                points.Add(new MyPoint(point.X, ComputeRegressionYforX(point.X, regLine)));

            var col = new SampleCollection(points);
            return col;
        }

        private double ComputeRegressionYforX(double x, MyLine regressionLine)
        {
            return x * regressionLine.Slope + regressionLine.YIntersect;
        }

        public double ComputeYforXbyInterpolation(double x)
        {
            var sectEndPoint = _points.FirstOrDefault(p => p.X > x);
            MyPoint sectStartPoint = null;
            if (sectEndPoint == null)
            {
                sectStartPoint = _points[_points.Count - 2];
                sectEndPoint = _points[_points.Count - 1];
            }
            else
            {
                int starPointIdx = _points.IndexOf(sectEndPoint) - 1;

                if (starPointIdx == -1)
                {
                    return sectEndPoint.Y;
                }

                sectStartPoint = _points[starPointIdx];
            }
            //var precent = (x - sectStartPoint.X) / sectEndPoint.X;
            double slope = (sectEndPoint.Y - sectStartPoint.Y) / (sectEndPoint.X - sectStartPoint.X);
            double yIntersect = sectEndPoint.Y - slope * sectEndPoint.X;

            //var newPointY1 = sectStartPoint.Y * (1d - precent) + sectEndPoint.Y * precent;
            var newPointY = slope * x + yIntersect;//sectStartPoint.Y * (1d - precent) + sectEndPoint.Y * precent;
            return newPointY;
        }


        public MyLine ComputeRegressionLine()
        {
            double XSquaredSum = 0.0;
            double YSum = 0.0;
            double XSum = 0.0;
            double XYProductSum = 0.0;
            double XMin = double.PositiveInfinity;
            double XMax = double.NegativeInfinity;

            foreach (var point in _points)
            {
                XSquaredSum += point.X * point.X;
                YSum += point.Y;
                XSum += point.X;
                XYProductSum += point.Y * point.X;
                XMin = XMin > point.X ? point.X : XMin;
                XMax = XMax < point.X ? point.X : XMax;
            }

            double delta = Count * XSquaredSum - Math.Pow(XSum, 2.0);
            var yIntersect = (1.0 / delta) * (XSquaredSum * YSum - XSum * XYProductSum);
            var slope = (1.0 / delta) * (Count * XYProductSum - XSum * YSum);

            MyLine l = new MyLine(slope, yIntersect);
            return l;
        }


        private static double StandardDeviation(double[] X)
        {
            if (X.Length > 1)
            {
                var mean = X.Average();
                var sum = X.Sum(x => Math.Pow(x - mean, 2));
                var var = sum / (X.Length - 1);

                return Math.Sqrt(var);
            }
            else
                return 0.0;
        }

        public bool Contains(MyPoint p)
        {
            return _points.Contains(p);
        }

        public void CopyTo(MyPoint[] points, int index)
        {
            _points.CopyTo(points, index);
        }

        public IEnumerator<MyPoint> GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        public int IndexOf(MyPoint p)
        {
            return _points.IndexOf(p);
        }

        public void Insert(int index, MyPoint p)
        {
            _points.Insert(index, p);
        }

        public bool Remove(MyPoint p)
        {
            var success = _points.Remove(p);
            return success;
        }

        public void RemoveAt(int index)
        {
            _points.RemoveAt(index);
        }
    }


}