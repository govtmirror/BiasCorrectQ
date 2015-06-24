using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiasCorrectQ
{
class MonthCDF
{
    public int Month
    {
        get;
        set;
    }
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

    public MonthCDF(List<Point> points)
    {
        Month = points[0].Date.Month;

        var values = new List<double> { };
        foreach (Point pt in points)
        {
            values.Add(pt.Value);
        }

        List<double> sorted_values;
        List<double> cdf = Utils.ComputeCDF(values, out sorted_values);

        Probability = cdf;
        Flow = sorted_values;
    }

} //namespace
} //class
