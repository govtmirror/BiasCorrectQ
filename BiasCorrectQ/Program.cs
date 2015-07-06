using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BiasCorrectQ
{
class Program
{
    internal enum TextFormat
    {
        csv,
        vic
    }

    static void Main(string[] args)
    {
        if (args.Length != 5)
        {
            PrintUsage();
            return;
        }

        string observedFile = args[0];
        string baselineFile = args[1];
        string futureFile = args[2];
        string informat = args[3];
        string outformat = args[4];

        //check input files exist
        var inFiles = new List<string> { observedFile, baselineFile, futureFile };
        foreach (var str in inFiles)
        {
            if (!File.Exists(str))
            {
                Console.WriteLine("error: file not found - " + str);
                return;
            }
        }

        //check input/output format properly specified
        if (informat != "csv" && informat != "vic")
        {
            PrintUsage();
        }
        if (outformat != "csv" && outformat != "vic")
        {
            PrintUsage();
        }

        //parse informat/outformat to TextFormat enum type
        TextFormat infmt = (TextFormat)Enum.Parse(typeof(TextFormat), informat);
        TextFormat outfmt = (TextFormat)Enum.Parse(typeof(TextFormat), outformat);

        //get input data
        List<Point> observed = GetInputData(observedFile, infmt);
        List<Point> baseline = GetInputData(baselineFile, infmt);
        List<Point> future = GetInputData(futureFile, infmt);
        if (observed.Count == 0 || baseline.Count == 0 || future.Count == 0)
        {
            Console.WriteLine("error parsing input files");
            return;
        }

        //do bias correction
        List<Point> sim_biased = DoBiasCorrection(observed, baseline, future);

        //check bias correction was successful
        if (sim_biased.Count == 0)
        {
            Console.WriteLine("error computing bias corrected flow for:");
            Console.WriteLine("  observed: " + observedFile);
            Console.WriteLine("  baseline: " + baselineFile);
            Console.WriteLine("  future: " + futureFile);
            return;
        }

        //write output
        WriteFile(sim_biased, futureFile, outfmt);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:  BiasCorrectQ.exe  observedFile  baselineFile  futureFile  informat  outformat");
        Console.WriteLine("Where:");
        Console.WriteLine("    observedFile - observed monthly streamflow");
        Console.WriteLine("    baselineFile - simulated historical monthly streamflow");
        Console.WriteLine("    futureFile - simulated future monthly streamflow");
        Console.WriteLine("    informat/outformat - either \"vic\" or \"csv\"");
        Console.WriteLine();
        Console.WriteLine("NOTE: If running the baseline bias correction enter \"baselineFile\" as the \"futureFile\"");
    }

    internal static List<Point> DoBiasCorrection(List<Point> observed,
            List<Point> baseline, List<Point> future)
    {
        //truncate inputs to water year data
        Utils.TruncateToWYs(observed);
        Utils.TruncateToWYs(baseline);
        Utils.TruncateToWYs(future);

        List<Point> biasedMonthly = DoMonthlyBiasCorrection(observed, baseline, future);
        List<Point> biasedFinal = DoAnnualBiasCorrection(observed, baseline, future, biasedMonthly);

        return biasedFinal;
    }

    private static List<Point> DoAnnualBiasCorrection(List<Point> obs,
            List<Point> sim, List<Point> fut, List<Point> biasedMonthly)
    {
        List<double> sim_annual = AnnualBiasCorrection(obs, sim, fut);

        Dictionary<int, double> annualFactors =
            GetAnnualFactors(sim_annual, biasedMonthly, obs[0].Date.Year + 1);

        var rval = new List<Point> { };
        foreach (Point pt in biasedMonthly)
        {
            int year = (pt.Date.Month < 10) ? pt.Date.Year : pt.Date.Year + 1;
            double val = pt.Value * annualFactors[year];
            rval.Add(new Point(pt.Date, val));
        }

        return rval;
    }

    private static List<double> AnnualBiasCorrection(List<Point> obs, List<Point> sim, List<Point> fut)
    {
        List<double> fut_avgs = Utils.GetWYAnnualAverages(fut);

        AnnualCDF obs_dist = new AnnualCDF(obs);
        AnnualCDF sim_dist = new AnnualCDF(sim);

        var rval = new List<double> { };
        foreach (var item in fut_avgs)
        {
            double value = GetBiasCorrectedFlow(item, obs_dist.Flow, obs_dist.Probability, obs_dist.FittedStats,
                                                sim_dist.Flow, sim_dist.Probability, sim_dist.FittedStats);

            rval.Add(value);
        }
        return rval;
    }

    private static Dictionary<int, double> GetAnnualFactors(List<double> biasedAnnual,
            List<Point> biasedMonthly, int startYear)
    {
        List<double> biasedMonthlyAnnualVolumes = Utils.GetWYAnnualVolumes(biasedMonthly);

        var rval = new Dictionary<int, double> { };
        for (int i = 0; i < biasedAnnual.Count; i++)
        {
            rval.Add(startYear + i, (biasedAnnual[i] * 12) / biasedMonthlyAnnualVolumes[i]);
        }

        return rval;
    }

    private static List<Point> DoMonthlyBiasCorrection(List<Point> obs, List<Point> sim, List<Point> fut)
    {
        var obs_dist = new List<MonthCDF> { };
        var sim_dist = new List<MonthCDF> { };
        for (int i = 1; i <= 12; i++) //calender year list
        {
            obs_dist.Add(new MonthCDF(obs, i));
            sim_dist.Add(new MonthCDF(sim, i));
        }

        var rval = new List<Point> { };
        foreach (Point pt in fut)
        {
            var obs_cdf = obs_dist[pt.Date.Month - 1];
            var sim_cdf = sim_dist[pt.Date.Month - 1];
            double value = GetBiasCorrectedFlow(pt.Value, obs_cdf.Flow, obs_cdf.Probability, obs_cdf.FittedStats,
                                                sim_cdf.Flow, sim_cdf.Probability, sim_cdf.FittedStats);

            rval.Add(new Point(pt.Date, value));
        }
        return rval;
    }

    private static double GetBiasCorrectedFlow(double value,
            List<double> obs_flow, List<double> obs_exc, FittedStats obs_stats,
            List<double> sim_flow, List<double> sim_exc, FittedStats sim_stats)
    {
        double rval;

        //if simulated value is zero return zero
        if (value > -0.001 && value < 0.001)
        {
            return 0;
        }

        double quantile = -1;
        double ln3anom = (Math.Log(value) - sim_stats.fittedmean) / sim_stats.fittedstd;
        double thresh = 3.5;

        //check if flow higher or lower than any quantile value
        bool outRangeFlow = (value > sim_flow[0] || value < sim_flow[sim_flow.Count - 1]);

        if (!outRangeFlow)
        {
            quantile = Interpolate(value, sim_flow, sim_exc, false);
        }

        //check if quantile is out of range of observed quantile
        bool outRangeQuantile = (quantile > obs_exc[obs_exc.Count - 1]
                                 || quantile < obs_exc[0] || outRangeFlow);

        if (outRangeQuantile)
        {
            rval = Math.Exp(obs_stats.fittedstd * ln3anom + obs_stats.fittedmean);
        }
        else
        {
            rval = Interpolate(quantile, obs_exc, obs_flow);
        }

        //if simulated value is sufficiently out of range as defined by
        //threshold value, use simple scaling technique
        if (ln3anom < (-1 * thresh) || ln3anom > thresh)
        {
            rval = value / sim_stats.mean * obs_stats.mean;
        }

        return rval;
    }

    private static double Interpolate(double value, List<double> valuesList,
                                      List<double> interpList, bool valuesListAscending = true)
    {
        int idx = ValueIndex(value, valuesList, valuesListAscending);

        // out of bounds, interpolation unknown
        if (idx < 0)
        {
            return idx;
        }

        // no interpolation needed, first value in list
        if (idx == 0)
        {
            return interpList[0];
        }

        double x = value;
        double x1 = valuesList[idx - 1];
        double x2 = valuesList[idx];

        double y1 = interpList[idx - 1];
        double y2 = interpList[idx];

        return Interpolate(x, x1, x2, y1, y2);
    }

    private static int ValueIndex(double value, List<double> list, bool listAscending)
    {
        if (listAscending)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] >= value)
                {
                    return i;
                }
            }
        }
        else //descending
        {
            if (value < list[list.Count - 1])
            {
                return -1; //out of bounds, no index
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] >= value)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static double Interpolate(double x, double x1, double x2, double y1, double y2)
    {
        if ((x2 - x1) == 0)
        {
            return (y1 + y2) / 2;
        }
        return y1 + (x - x1) * (y2 - y1) / (x2 - x1);
    }

    internal static List<Point> GetInputData(string file, TextFormat fmt)
    {
        if (fmt == TextFormat.csv)
        {
            return GetCsvData(file);
        }
        if (fmt == TextFormat.vic)
        {
            return GetVicMonthly(file);
        }
        return new List<Point> { };
    }

    private static List<Point> GetVicMonthly(string filename)
    {
        var rval = new List<Point> { };

        string[] lines = File.ReadAllLines(filename);
        for (int i = 0; i < lines.Length; i++)
        {
            string[] line = lines[i].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            DateTime dt = new DateTime(Convert.ToInt32(line[0]), Convert.ToInt32(line[1]), 1);

            double val;
            if (!double.TryParse(line[2], out val))
            {
                Console.WriteLine("error parsing value at row: " + (i + 1));
                return new List<Point> { };
            }

            Point pt = new Point(dt, val);
            rval.Add(pt);
        }

        return rval;
    }

    private static List<Point> GetCsvData(string filename)
    {
        var rval = new List<Point> { };

        string[] lines = File.ReadAllLines(filename);
        for (int i = 0; i < lines.Length; i++)
        {
            string[] line = lines[i].Split(',');

            DateTime dt;
            if (!DateTime.TryParse(line[0], out dt))
            {
                Console.WriteLine("error parsing date at row: " + (i + 1));
                return new List<Point> { };
            }

            double val;
            if (!double.TryParse(line[1], out val))
            {
                Console.WriteLine("error parsing value at row: " + (i + 1));
                return new List<Point> { };
            }

            Point pt = new Point(dt, val);
            rval.Add(pt);
        }

        return rval;
    }

    private static void WriteFile(List<Point> sim_new, string origname, TextFormat fmt)
    {
        string filename = Path.GetFileNameWithoutExtension(origname);
        string ext = Path.GetExtension(origname);
        filename += "_BC" + ext;

        string[] lines = new string[sim_new.Count];
        for (int i = 0; i < sim_new.Count; i++)
        {
            Point pt = sim_new[i];

            if (fmt == TextFormat.vic)
            {
                lines[i] = string.Format("{0} {1} {2}", pt.Date.Year, pt.Date.Month, pt.Value);
            }
            if (fmt == TextFormat.csv)
            {
                lines[i] = string.Format("{0},{1}", pt.Date, pt.Value);
            }
        }
        File.WriteAllLines(filename, lines);
    }

} //class
} //namespace
