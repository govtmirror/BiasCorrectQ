[![Build status](https://ci.appveyor.com/api/projects/status/g0gdqfjjmfgnrskj?svg=true)](https://ci.appveyor.com/project/blounsbury36183/biascorrectq)


## BiasCorrectQ
### Streamflow bias correction
This code can be used to adjust daily or monthly simulated streamflow to be consistent with monthly to annual aspects of observed streamflow.

The methods follow those outlined in Snover et al. (2003):
http://www.hydro.washington.edu/Lettenmaier/permanent_archive/hamleaf/bams_paper/technical_documentation.pdf

### Usage
This is a Console Application, it is run from a Windows Command Prompt (cmd.exe), like so:

<pre>C:\>BiasCorrectQ.exe  observed  baseline  future  output  informat  outformat</pre>

Where:
* observed - observed streamflow by file or folder (daily or monthly timestep)
* baseline - simulated historical streamflow by file or folder (daily or monthly timestep)
* future - simulated future streamflow by file or folder (daily or monthly timestep)
* output - file name or folder for program output of bias corrected streamflow
* informat/outformat - either "vic" or "csv", text file format is routed streamflow from [VIC](http://www.hydro.washington.edu/Lettenmaier/Models/VIC/index.shtml) (vic) or comma-separated (csv)

**NOTE:**
* If running the baseline bias correction enter "baseline" as the "future". 
* The inputs *must* be all files or all folders not a combination.
* If folders are specified the program assumes the simulated future file name *without the extension* is part of the observed and baseline file names.
* Given monthly future file(s) the bias corrected streamflow will be monthly, likewise given daily future file(s) the bias corrected streamflow will be daily.
* Streamflow is assumed to be an average flow rate (e.g., ft<sup>3</sup>/s or m<sup>3</sup>/s) NOT a volume (e.g., acre-feet or m<sup>3</sup>).
