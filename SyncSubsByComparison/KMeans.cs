using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncSubsByComparison
{
    public class KMeans
    {
        private static int[] InitClustering(int numTuples, int numClusters, int randomSeed)
        {
            // assign each tuple to a random cluster, making sure that there's at least
            // one tuple assigned to every cluster
            Random random = new Random(randomSeed);
            int[] clustering = new int[numTuples];

            // assign first numClusters tuples to clusters 0..k-1
            for (int i = 0; i < numClusters; ++i)
                clustering[i] = i;
            // assign rest randomly
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters);
            return clustering;
        }

        private static double[][] Allocate(int numClusters, int numAttributes)
        {
            // helper allocater for means[][] and centroids[][]
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numAttributes];
            return result;
        }

        private static void UpdateMeans(double[][] rawData, int[] clustering, double[][] means)
        {
            // assumes means[][] exists. consider making means[][] a ref parameter
            int numClusters = means.Length;
            // zero-out means[][]
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] = 0.0;

            // make an array to hold cluster counts
            int[] clusterCounts = new int[numClusters];

            // walk through each tuple, accumulate sum for each attribute, update cluster count
            for (int i = 0; i < rawData.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];

                for (int j = 0; j < rawData[i].Length; ++j)
                    means[cluster][j] += rawData[i][j];
            }

            // divide each attribute sum by cluster count to get average (mean)
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] /= clusterCounts[k];  // will throw if count is 0. consider an error-check

            return;
        } // UpdateMeans

        private static double[] ComputeCentroid(double[][] rawData, int[] clustering, int cluster, double[][] means)
        {
            // the centroid is the actual tuple values that are closest to the cluster mean
            int numAttributes = means[0].Length;
            double[] centroid = new double[numAttributes];
            double minDist = double.MaxValue;
            for (int i = 0; i < rawData.Length; ++i) // walk thru each data tuple
            {
                int c = clustering[i];  // if curr tuple isn't in the cluster we're computing for, continue on
                if (c != cluster) continue;

                double currDist = Distance(rawData[i], means[cluster]);  // call helper
                if (currDist < minDist)
                {
                    minDist = currDist;
                    for (int j = 0; j < centroid.Length; ++j)
                        centroid[j] = rawData[i][j];
                }
            }
            return centroid;
        }

        private static void UpdateCentroids(double[][] rawData, int[] clustering, double[][] means, double[][] centroids)
        {
            // updates all centroids by calling helper that updates one centroid
            for (int k = 0; k < centroids.Length; ++k)
            {
                double[] centroid = ComputeCentroid(rawData, clustering, k, means);
                centroids[k] = centroid;
            }
        }

        private static double Distance(double[] tuple, double[] vector)
        {
            // Euclidean distance between an actual data tuple and a cluster mean or centroid
            double sumSquaredDiffs = 0.0;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Math.Pow((tuple[j] - vector[j]), 2);
            return Math.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(double[] distances)
        {
            // index of smallest value in distances[]
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k]; indexOfMin = k;
                }
            }
            return indexOfMin;
        }

        private static bool Assign(double[][] rawData, int[] clustering, double[][] centroids)
        {
            // assign each tuple to best cluster (closest to cluster centroid)
            // return true if any new cluster assignment is different from old/curr cluster
            // does not prevent a state where a cluster has no tuples assigned. see article for details
            int numClusters = centroids.Length;
            bool changed = false;

            double[] distances = new double[numClusters]; // distance from curr tuple to each cluster mean
            for (int i = 0; i < rawData.Length; ++i)      // walk thru each tuple
            {
                for (int k = 0; k < numClusters; ++k)       // compute distances to all centroids
                    distances[k] = Distance(rawData[i], centroids[k]);

                int newCluster = MinIndex(distances);  // find the index == custerID of closest 
                if (newCluster != clustering[i]) // different cluster assignment?
                {
                    changed = true;
                    clustering[i] = newCluster;
                } // else no change
            }
            return changed; // was there any change in clustering?
        } // Assign

        public static int[] Cluster(double[][] rawData, int numClusters, int numAttributes, int maxCount)
        {
            bool changed = true;
            int ct = 0;

            int numTuples = rawData.Length;
            int[] clustering = InitClustering(numTuples, numClusters, 0);  // 0 is a seed for random
            double[][] means = Allocate(numClusters, numAttributes);       // just makes things a bit cleaner
            double[][] centroids = Allocate(numClusters, numAttributes);
            UpdateMeans(rawData, clustering, means);                       // could call this inside UpdateCentroids instead
            UpdateCentroids(rawData, clustering, means, centroids);

            while (changed == true && ct < maxCount)
            {
                ++ct;
                changed = Assign(rawData, clustering, centroids); // use centroids to update cluster assignment
                UpdateMeans(rawData, clustering, means);  // use new clustering to update cluster means
                UpdateCentroids(rawData, clustering, means, centroids);  // use new means to update centroids
            }
            //ShowMatrix(centroids, centroids.Length, true);  // show the final centroids for each cluster
            return clustering;
        }

        private static double[] Outlier(double[][] rawData, int[] clustering, int numClusters, int cluster)
        {
            // return the tuple values in cluster that is farthest from cluster centroid
            int numAttributes = rawData[0].Length;

            double[] outlier = new double[numAttributes];
            double maxDist = 0.0;

            double[][] means = Allocate(numClusters, numAttributes);
            double[][] centroids = Allocate(numClusters, numAttributes);
            UpdateMeans(rawData, clustering, means);
            UpdateCentroids(rawData, clustering, means, centroids);

            for (int i = 0; i < rawData.Length; ++i)
            {
                int c = clustering[i];
                if (c != cluster) continue;
                double dist = Distance(rawData[i], centroids[cluster]);
                if (dist > maxDist)
                {
                    maxDist = dist;  // might also want to return (as an out param) the index of rawData
                    Array.Copy(rawData[i], outlier, rawData[i].Length);
                }
            }
            return outlier;
        }

        // display routines below

        private static void ShowMatrix(double[][] matrix, int numRows, bool newLine)
        {
            for (int i = 0; i < numRows; ++i)
            {
                Console.Write("[" + i.ToString().PadLeft(2) + "]  ");
                for (int j = 0; j < matrix[i].Length; ++j)
                    Console.Write(matrix[i][j].ToString("F1") + "  ");
                Console.WriteLine("");
            }
            if (newLine == true) Console.WriteLine("");
        } // ShowMatrix

        private static void ShowVector(int[] vector, bool newLine)
        {
            for (int i = 0; i < vector.Length; ++i)
                Console.Write(vector[i] + " ");
            Console.WriteLine("");
            if (newLine == true) Console.WriteLine("");
        }

        private static void ShowVector(double[] vector, bool newLine)
        {
            for (int i = 0; i < vector.Length; ++i)
                Console.Write(vector[i].ToString("F1") + " ");
            Console.WriteLine("");
            if (newLine == true) Console.WriteLine("");
        }

        private static void ShowClustering(double[][] rawData, int numClusters, int[] clustering, bool newLine)
        {
            Console.WriteLine("-----------------");
            for (int k = 0; k < numClusters; ++k) // display by cluster
            {
                for (int i = 0; i < rawData.Length; ++i) // each tuple
                {
                    if (clustering[i] == k) // curr tuple i belongs to curr cluster k.
                    {
                        Console.Write("[" + i.ToString().PadLeft(2) + "]");
                        for (int j = 0; j < rawData[i].Length; ++j)
                            Console.Write(rawData[i][j].ToString("F1").PadLeft(6) + " ");
                        Console.WriteLine("");
                    }
                }
                Console.WriteLine("-----------------");
            }
            if (newLine == true) Console.WriteLine("");
        }
    }
}
