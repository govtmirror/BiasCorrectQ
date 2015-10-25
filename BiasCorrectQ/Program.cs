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

        var inFilesOrFolders = new List<string> { observed, baseline, future };

        //check input files exist
        if (File.Exists(inFilesOrFolders[0]))
        {
            foreach (var file in inFilesOrFolders)
            {
                if (!File.Exists(file))
                {
                    Console.WriteLine("error: file not found - " + file);
                    return;
                }
            }
        }

        //check input folders exist
        bool processDirectories = false;
        string dir = Path.GetFullPath(inFilesOrFolders[0]);
        if (Directory.Exists(dir))
        {
            foreach (var str in inFilesOrFolders)
            {
                dir = Path.GetFullPath(str);
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine("error: directory not found - " + dir);
                    return;
                }
            }
            processDirectories = true;
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
        if (processDirectories)
        {
            BiasCorrectFolders(observed, baseline, future, outfile, infmt, outfmt);
        }
        else
        {
            BiasCorrectFile(observed, baseline, future, outfile, infmt, outfmt);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:  BiasCorrectQ.exe  observed  baseline  future  output  informat  outformat");
        Console.WriteLine("Where:");
        Console.WriteLine("    observed - observed streamflow by file or folder (daily or monthly timestep)");
        Console.WriteLine("    baseline - simulated historical streamflow by file or folder (daily or monthly timestep)");
        Console.WriteLine("    future - simulated future streamflow by file or folder (daily or monthly timestep)");
        Console.WriteLine("    output - file name or folder for program output of bias corrected streamflow");
        Console.WriteLine("    informat/outformat - either \"vic\" or \"csv\" text file format is routed streamflow from VIC (vic) or comma-separated (csv)");
        Console.WriteLine();
        Console.WriteLine("NOTE: If running the baseline bias correction enter \"baseline\" as the \"future\"");
    }

    private static void BiasCorrectFile(string observedFile, string baselineFile,
                                        string futureFile, string outFile,
                                        TextFormat infmt, TextFormat outfmt)
    {
        Console.WriteLine("bias correcting... " + futureFile);

        var bc = DoBiasCorrection(observedFile, baselineFile, futureFile, infmt);

        if (bc.Count == 0)
        {
            Console.WriteLine("error computing bias corrected flow for:");
            Console.WriteLine("  observed: " + observedFile);
            Console.WriteLine("  baseline: " + baselineFile);
            Console.WriteLine("  future: " + futureFile);
        }
        else
        {
            //write output
            WriteFile(bc, outFile, outfmt);
        }
    }

    private static void BiasCorrectFolders(string obsDir, string simDir,
                                           string futDir, string outDir,
                                           TextFormat infmt, TextFormat outfmt)
    {
        Console.WriteLine("\nbias correcting files in folder... " + futDir + "\n");

        var fut_files = Directory.GetFiles(futDir).ToList();

        for (int i = 0; i < fut_files.Count; i++)
        {
            var fut_file = fut_files[i];
            var obs_file = GetMatchingFile(fut_file, obsDir);
            var sim_file = GetMatchingFile(fut_file, simDir);
            var out_file = Path.Combine(outDir, Path.GetFileName(fut_file) + ".bc");

            if (!string.IsNullOrEmpty(obs_file) && !string.IsNullOrEmpty(sim_file))
            {
                BiasCorrectFile(obs_file, sim_file, fut_file, out_file, infmt, outfmt);
            }
        }
    }

    private static string GetMatchingFile(string file, string dir)
    {
        var list_files = Directory.GetFiles(dir).ToList();

        //assume files contain name of future file without the extension
        string pattern = Path.GetFileNameWithoutExtension(file);

        //search for pattern in list_files
        var files = list_files.Where(s => s.Contains(pattern)).ToList();
        if (files.Count != 1)
        {
            if (files.Count == 0)
            {
                Console.WriteLine(
                    string.Format("error: no matching file for pattern ({0}) in directory - {1}",
                                  pattern, dir));
            }
            else
            {
                Console.WriteLine(
                    string.Format("error: multiple files match pattern ({0}) in directory - {1}",
                                  pattern, dir));
            }
            return string.Empty;
        }

        return files[0];
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
            Console.WriteLine("error parsing input file(s)");
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
        if (Utils.IsDataDaily(future))
        {
            List<Point> biasedDaily = AdjDailyToMonthly(future, biasedFinal);
            AdjMonthlyBoundary(biasedDaily);
            biasedFinal = AdjDailyToMonthly(biasedDaily, biasedFinal);
        }

        return biasedFinal;
    }

    private static void AdjMonthlyBoundary(List<Point> biasedFinal)
    {
        /*
         * note: loop over (biasedFinal.Count - 1) is intentional to ignore
         * the last value in the list
         */
        for (int i = 0; i < biasedFinal.Count - 1; i++)
        {
            var pt = biasedFinal[i];
            if (pt.Date.Day == DateTime.DaysInMonth(pt.Date.Year, pt.Date.Month))
            {
                var ptNext = biasedFinal[i + 1];

                var val = pt.Value;
                var valNext = ptNext.Value;

                pt.Value = (3 * val + valNext) / 4;
                ptNext.Value = (val + 3 * valNext) / 4;
            }
        }
    }

    private static List<Point> AdjDailyToMonthly(List<Point> future,
            List<Point> biasedMonthly)
    {
        var futureMonthly = DataToMonthly(future);

        Dictionary<DateTime, double> monthlyFactors =
            GetMonthlyFactors(biasedMonthly, futureMonthly);

        var rval = new List<Point> { };
        foreach (var pt in future)
        {
            DateTime key = new DateTime(pt.Date.Year, pt.Date.Month, 1);
            rval.Add(new Point(pt.Date, pt.Value * monthlyFactors[key]));
        }

        return rval;
    }

    /// <summary>
    /// Dictionary of key=DateTime, value=monthly_factor
    /// where DateTime is first of month
    /// </summary>
    /// <param name="futureMonthly"></param>
    /// <param name="biasedMonthly"></param>
    /// <returns></returns>
    private static Dictionary<DateTime, double> GetMonthlyFactors(
        List<Point> biasedMonthly, List<Point> futureMonthly)
    {
        var rval = new Dictionary<DateTime, double> { };
        for (int i = 0; i < biasedMonthly.Count; i++)
        {
            var bcPt = biasedMonthly[i];
            var futPt = futureMonthly[i];

            DateTime key = new DateTime(bcPt.Date.Year, bcPt.Date.Month, 1);

            rval.Add(key, bcPt.Value / futPt.Value);
        }
        return rval;
    }

    private static List<Point> DoAnnualBiasCorrection(List<Point> obs,
            List<Point> sim, List<Point> fut, List<Point> biasedMonthly)
    {
        List<double> sim_annual = AnnualBiasCorrection(obs, sim, fut);

        Dictionary<int, double> annualFactors =
            GetAnnualFactors(sim_annual, biasedMonthly);

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
                                                obs_dist.LNfit,
                                                sim_dist.Flow,
                                                sim_dist.Probability,
                                                sim_dist.LNfit);

            rval.Add(value);
        }
        return rval;
    }

    /// <summary>
    /// Dictionary of key=wy, value=annual_factor
    /// </summary>
    /// <param name="biasedAnnual"></param>
    /// <param name="biasedMonthly"></param>
    /// <param name="startYear"></param>
    /// <returns></returns>
    private static Dictionary<int, double> GetAnnualFactors(
        List<double> biasedAnnual,
        List<Point> biasedMonthly)
    {
        var rval = new Dictionary<int, double> { };

        List<double> biasedMonthlyAnnual = Utils.GetWYAnnualAverages(biasedMonthly);

        int startYear = biasedMonthly[0].Date.Year + 1;
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
                                                obs_cdf.LNfit,
                                                sim_cdf.Flow,
                                                sim_cdf.Probability,
                                                sim_cdf.LNfit);

            rval.Add(new Point(pt.Date, value));
        }
        return rval;
    }

    private static double GetBiasCorrectedFlow(double value,
            List<double> obs_flow, List<double> obs_exc, LNFit obs_stats,
            List<double> sim_flow, List<double> sim_exc, LNFit sim_stats)
    {
        double rval;

        //if simulated value is zero return zero
        if (value > -0.001 && value < 0.001)
        {
            return 0;
        }

        double quantile = -1;
        double ln3anom = (Math.Log(value) - sim_stats.lnmean) / sim_stats.lnstd;
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
            rval = Math.Exp(obs_stats.lnstd * ln3anom + obs_stats.lnmean);
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
        int idx = Utils.ValueIndex(value, valuesList);

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

        return Utils.Interpolate(x, x1, x2, y1, y2);
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

            if (val < 0)
            {
                Console.WriteLine("error: data contains negative values that" +
                                  " are incompatible with fitting of log normal distribution");
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

            if (val < 0)
            {
                Console.WriteLine("error: data contains negative values that" +
                                  " are incompatible with fitting of log normal distribution");
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
        //make sure directory to filename exists
        string outdir = Path.GetDirectoryName(Path.GetFullPath(filename));
        if (!Directory.Exists(outdir))
        {
            Directory.CreateDirectory(outdir);
        }

        //fill lines with sim new data
        string[] lines = new string[sim_new.Count];
        for (int i = 0; i < sim_new.Count; i++)
        {
            Point pt = sim_new[i];

            if (fmt == TextFormat.vic)
            {
                lines[i] = string.Format("{0} {1} {2:0.000}", pt.Date.Year, pt.Date.Month,
                                         pt.Value);
                if (Utils.IsDataDaily(sim_new))
                {
                    lines[i] = string.Format("{0} {1} {2} {3:0.000}", pt.Date.Year, pt.Date.Month,
                                             pt.Date.Day, pt.Value);
                }

            }
            if (fmt == TextFormat.csv)
            {
                lines[i] = string.Format("{0:M/d/yyyy},{1:0.000}", pt.Date, pt.Value);
            }
        }

        //write file
        File.WriteAllLines(filename, lines);
    }

    private static List<Point> DataToMonthly(List<Point> data)
    {
        /*
         * if data is monthly return data, if data is daily process
         * data, otherwise print error message and get out of here
         */
        if (Utils.IsDataMonthly(data))
        {
            return data;
        }
        else if (Utils.IsDataDaily(data))
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
