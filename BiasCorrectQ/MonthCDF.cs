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

    public MonthCDF(List<Point> points, int month)
    {
        var values = GetMonthlyData(points, month);

        List<double> sorted_values;
        List<double> cdf = Utils.ComputeCDF(values, out sorted_values);

        Month = month;
        Probability = cdf;
        Flow = sorted_values;
    }

    private static List<double> GetMonthlyData(List<Point> data, int month)
    {
        var values = new List<double> { };
        foreach (Point pt in data)
        {
            if (pt.Date.Month == month)
            {
                values.Add(pt.Value);
            }
        }
        return values;
    }

} //namespace
} //class
