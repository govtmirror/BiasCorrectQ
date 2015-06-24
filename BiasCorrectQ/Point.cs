using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiasCorrectQ
{
class Point
{
    public DateTime Date
    {
        get;
        set;
    }
    public double Value
    {
        get;
        set;
    }

    public Point(DateTime dt, double value)
    {
        Date = dt;
        Value = value;
    }
}
}
