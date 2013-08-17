using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncSubsByComparison
{
    public class MyLine
    {
        public MyLine(double s, double yIntersect)
        {
            Slope = s;
            YIntersect = yIntersect;
        }
        public double Slope { get; set; }
        public double YIntersect { get; set; }
    }

    public class MyPoint
    {
        private double x;
        private double y;

        public MyPoint() 
        {
            Init(0.0, 0.0);
        }

        public MyPoint(double x, double y)
        {
            Init(x, y);
        }

        public void Init(double x, double y)
        {
            this.X = x; this.Y = y; 
        }

        public double X
        {
            get { return x; }
            set { x = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() + this.Y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var p = obj as MyPoint;
            if (p == null)
                return false;
            return p.X == this.X && p.Y == this.Y;
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }
}