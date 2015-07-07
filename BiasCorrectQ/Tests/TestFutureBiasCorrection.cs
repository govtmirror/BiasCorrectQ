using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BiasCorrectQ.Tests
{

[TestFixture]
class TestFutureBiasCorrection
{
    [Test]
    public void FutureBiasCorrection()
    {
        string projDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        var knownMonthlyMeans = new List<Point> { };
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 10, 1), 533.7));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 11, 1), 791.6));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 12, 1), 1101.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 1, 1), 1547.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 2, 1), 2611.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 3, 1), 5300.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 4, 1), 10351.0));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 5, 1), 7684.4));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 6, 1), 2853.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 7, 1), 825.8));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 8, 1), 484.4));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 9, 1), 485.6));

        //get input data
        string observedFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Observations.txt");
        string baselineFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Baseline.month");
        string futureFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Median2080.month");
        List<Point> observed = BiasCorrectQ.Program.GetInputData(observedFile, BiasCorrectQ.Program.TextFormat.vic);
        List<Point> baseline = BiasCorrectQ.Program.GetInputData(baselineFile, BiasCorrectQ.Program.TextFormat.vic);
        List<Point> future = BiasCorrectQ.Program.GetInputData(futureFile, BiasCorrectQ.Program.TextFormat.vic);

        //do bias correction
        List<Point> sim_biased = BiasCorrectQ.Program.DoBiasCorrection(observed, baseline, future);

        //get monthly means and check against accepted correct results
        List<Point> monthlyMeans = Utils.GetMeanSummaryHydrograph(sim_biased);
        for (int i = 0; i < knownMonthlyMeans.Count; i++)
        {
            Assert.AreEqual(knownMonthlyMeans[i].Value, Math.Round(monthlyMeans[i].Value, 1));
        }
    }

} //class
} //namespace
