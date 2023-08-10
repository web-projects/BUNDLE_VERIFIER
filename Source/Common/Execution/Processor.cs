using Common.LoggerManager;
using System;
using System.Diagnostics;
using System.IO;

namespace Execution
{
    public static class Processor
    {
        public static void OpenNotePadPlus(string filenamePath)
        {
            try
            {
                if (File.Exists(filenamePath))
                {
                    Process process = new Process();

                    process.StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        FileName = filenamePath
                    };

                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Logger.error($"EXCEPTION in BundleProcessing: [{e.Message}]");
            }
        }
    }
}
