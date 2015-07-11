[![Build status](https://ci.appveyor.com/api/projects/status/g0gdqfjjmfgnrskj?svg=true)](https://ci.appveyor.com/project/blounsbury36183/biascorrectq)


## BiasCorrectQ
### Streamflow bias correction
This code can be used to adjust daily simulated streamflow to be consistent with monthly to annual aspects of observed streamflow.

The methods follow those outlined in Snover et al. (2003):
http://www.hydro.washington.edu/Lettenmaier/permanent_archive/hamleaf/bams_paper/technical_documentation.pdf

### Usage
This is a Console Application, it is run from a Windows Command Prompt (cmd.exe), like so:

<pre>C:\>BiasCorrectQ.exe  observed  baseline  future  output  informat  outformat</pre>

Where:
* observedFile - observed daily or monthly file or folder of streamflow
* baselineFile - simulated historical daily or monthly file or folder of streamflow
* futureFile - simulated future daily or monthly file or folder of streamflow
* outFile - file name or folder for program output of bias corrected streamflow
* informat/outformat - either "vic" or "csv", text file format is routed streamflow from [VIC](http://www.hydro.washington.edu/Lettenmaier/Models/VIC/index.shtml) (vic) or comma-seperated (csv)

**NOTE:**
* If running the baseline bias correction enter "baseline" as the "future". 
* The inputs *must* be all files or all folders not a combination.
* If folders are specified the program assumes the simulated future file name *without the extension* is part of the observed and baseline file names.
* Given monthly future file(s) the bias corrected streamflow will be monthly, likewise given daily future file(s) the bias corrected streamflow will be daily.

### TO-DO
* ~~Extend the methods to bias correct future streamflow at equal monthly timesteps and periods of record.~~
* ~~Extend the methods to handle different periods of record at a monthly timestep.~~
* ~~Extend the methods to adjust daily simulated streamflow.~~
* ~~Modify code to allow input arguments to be files or folders. If folders are specified, the code would then process all files in the folders. This will require the user to have some consistent naming convention between files in the folders. Also modify the code to specify an output directory.~~
