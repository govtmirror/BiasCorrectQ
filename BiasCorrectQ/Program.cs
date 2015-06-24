using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BiasCorrectQ
{
class Program
{
    enum OutFormat
    {
        csv,
        vic
    }

    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage:  HybridDeltaQ.exe  observed.month  baseline.month  future.month");
            Console.WriteLine("    If running the baseline only enter \"baseline.month\" twice");
            return;
        }

        string observedFile = args[0];
        string baselineFile = args[1];
        string futureFile = args[2];

        if (!File.Exists(observedFile))
        {
            Console.WriteLine("error: file not found - " + observedFile);
            return;
        }

        if (!File.Exists(baselineFile))
        {
            Console.WriteLine("error: file not found - " + baselineFile);
            return;
        }

        if (!File.Exists(futureFile))
        {
            Console.WriteLine("error: file not found - " + futureFile);
            return;
        }

        List<Point> observed = GetVicMonthly(observedFile);
        List<Point> baseline = GetVicMonthly(baselineFile);
        List<Point> future = GetVicMonthly(futureFile);
        if (observed.Count == 0 || baseline.Count == 0 || future.Count == 0)
        {
            Console.WriteLine("error parsing input files");
            return;
        }

        List<Point> sim_biased = DoHDBiasCorrection(observed, baseline, future);

        if (sim_biased.Count == 0)
        {
            Console.WriteLine("error computing bias corrected flow for:");
            Console.WriteLine("  observed: " + observedFile);
            Console.WriteLine("  simulated: " + baselineFile);
            return;
        }
        WriteFile(sim_biased, futureFile, OutFormat.vic);
    }

    private static List<Point> DoHDBiasCorrection(List<Point> observed,
            List<Point> baseline, List<Point> future)
    {
        List<Point> biasedMonthly = DoMonthlyBiasCorrection(observed, baseline);
        List<Point> biasedAnnual = DoAnnualBiasCorrection(observed, baseline, biasedMonthly);
        //List<Point> biasedFinal = DoHistoricalAdjustment(obs, sim, biasedAnnual);

        return biasedAnnual;
    }

    private static List<Point> DoHistoricalAdjustment(List<Point> obs,
            List<Point> sim, List<Point> sim_biased)
    {
        var rval = new List<Point> { };
        for (int i = 0; i < obs.Count; i++)
        {
            double factor = sim_biased[i].Value - sim[i].Value;
            rval.Add(new Point(obs[i].Date, obs[i].Value + factor));
        }
        return rval;
    }

    private static List<Point> DoAnnualBiasCorrection(List<Point> obs,
            List<Point> sim, List<Point> biasedMonthly)
    {
        List<double> sim_annual = AnnualBiasCorrection(obs, sim);

        Dictionary<int, double> annualFactors =
            GetAnnualFactors(sim_annual, biasedMonthly, obs[0].Date.Year);

        var rval = new List<Point> { };
        foreach (Point pt in biasedMonthly)
        {
            double val = pt.Value * annualFactors[pt.Date.Year];
            rval.Add(new Point(pt.Date, val));
        }

        return rval;
    }

    private static List<double> AnnualBiasCorrection(List<Point> obs, List<Point> sim)
    {
        List<double> sim_avgs = Utils.GetAnnualAverages(sim);

        AnnualCDF obs_dist = new AnnualCDF(obs);
        AnnualCDF sim_dist = new AnnualCDF(sim);

        var rval = new List<double> { };
        foreach (var item in sim_avgs)
        {
            double sim_exc = Interpolate(item, sim_dist.Flow, sim_dist.Probability, false);
            if (sim_exc < 0)
                return new List<double> { };

            double value = Interpolate(sim_exc, obs_dist.Probability, obs_dist.Flow);
            if (value < 0)
                return new List<double> { };

            rval.Add(value);
        }
        return rval;
    }

    private static Dictionary<int, double> GetAnnualFactors(List<double> biasedAnnual,
            List<Point> biasedMonthly, int startYear)
    {
        List<double> biasedMonthlyAnnualVolumes = Utils.GetAnnualVolumes(biasedMonthly);

        var rval = new Dictionary<int, double> { };
        for (int i = 0; i < biasedAnnual.Count; i++)
        {
            rval.Add(startYear + i, (biasedAnnual[i] * 12) / biasedMonthlyAnnualVolumes[i]);
        }

        return rval;
    }

    private static List<Point> DoMonthlyBiasCorrection(List<Point> obs, List<Point> sim)
    {
        List<MonthCDF> obs_dist = GetMonthlyCDFs(obs);
        List<MonthCDF> sim_dist = GetMonthlyCDFs(sim);

        var rval = new List<Point> { };
        foreach (Point pt in sim)
        {
            var sim_cdf = sim_dist[pt.Date.Month - 1];
            double sim_exc = Interpolate(pt.Value, sim_cdf.Flow, sim_cdf.Probability, false);
            if (sim_exc < 0)
                return new List<Point> { };

            var obc_cdf = obs_dist[pt.Date.Month - 1];
            double value = Interpolate(sim_exc, obc_cdf.Probability, obc_cdf.Flow);
            if (value < 0)
                return new List<Point> { };

            rval.Add(new Point(pt.Date, value));
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

    private static List<MonthCDF> GetMonthlyCDFs(List<Point> data)
    {
        List<List<Point>> monthlyData = GetMonthlyData(data);

        var rval = new List<MonthCDF> { };
        for (int i = 0; i < monthlyData.Count; i++)
        {
            int month = i + 1;
            rval.Add(new MonthCDF(monthlyData[i]));
        }

        return rval;
    }

    private static List<List<Point>> GetMonthlyData(List<Point> data)
    {
        var monthData = new List<List<Point>> { };
        for (int i = 0; i < 12; i++)
        {
            monthData.Add(new List<Point> { });
        }

        foreach (Point pt in data)
        {
            monthData[pt.Date.Month - 1].Add(pt);
        }

        return monthData;
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

    private static void WriteFile(List<Point> sim_new, string origname, OutFormat fmt)
    {
        string filename = Path.GetFileNameWithoutExtension(origname);
        if (fmt == OutFormat.csv)
        {
            filename += ".HD.csv";
        }
        if (fmt == OutFormat.vic)
        {
            filename += ".HD.txt";
        }

        string[] lines = new string[sim_new.Count];
        for (int i = 0; i < sim_new.Count; i++)
        {
            Point pt = sim_new[i];

            if (fmt == OutFormat.vic)
            {
                lines[i] = string.Format("{0} {1} {2}", pt.Date.Year, pt.Date.Month, pt.Value);
            }
            if (fmt == OutFormat.csv)
            {
                lines[i] = string.Format("{0},{1}", pt.Date, pt.Value);
            }
        }
        File.WriteAllLines(filename, lines);
    }

} //class
} //namespace
