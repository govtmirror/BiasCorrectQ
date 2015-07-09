[![Build status](https://ci.appveyor.com/api/projects/status/g0gdqfjjmfgnrskj?svg=true)](https://ci.appveyor.com/project/blounsbury36183/biascorrectq)


## BiasCorrectQ [BETA]
### Streamflow bias correction
This code can be used to adjust daily simulated streamflow to be consistent with monthly to annual aspects of observed streamflow.

The methods follow those outlined in Snover et al. (2003):
http://www.hydro.washington.edu/Lettenmaier/permanent_archive/hamleaf/bams_paper/technical_documentation.pdf

**NOTE: This is BETA code! It works to bias correct simulated streamflow to observed streamflow with inputs at a monthly timestep. Work is in progress to extend the methods to adjust daily simulated streamflow. A fully functional program is expected to be complete by September 2015.**

### Running the program
This is a Console Application, it is run from a Windows Command Prompt (cmd.exe), like so:

<pre>C:\>BiasCorrectQ.exe  observedFile  baselineFile  futureFile  outFile  informat  outformat</pre>

Where:
* observedFile - observed monthly streamflow
* baselineFile - simulated historical monthly streamflow
* futureFile - simulated future monthly streamflow
* outFile - file name for program output of bias corrected monthly streamflow
* informat/outformat - either "vic" or "csv", text file format is from [VIC](http://www.hydro.washington.edu/Lettenmaier/Models/VIC/index.shtml) (vic) or comma-seperated (csv),

NOTE: If running the baseline bias correction enter "baselineFile" as the "futureFile".

### TO-DO
* ~~Extend the methods to bias correct future streamflow at equal monthly timesteps and periods of record.~~
* ~~Extend the methods to handle different periods of record at a monthly timestep.~~
* Extend the methods to adjust daily simulated streamflow.
* Modify code to allow input arguments to be files or folders. If folders are specified, the code would then process all files in the folders. This will require the user to have some consistent naming convention between files in the folders. Also modify the code to specify an output directory.
