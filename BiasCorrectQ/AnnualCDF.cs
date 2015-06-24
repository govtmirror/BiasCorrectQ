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
        set;
    }
    public List<double> Flow
    {
        get;
        set;
    }

    public AnnualCDF(List<Point> points)
    {
        var values = Utils.GetAnnualAverages(points);

        List<double> sorted_values;
        var cdf = Utils.ComputeCDF(values, out sorted_values);

        Probability = cdf;
        Flow = sorted_values;
    }

} //namespace
} //class
