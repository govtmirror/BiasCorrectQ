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
    public void FutureMonthlyBiasCorrection()
    {
        string projDir = Directory.GetParent(
                             Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        var knownMonthlyMeans = new List<Point> { };
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 10, 1), 533.1));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 11, 1), 790.8));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 12, 1), 1100.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 1, 1), 1546.8));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 2, 1), 2628.5));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 3, 1), 5296.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 4, 1), 10345.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 5, 1), 7681.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 6, 1), 2853.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 7, 1), 825.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 8, 1), 484.0));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 9, 1), 485.1));

        //get input data
        string observed = Path.Combine(projDir,
                                       @"Tests\TestData\BOISE_Observations.txt");
        string baselineMonth = Path.Combine(projDir,
                                            @"Tests\TestData\BOISE_Baseline.month");
        string futureMonth = Path.Combine(projDir,
                                          @"Tests\TestData\BOISE_Median2080.month");

        //do bias correction
        List<Point> bc_monthlyInputs = BiasCorrectQ.Program.DoBiasCorrection(observed,
                                       baselineMonth, futureMonth, BiasCorrectQ.Program.TextFormat.vic);

        //get monthly means and check against accepted correct results
        List<Point> bcm_monthlyMeans = Utils.GetMeanSummaryHydrograph(bc_monthlyInputs);
        for (int i = 0; i < knownMonthlyMeans.Count; i++)
        {
            Assert.AreEqual(knownMonthlyMeans[i].Value,
                            Math.Round(bcm_monthlyMeans[i].Value, 1));
        }
    }

    [Test]
    public void FutureDailyBiasCorrection()
    {
        string projDir = Directory.GetParent(
                             Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        var knownMonthlyMeans = new List<Point> { };
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 10, 1), 533.1));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 11, 1), 790.8));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 12, 1), 1100.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 1, 1), 1546.8));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 2, 1), 2623.6));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 3, 1), 5296.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 4, 1), 10345.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 5, 1), 7681.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 6, 1), 2853.1));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 7, 1), 825.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 8, 1), 484.0));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 9, 1), 485.1));

        //get input data
        string observed = Path.Combine(projDir,
                                       @"Tests\TestData\BOISE_Observations.txt");
        string baselineDay = Path.Combine(projDir,
                                          @"Tests\TestData\BOISE_Baseline.day");
        string futureDay = Path.Combine(projDir,
                                        @"Tests\TestData\BOISE_Median2080.day");

        //do bias correction
        List<Point> bc_dailyInputs = BiasCorrectQ.Program.DoBiasCorrection(observed,
                                     baselineDay, futureDay, BiasCorrectQ.Program.TextFormat.vic);

        //get monthly means and check against accepted correct results
        List<Point> bcd_monthlyMeans = Utils.GetMeanSummaryHydrograph(bc_dailyInputs);
        for (int i = 0; i < knownMonthlyMeans.Count; i++)
        {
            Assert.AreEqual(knownMonthlyMeans[i].Value,
                            Math.Round(bcd_monthlyMeans[i].Value, 1));
        }
    }

} //class
} //namespace
