using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using BiasCorrectQ;

namespace BiasCorrectQ.Tests
{

[TestFixture]
class TestBaselineBiasCorrection
{
    [Test]
    public void BaselineBiasCorrection()
    {
        string projDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        var knownMonthlyMeans = new List<Point> { };
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 10, 1), 822.1));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 11, 1), 975.4));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 12, 1), 1037.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 1, 1), 1179.4));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 2, 1), 1535.6));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 3, 1), 2837.8));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 4, 1), 5175.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 5, 1), 7848.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 6, 1), 6066.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 7, 1), 2175.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 8, 1), 962.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 9, 1), 832.3));

        //get input data
        string observedFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Observations.txt");
        string baselineFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Baseline.month");
        List<Point> observed = BiasCorrectQ.Program.GetInputData(observedFile, BiasCorrectQ.Program.TextFormat.vic);
        List<Point> baseline = BiasCorrectQ.Program.GetInputData(baselineFile, BiasCorrectQ.Program.TextFormat.vic);

        //do bias correction
        List<Point> sim_biased = BiasCorrectQ.Program.DoHDBiasCorrection(observed, baseline, baseline);

        //get monthly means and check against accepted correct results
        List<Point> monthlyMeans = Utils.GetMeanSummaryHydrograph(sim_biased);
        for (int i = 0; i < knownMonthlyMeans.Count; i++)
        {
            Assert.AreEqual(knownMonthlyMeans[i].Value, Math.Round(monthlyMeans[i].Value, 1));
        }
    }

} //namespace
} //class
