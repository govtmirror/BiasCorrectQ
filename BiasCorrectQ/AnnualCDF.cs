using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiasCorrectQ
{
class AnnualCDF
{
    public List<double> Probability
    {
        get;
        private set;
    }

    public List<double> Flow
    {
        get;
        private set;
    }

    public FittedStats FittedStats
    {
        get;
        private set;
    }

    public AnnualCDF(List<Point> points)
    {
        var values = Utils.GetWYAnnualAverages(points);

        List<double> sorted_values;
        var cdf = Utils.ComputeCDF(values, out sorted_values);

        Probability = cdf;
        Flow = sorted_values;
        FittedStats = new FittedStats(values);
    }

} //class
} //namespace
