using BUNDLE_VERIFIER.Config;
using Common.LoggerManager;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Application.Config
{
    internal static class SetupEnvironment
    {
        private static AppConfig configuration;

        private static string sourceDirectory;
        private static string workingDirectory;

        #region --- APPLICATION ENVIRONMENT ---
        public static AppConfig SetEnvironment()
        {
            ConfigurationLoad();

            // logger manager
            SetLogging();

            // Screen Colors
            SetScreenColors();

            Console.WriteLine($"\r\n==========================================================================================");
            Console.WriteLine($"{Assembly.GetEntryAssembly().GetName().Name} - Version {Assembly.GetEntryAssembly().GetName().Version}");
            Console.WriteLine($"==========================================================================================\r\n");

            // Working Directories
            SetWorkingDirectories();

            return configuration;
        }

        public static string GetSourceDirectory()
            => sourceDirectory;

        public static string GetWorkingDirectory()
            => workingDirectory;

        private static void ConfigurationLoad()
        {
            // Get appsettings.json config.
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build()
                .Get<AppConfig>();
        }

        private static void ParseArguments(string[] args)
        {

        }

        private static void SetLogging()
        {
            try
            {
                //string[] logLevels = GetLoggingLevels(0);
                string[] logLevels = configuration.LoggerManager.Logging.Levels.Split("|");

                if (logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = Directory.GetCurrentDirectory();
                    string filepath = path + "\\logs\\" + logname;

                    int levels = 0;
                    foreach (string item in logLevels)
                    {
                        foreach (LOGLEVELS level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            levels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, levels);

                    Logger.info($"{Assembly.GetEntryAssembly().GetName().Name} ({Assembly.GetEntryAssembly().GetName().Version}) - LOGGING INITIALIZED.");
                }
            }
            catch (Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", e.Message);
            }
        }

        private static void SetScreenColors()
        {
            if (configuration.Application.EnableColors)
            {
                try
                {
                    // Set Foreground color
                    //Console.ForegroundColor = GetColor(configuration.GetSection("Application:Colors").GetValue<string>("ForeGround"));
                    Console.ForegroundColor = GetColor(configuration.Application.Colors.ForeGround);

                    // Set Background color
                    //Console.BackgroundColor = GetColor(configuration.GetSection("Application:Colors").GetValue<string>("BackGround"));
                    Console.BackgroundColor = GetColor(configuration.Application.Colors.BackGround);

                    Console.Clear();
                }
                catch (Exception ex)
                {
                    Logger.error("main: SetScreenColors() - exception={0}", ex.Message);
                }
            }
        }

        private static ConsoleColor GetColor(string color) => color switch
        {
            "BLACK" => ConsoleColor.Black,
            "DARKBLUE" => ConsoleColor.DarkBlue,
            "DARKGREEEN" => ConsoleColor.DarkGreen,
            "DARKCYAN" => ConsoleColor.DarkCyan,
            "DARKRED" => ConsoleColor.DarkRed,
            "DARKMAGENTA" => ConsoleColor.DarkMagenta,
            "DARKYELLOW" => ConsoleColor.DarkYellow,
            "GRAY" => ConsoleColor.Gray,
            "DARKGRAY" => ConsoleColor.DarkGray,
            "BLUE" => ConsoleColor.Blue,
            "GREEN" => ConsoleColor.Green,
            "CYAN" => ConsoleColor.Cyan,
            "RED" => ConsoleColor.Red,
            "MAGENTA" => ConsoleColor.Magenta,
            "YELLOW" => ConsoleColor.Yellow,
            "WHITE" => ConsoleColor.White,
            _ => throw new Exception($"Invalid color identifier '{color}'.")
        };

        static void SetWorkingDirectories()
        {
            string fullName = Assembly.GetEntryAssembly().Location;
            string path = Directory.GetCurrentDirectory();
            
            sourceDirectory = path + "\\in\\";
            if (!Directory.Exists(sourceDirectory)) 
            {
                Directory.CreateDirectory(sourceDirectory);
            }

            workingDirectory = path + "\\temp\\";
            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
            }
        }

        #endregion --- APPLICATION ENVIRONMENT ---
    }
}
