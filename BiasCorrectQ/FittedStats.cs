using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiasCorrectQ
{
class FittedStats
{
    public double fittedmean
    {
        get;
        private set;
    }

    public double fittedstd
    {
        get;
        private set;
    }

    public double mean
    {
        get;
        private set;
    }

    public double std
    {
        get;
        private set;
    }

    public double alpha
    {
        get;
        private set;
    }

    public double zeta
    {
        get;
        private set;
    }

    public FittedStats(List<double> values)
    {
        var sum = new double[4];
        for (int i = 0; i < values.Count; i++)
        {
            double val = values[i];
            double log_val = Math.Log(val);
            sum[0] += log_val;
            sum[1] += log_val * log_val;
            sum[2] += val;
            sum[3] += val * val;
        }

        double nvals = values.Count;
        fittedmean = sum[0] / nvals;
        fittedstd = Math.Sqrt((sum[1] - sum[0] * sum[0] / nvals) / (nvals - 1));
        mean = sum[2] / nvals;
        std = Math.Sqrt((sum[3] - sum[2] * sum[2] / nvals) / (nvals - 1));
        alpha = 0.7797 * std;
        zeta = mean - 0.5772 * alpha;
    }

} //class
} //namespace
