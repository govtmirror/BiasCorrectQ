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
        if (args.Length != 6)
        {
            PrintUsage();
            return;
        }

        string observed = args[0];
        string baseline = args[1];
        string future = args[2];
        string outfile = args[3];
        string informat = args[4];
        string outformat = args[5];

        //check input files exist
        var inFiles = new List<string> { observed, baseline, future };
        foreach (var str in inFiles)
        {
            if (!File.Exists(str))
            {
                Console.WriteLine("error: file not found - " + str);
                return;
            }
        }

        //check outfile directory exists
        string outDir = Path.GetDirectoryName(Path.GetFullPath(outfile));
        if (!Directory.Exists(outDir))
        {
            Console.WriteLine("error: outFile directory not found - " + outDir);
            return;
        }

        //check informat/outformat properly specified
        if ((informat != "csv" && informat != "vic") ||
                (outformat != "csv" && outformat != "vic"))
        {
            PrintUsage();
            return;
        }

        //parse informat/outformat to TextFormat enum type
        TextFormat infmt = (TextFormat)Enum.Parse(typeof(TextFormat), informat);
        TextFormat outfmt = (TextFormat)Enum.Parse(typeof(TextFormat), outformat);

        //do bias correction
        List<Point> biasedFinal = DoBiasCorrection(observed, baseline, future, infmt);

        //write output
        WriteFile(biasedFinal, outfile, outfmt);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:  BiasCorrectQ.exe  observedFile  baselineFile  futureFile  outFile  informat  outformat");
        Console.WriteLine("Where:");
        Console.WriteLine("    observedFile - observed monthly streamflow");
        Console.WriteLine("    baselineFile - simulated historical monthly streamflow");
        Console.WriteLine("    futureFile - simulated future monthly streamflow");
        Console.WriteLine("    outFile - file name for program output of bias corrected monthly streamflow");
        Console.WriteLine("    informat/outformat - either \"vic\" or \"csv\"");
        Console.WriteLine();
        Console.WriteLine("NOTE: If running the baseline bias correction enter \"baselineFile\" as the \"futureFile\"");
    }

    internal static List<Point> DoBiasCorrection(string observedFile,
            string baselineFile, string futureFile, TextFormat infmt)
    {
        //get input data
        List<Point> observed = GetInputData(observedFile, infmt);
        List<Point> baseline = GetInputData(baselineFile, infmt);
        List<Point> future = GetInputData(futureFile, infmt);
        if (observed.Count == 0 || baseline.Count == 0 || future.Count == 0)
        {
            Console.WriteLine("error parsing input files");
            return new List<Point> { };
        }

        //get monthly data
        List<Point> observedMonthly = DataToMonthly(observed);
        List<Point> baselineMonthly = DataToMonthly(baseline);
        List<Point> futureMonthly = DataToMonthly(future);
        if (observedMonthly.Count == 0 || baselineMonthly.Count == 0
                || futureMonthly.Count == 0)
        {
            Console.WriteLine("error parsing input file(s) to monthly");
            return new List<Point> { };
        }

        //truncate inputs to water year data
        Utils.TruncateToWYs(observed);
        Utils.TruncateToWYs(baseline);
        Utils.TruncateToWYs(future);
        Utils.TruncateToWYs(observedMonthly);
        Utils.TruncateToWYs(baselineMonthly);
        Utils.TruncateToWYs(futureMonthly);

        //do monthly bias correction
        List<Point> biasedMonthly = DoMonthlyBiasCorrection(observedMonthly,
                                    baselineMonthly, futureMonthly);
        List<Point> biasedFinal = DoAnnualBiasCorrection(observedMonthly,
                                  baselineMonthly, futureMonthly, biasedMonthly);

        //do daily adjustments
        if (IsDataDaily(future))
        {

        }

        //check bias correction was successful
        if (biasedFinal.Count == 0)
        {
            Console.WriteLine("error computing bias corrected flow for:");
            Console.WriteLine("  observed: " + observedFile);
            Console.WriteLine("  baseline: " + baselineFile);
            Console.WriteLine("  future: " + futureFile);
            return new List<Point> { };
        }

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
            int year = (pt.Date.Month > 9) ? pt.Date.Year + 1 : pt.Date.Year;
            double val = pt.Value * annualFactors[year];
            rval.Add(new Point(pt.Date, val));
        }

        return rval;
    }

    private static List<double> AnnualBiasCorrection(List<Point> obs,
            List<Point> sim, List<Point> fut)
    {
        List<double> fut_avgs = Utils.GetWYAnnualAverages(fut);

        AnnualCDF obs_dist = new AnnualCDF(obs);
        AnnualCDF sim_dist = new AnnualCDF(sim);

        var rval = new List<double> { };
        foreach (var item in fut_avgs)
        {
            double value = GetBiasCorrectedFlow(item,
                                                obs_dist.Flow,
                                                obs_dist.Probability,
                                                obs_dist.FittedStats,
                                                sim_dist.Flow,
                                                sim_dist.Probability,
                                                sim_dist.FittedStats);

            rval.Add(value);
        }
        return rval;
    }

    private static Dictionary<int, double> GetAnnualFactors(
        List<double> biasedAnnual,
        List<Point> biasedMonthly, int startYear)
    {
        List<double> biasedMonthlyAnnual = Utils.GetWYAnnualAverages(biasedMonthly);

        var rval = new Dictionary<int, double> { };
        for (int i = 0; i < biasedAnnual.Count; i++)
        {
            rval.Add(startYear + i, biasedAnnual[i] / biasedMonthlyAnnual[i]);
        }

        return rval;
    }

    private static List<Point> DoMonthlyBiasCorrection(List<Point> obs,
            List<Point> sim, List<Point> fut)
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
            double value = GetBiasCorrectedFlow(pt.Value,
                                                obs_cdf.Flow,
                                                obs_cdf.Probability,
                                                obs_cdf.FittedStats,
                                                sim_cdf.Flow,
                                                sim_cdf.Probability,
                                                sim_cdf.FittedStats);

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
        bool outRangeFlow = (value > sim_flow[0]
                             || value < sim_flow[sim_flow.Count - 1]);

        if (!outRangeFlow)
        {
            quantile = Interpolate(value, sim_flow, sim_exc);
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
                                      List<double> interpList)
    {
        int idx = ValueIndex(value, valuesList);

        // out of bounds, interpolation unknown
        if (idx < 0)
        {
            return idx;
        }

        // no interpolation needed, first value in list
        if (idx == 0)
        {
            return interpList.First();
        }

        double x = value;
        double x1 = valuesList[idx - 1];
        double x2 = valuesList[idx];

        double y1 = interpList[idx - 1];
        double y2 = interpList[idx];

        return Interpolate(x, x1, x2, y1, y2);
    }

    private static int ValueIndex(double value, List<double> list)
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

    private static double Interpolate(double x, double x1, double x2, double y1,
                                      double y2)
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
            return GetVicData(file);
        }
        return new List<Point> { };
    }

    private static List<Point> GetVicData(string filename)
    {
        var rval = new List<Point> { };

        string[] lines = File.ReadAllLines(filename);
        for (int i = 0; i < lines.Length; i++)
        {
            string[] line = lines[i].Split(new char[0],
                                           StringSplitOptions.RemoveEmptyEntries);

            DateTime dt = default(DateTime);
            string value = string.Empty;

            if (line.Length == 3)
            {
                dt = new DateTime(Convert.ToInt32(line[0]), Convert.ToInt32(line[1]), 1);
                value = line[2];
            }
            else if (line.Length == 4)
            {
                dt = new DateTime(Convert.ToInt32(line[0]), Convert.ToInt32(line[1]),
                                  Convert.ToInt32(line[2]));
                value = line[3];
            }
            else
            {
                Console.WriteLine("unsupported input file format - " + filename);
                return new List<Point> { };
            }

            double val;
            if (!double.TryParse(value, out val))
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

    private static void WriteFile(List<Point> sim_new, string filename,
                                  TextFormat fmt)
    {
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

    private static bool IsDataMonthly(List<Point> data)
    {
        /*
         * pretty primitive check, if someone has a better method feel free
         * to modify this
         */
        var d1 = data[0].Date;
        var d2 = data[1].Date;
        if (d2 == d1.AddMonths(1))
        {
            return true;
        }
        return false;
    }

    private static bool IsDataDaily(List<Point> data)
    {
        /*
         * pretty primitive check, if someone has a better method feel free
         * to modify this
         */
        var d1 = data[0].Date;
        var d2 = data[1].Date;
        if (d2 == d1.AddDays(1))
        {
            return true;
        }
        return false;
    }

    private static List<Point> DataToMonthly(List<Point> data)
    {
        /*
         * if data is monthly return data, if data is daily process
         * data, otherwise print error message and get out of here
         */
        if (IsDataMonthly(data))
        {
            return data;
        }
        else if (IsDataDaily(data))
        {
            return DailyToMonthly(data);
        }
        else
        {
            Console.WriteLine("error: only monthly or daily inputs supported");
            return new List<Point> { };
        }
    }

    private static List<Point> DailyToMonthly(List<Point> data)
    {
        var rval = new List<Point> { };

        int startYear = data.First().Date.Year;
        int startMonth = data.First().Date.Month;
        int endYear = data.Last().Date.Year;
        int endMonth = data.Last().Date.Month;

        var startDate = new DateTime(startYear, startMonth, 1);
        var endDate = new DateTime(endYear, endMonth, 1);

        int currIdx = 0;
        var currDate = startDate;
        while (currDate <= endDate)
        {
            var values = new List<double> { };
            for (int i = currIdx; i < data.Count; i++)
            {
                var pt = data[i];

                if (pt.Date.Month == currDate.Month)
                {
                    values.Add(pt.Value);
                    currIdx++;
                }
                else
                {
                    break;
                }
            }

            rval.Add(new Point(currDate, values.Average()));

            currDate = currDate.AddMonths(1);
        }

        return rval;
    }

} //class
} //namespace
