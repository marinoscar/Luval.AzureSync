using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Luval.AzureSync;

namespace Luval.AzureSyncConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var consoleArgs = new ConsoleArguments(args);
            if (consoleArgs.ContainsSwitch("-h") || consoleArgs.ContainsSwitch("-?") || args.Length <= 0)
            {
                PrintHelp();
                return;
            }
            var p = new Dictionary<string, string>();
            p["key"] = consoleArgs.GetSwitchValue("-k");
            p["account"] = consoleArgs.GetSwitchValue("-a");
            p["share"] = consoleArgs.GetSwitchValue("-s");
            p["dir"] = consoleArgs.GetSwitchValue("-d");
            if (p.Values.Any(string.IsNullOrWhiteSpace))
            {
                PrintHelp();
                return;
            }
            var logger = new ConsoleLogger();
            var keys = new KeyReader(p["key"]);
            var dir = new DirectoryInfo(p["dir"]);
            if (!dir.Exists) throw new ArgumentException(string.Format("Invalid directory {0}", p["dir"]));
            var sw = Stopwatch.StartNew();
            var dirSync = new DirectorySync(keys.GetByAccountName(p["account"]), p["share"], dir, logger)
                {
                    RunAsync = consoleArgs.ContainsSwitch("-async")
                };
            if (consoleArgs.ContainsSwitch("-deleteAll"))
                dirSync.DeleteAll();
            else if (consoleArgs.ContainsSwitch("-force")) dirSync.CleanAndDownload();
            else dirSync.Sync();
            sw.Stop();
            logger.WriteLine("");
            logger.WriteLine("FINISH THE SYNC IN {0}", sw.Elapsed);
        }

        static void PrintHelp()
        {
            Console.Write(@"
    Luval Azure Sync
    Application to sync a directory with the Azure File Storage Service

    -k           Path to the file where the keys are stored
    -a           Name of the account in the key files to use
    -s           Name of the cloud share to use in Azure
    -d           Path of the directory to sync in Azure
    -async       Indicates that the files will be uploaded in multiple threads
    -deleteCloud Delete all the content from the cloud files
    -force       Cleans and force the download
");
        }
    }
}
