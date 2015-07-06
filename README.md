## BiasCorrectQ [BETA]
### Streamflow bias correction
This code can be used to adjust daily simulated streamflows to be consistent with monthly to annual aspects of observed streamflows.

The methods follow those outlined in Snover et al. (2003):
http://www.hydro.washington.edu/Lettenmaier/permanent_archive/hamleaf/bams_paper/technical_documentation.pdf

NOTE: This is BETA code! It works to bias correct simulated streamflows to observed streamflow given data at equal timesteps (Monthly ONLY) and equal periods of record. Work is in progress to extend the methods adjust daily simulated streamflows.

## Running the program
This is a Console Application, it is run from a Windows Command Prompt (cmd.exe), like so:

<pre>C:\>BiasCorrectQ.exe  observedFile  baselineFile  futureFile  informat  outformat</pre>

Where:
* observedFile - observed monthly streamflow
* baselineFile - simulated historical monthly streamflow
* futureFile - simulated future monthly streamflow
* informat/outformat - either "vic" or "csv", text file format is from [VIC](http://www.hydro.washington.edu/Lettenmaier/Models/VIC/index.shtml) (vic) or comma-seperated (csv),

NOTE: If running the baseline bias correction enter "baselineFile" as the "futureFile".

### TO-DO
* ~~Extend the methods to bias correct future streamflow at equal monthly timesteps and periods of record.~~
* Extend the methods to handle different periods of record at a monthly timestep.
* Extend the methods to adjust daily simulated streamflows.
