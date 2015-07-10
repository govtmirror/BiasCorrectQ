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
    public void BaselineMonthlyBiasCorrection()
    {
        string projDir = Directory.GetParent(
                             Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        var knownMonthlyMeans = new List<Point> { };
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 10, 1), 821.5));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 11, 1), 975.5));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 12, 1), 1036.5));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 1, 1), 1178.4));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 2, 1), 1529.5));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 3, 1), 2835.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 4, 1), 5172.6));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 5, 1), 7854.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 6, 1), 6067.0));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 7, 1), 2178.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 8, 1), 962.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 9, 1), 832.6));

        //get input data
        string observedFile = Path.Combine(projDir,
                                           @"Tests\TestData\BOISE_Observations.txt");
        string baselineMonth = Path.Combine(projDir,
                                            @"Tests\TestData\BOISE_Baseline.month");

        //do bias correction
        List<Point> bc_monthlyInputs = BiasCorrectQ.Program.DoBiasCorrection(
                                           observedFile, baselineMonth, baselineMonth,
                                           BiasCorrectQ.Program.TextFormat.vic);

        //get monthly means and check against accepted correct results
        List<Point> bcm_monthlyMeans = Utils.GetMeanSummaryHydrograph(bc_monthlyInputs);
        for (int i = 0; i < knownMonthlyMeans.Count; i++)
        {
            Assert.AreEqual(knownMonthlyMeans[i].Value,
                            Math.Round(bcm_monthlyMeans[i].Value, 1));
        }
    }

    [Test]
    public void BaselineDailyBiasCorrection()
    {
        string projDir = Directory.GetParent(
                             Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        var knownMonthlyMeans = new List<Point> { };
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 10, 1), 821.5));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 11, 1), 975.5));
        knownMonthlyMeans.Add(new Point(new DateTime(1999, 12, 1), 1036.5));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 1, 1), 1178.4));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 2, 1), 1528.3));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 3, 1), 2835.2));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 4, 1), 5172.6));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 5, 1), 7854.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 6, 1), 6067.0));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 7, 1), 2178.7));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 8, 1), 962.9));
        knownMonthlyMeans.Add(new Point(new DateTime(2000, 9, 1), 832.6));

        //get input data
        string observedFile = Path.Combine(projDir,
                                           @"Tests\TestData\BOISE_Observations.txt");
        string baselineDay = Path.Combine(projDir,
                                          @"Tests\TestData\BOISE_Baseline.day");

        //do bias correction
        List<Point> bc_dailyInputs = BiasCorrectQ.Program.DoBiasCorrection(observedFile,
                                     baselineDay, baselineDay, BiasCorrectQ.Program.TextFormat.vic);

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
