using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiasCorrectQ
{
static class Utils
{
    public static List<double> ComputeCDF(List<double> values, out List<double> sorted_values)
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
             * for a total sample size of n
             */
            rval.Add((count - 0.4) / (vals.Count + 0.2));
            count++;
        }

        return rval;
    }

    public static List<double> GetAnnualAverages(List<Point> flow)
    {
        var annualData = GetAnnualData(flow);

        // get average for each year
        var values = new List<double> { };
        foreach (var item in annualData)
        {
            values.Add(item.Average());
        }

        return values;
    }

    private static List<List<double>> GetAnnualData(List<Point> flow)
    {
        int startYear = flow[0].Date.Year;
        int endYear = flow[flow.Count - 1].Date.Year;

        var annualData = new List<List<double>> { };
        for (int i = 0; i < (endYear - startYear + 1); i++)
        {
            annualData.Add(new List<double> { });
        }

        // add all data for the year
        foreach (Point pt in flow)
        {
            annualData[pt.Date.Year - startYear].Add(pt.Value);
        }
        return annualData;
    }


    internal static List<double> GetAnnualVolumes(List<Point> flow)
    {
        var annualData = GetAnnualData(flow);

        // get sum for each year
        var values = new List<double> { };
        foreach (var item in annualData)
        {
            double sum = 0;
            foreach (var val in item)
            {
                sum += val;
            }
            values.Add(sum);
        }

        return values;
    }
} //namespace
} //class
