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

        var known = new List<Point> { };
        known.Add(new Point(new DateTime(1980, 1, 1), 1558.60653838043));
        known.Add(new Point(new DateTime(1980, 2, 1), 2739.17117429521));
        known.Add(new Point(new DateTime(1980, 3, 1), 3176.06796880605));
        known.Add(new Point(new DateTime(1980, 4, 1), 9015.36011422707));
        known.Add(new Point(new DateTime(1980, 5, 1), 11203.3915049319));
        known.Add(new Point(new DateTime(1980, 6, 1), 3204.68918343077));
        known.Add(new Point(new DateTime(1980, 7, 1), 1598.75686199482));
        known.Add(new Point(new DateTime(1980, 8, 1), 781.641340244064));
        known.Add(new Point(new DateTime(1980, 9, 1), 964.494621283109));
        known.Add(new Point(new DateTime(1980, 10, 1), 707.951790477888));
        known.Add(new Point(new DateTime(1980, 11, 1), 927.004861281717));
        known.Add(new Point(new DateTime(1980, 12, 1), 1206.36404064692));

        string observedFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Observations.txt");
        List<Point> observed = BiasCorrectQ.Program.GetInputData(observedFile, BiasCorrectQ.Program.TextFormat.vic);

        string baselineFile = Path.Combine(projDir, @"Tests\TestData\BOISE_Baseline.month");
        List<Point> baseline = BiasCorrectQ.Program.GetInputData(baselineFile, BiasCorrectQ.Program.TextFormat.vic);

        List<Point> sim_biased = BiasCorrectQ.Program.DoHDBiasCorrection(observed, baseline, baseline, true);

        for (int i = 0; i < known.Count; i++)
        {
            Assert.AreEqual(Math.Round(known[i].Value, 2), Math.Round(sim_biased[i].Value, 2));
        }
    }

} //namespace
} //class
