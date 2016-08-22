using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiasCorrectQ
{
static class Utils
{
    internal static List<double> ComputeCDF(List<double> values,
                                            out List<double> sorted_values)
    {
        var rval = new List<double> { };

        //copy values and sort
        var vals = new List<double>(values);
        vals.Sort();
        vals.Reverse();
        sorted_values = vals;

        int count = 1;
        foreach (var val in vals)
        {
            /* Statistical Downscaling Techniques for Global Climate Model
             * Simulations of Temperature and Precipitation with Application to
             * Water Resources Planning Studies
             *     Alan F. Hamlet
             *     Eric P. Salathé
             *     Pablo Carrasco
             *
             * unbiased quantile estimator is used to assign a plotting
             * position to the data based on the Cunnane formulation
             * (Stedinger et al. 1993):15€
             *     q = (i − 0.4) / (n + 0.2)
             * where q is the estimated quantile (or probability of exceedance)
             * for the data value of rank position i (rank position 1 is the highest value),
             * for a total sample size of n */
            rval.Add((count - 0.4) / (vals.Count + 0.2));
            count++;
        }

        return rval;
    }

    internal static List<double> GetWYAnnualAverages(List<Point> flow)
    {
        var annualData = GetWYAnnualData(flow);

        // get average for each year
        var values = new List<double> { };
        foreach (var item in annualData)
        {
            int daysInYear = (DateTime.IsLeapYear(item.Key)) ? 366 : 365;
            values.Add(item.Value.Sum() / daysInYear);
        }

        return values;
    }

    private static Dictionary<int, List<double>> GetWYAnnualData(List<Point> flow)
    {
        int startWY = flow[0].Date.Year + 1;
        int endWY = flow[flow.Count - 1].Date.Year;

        var rval = new Dictionary<int, List<double>> { };
        for (int i = 0; i < (endWY - startWY + 1); i++)
        {
            rval.Add(startWY + i, new List<double> { });
        }

        // add data for the water year
        foreach (Point pt in flow)
        {
            int month = pt.Date.Month;
            int year = pt.Date.Year;

            double value = pt.Value * DateTime.DaysInMonth(year, month);

            if (month > 9)
            {
                rval[pt.Date.Year + 1].Add(value);
            }
            else
            {
                rval[pt.Date.Year].Add(value);
            }
        }
        return rval;
    }

    internal static List<Point> GetMeanSummaryHydrograph(List<Point> flow)
    {
        var rval = new List<Point> { };

        //initial new list to hold monthly values, index = month -1
        var monthData = new List<List<double>> { };
        for (int i = 0; i < 12; i++)
        {
            monthData.Add(new List<double> { });
        }

        //add monthly data
        foreach (var pt in flow)
        {
            monthData[pt.Date.Month - 1].Add(pt.Value);
        }

        //fill in rval with mean of month values, arbitrary dates of 10/1/1999 - 9/1/2000
        int[] wy_months = { 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        foreach (var month in wy_months)
        {
            int year = (month > 9) ? 1999 : 2000;
            rval.Add(new Point(new DateTime(year, month, 1),
                               monthData[month - 1].Average()));
        }

        return rval;
    }

    internal static void TruncateToWYs(List<Point> data)
    {
        //truncate beginning of data until October is found
        foreach (var pt in data.ToList())
        {
            if (pt.Date.Month != 10)
            {
                data.Remove(pt);
            }
            else
            {
                break;
            }
        }

        //truncate end of data until September is found
        for (int i = data.Count - 1; i >= 0; i--)
        {
            var pt = data[i];
            if (pt.Date.Month != 9)
            {
                data.Remove(pt);
            }
            else
            {
                break;
            }
        }
    }

    internal static void AlignPeriods(List<Point> obs, List<Point> data)
    {
        //align beginning of data so start dates match
        if (obs.First().Date < data.First().Date)
        {
            //truncate obs to data
            foreach (var pt in obs.ToList())
            {
                if (pt.Date < data.First().Date)
                {
                    obs.Remove(pt);
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            //truncate data to obs
            foreach (var pt in data.ToList())
            {
                if (pt.Date < obs.First().Date)
                {
                    data.Remove(pt);
                }
                else
                {
                    break;
                }
            }
        }
        

        //align end of data so end dates match
        if (obs.Last().Date > data.Last().Date)
        {
            //truncate obs to data
            for (int i = obs.Count - 1; i >= 0; i--)
            {
                var pt = obs[i];
                if (pt.Date > data.Last().Date)
                {
                    obs.Remove(pt);
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            //truncate data to obs
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var pt = data[i];
                if (pt.Date > obs.Last().Date)
                {
                    data.Remove(pt);
                }
                else
                {
                    break;
                }
            }
        }
    }

    internal static int ValueIndex(double value, List<double> list)
    {
        bool listAscending = (list.Last() > list.First());

        for (int i = 0; i < list.Count; i++)
        {
            bool found = listAscending ? list[i] >= value : value >= list[i];

            if (found)
            {
                return i;
            }
        }
        return -1;
    }

    internal static double Interpolate(double x, double x1, double x2,
                                       double y1, double y2)
    {
        if ((x2 - x1) == 0)
        {
            return (y1 + y2) / 2;
        }
        return y1 + (x - x1) * (y2 - y1) / (x2 - x1);
    }

    internal static bool IsDataMonthly(List<Point> data)
    {
        /* pretty primitive check, if someone has a better method feel free
         * to modify this */
        var d1 = data[0].Date;
        var d2 = data[1].Date;
        if (d2 == d1.AddMonths(1))
        {
            return true;
        }
        return false;
    }

    internal static bool IsDataDaily(List<Point> data)
    {
        /* pretty primitive check, if someone has a better method feel free
         * to modify this */
        var d1 = data[0].Date;
        var d2 = data[1].Date;
        if (d2 == d1.AddDays(1))
        {
            return true;
        }
        return false;
    }

} //class
} //namespace
