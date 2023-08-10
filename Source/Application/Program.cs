using Application.Config;
using Application.Execution;
using BUNDLE_VERIFIER.Config;
using Common.LoggerManager;
using Execution;
using System;

namespace BUNDLE_VERIFIER
{
    class Program
    {
        private static AppConfig configuration;

        static void Main(string[] args)
        {
            configuration = SetupEnvironment.SetEnvironment();

            // Validate Active Index
            if (configuration.Application.ActiveBundleIndex > configuration.Bundles.Count)
            {
                Console.WriteLine($"INVALID BUNDLE INDEX {configuration.Application.ActiveBundleIndex} FOR BUNDLE COUNT: {configuration.Bundles.Count}");
                Logger.error($"INVALID BUNDLE INDEX {configuration.Application.ActiveBundleIndex} FOR BUNDLE COUNT: {configuration.Bundles.Count}");
            }
            else
            {
                BundleProcessing.ProcessBundles(new BundleSchema()
                {
                    SourceDirectory = SetupEnvironment.GetSourceDirectory(),
                    WorkingDirectory = SetupEnvironment.GetWorkingDirectory(),
                    BundleSource = configuration.Bundles[configuration.Application.ActiveBundleIndex].BundlesSource,
                    Packages = configuration.Bundles[configuration.Application.ActiveBundleIndex].Packages
                });

                // open log file in Notepad++
                Processor.OpenNotePadPlus(SetupEnvironment.GetLogFilenamePath());
            }

#if !DEBUG
            Console.WriteLine("\r\n\r\nPress <ENTER> key to exit...");

            ConsoleKeyInfo keypressed = Console.ReadKey(true);

            while (keypressed.Key != ConsoleKey.Enter)
            {
                keypressed = Console.ReadKey(true);
                System.Threading.Thread.Sleep(100);
            }
#endif

            Console.WriteLine("APPLICATION EXITING ...");
            Console.WriteLine("");
        }
    }
}
