using BUNDLE_VERIFIER.Config;
using BundleValidator.Config;
using Common.Execution;
using Common.Helpers;
using Common.LoggerManager;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Execution
{
    internal static class BundleProcessing
    {
        private const int filenameSpaceFill = 20;
        private const char filenameSpaceFillChar = ' ';
        private const int TimeDelay = 1000;

        private static ProgressBar DeviceProgressBar = null;
        private static int cursorPositionLeft = 0;
        private static int cursorPositionTop = 0;

        public static void ProcessBundles(BundleSchema bundleSchema)
        {
            Console.WriteLine($"SOURCE: {bundleSchema.BundleSource}\n");
            Logger.info($"SOURCE: {bundleSchema.BundleSource}");

            string sourceFilenamePath = Path.Combine(bundleSchema.SourceDirectory, bundleSchema.BundleSource);

            // Ensure Source exists
            if (!File.Exists(sourceFilenamePath)) 
            {
                Console.WriteLine($"SOURCE: {sourceFilenamePath} - NOT FOUND\n");
                Logger.info($"SOURCE: {sourceFilenamePath} - NOT FOUND");
                return;
            }

            // display progress bar
            StartProgressBar();

            ExtractTGZ(sourceFilenamePath, bundleSchema.WorkingDirectory);

            StopProgressBar();

            foreach (Packages package in bundleSchema.Packages)
            {
                bool fileFound = File.Exists(Path.Combine(bundleSchema.WorkingDirectory, package.Name));
                if (!fileFound)
                {
                    Console.WriteLine($"BUNDLE: {Utils.FormatStringAsRequired(package.Name)} - NOT FOUND");
                    Logger.error($"BUNDLE: {Utils.FormatStringAsRequired(package.Name)} - NOT FOUND");
                    continue;
                }

                Console.WriteLine($"BUNDLE: {Utils.FormatStringAsRequired(package.Name)} - FOUND");
                Logger.info($"BUNDLE: {Utils.FormatStringAsRequired(package.Name)} - FOUND");

                // Process child bundle: bundle with 'Name' is the target
                if (package.ChildrenPackages?.Count > 0)
                {
                    string bundleName = package.Name;
                    string childBundlePath = string.Empty;
                    string workingDirectory = bundleSchema.WorkingDirectory;
                    string targetArchiveFullPath = string.Empty;
                    string targetArchiveDestinationFolder = Path.Combine(workingDirectory, package.Name.Replace(".tgz", ".dir"));

                    foreach (Packages child in package.ChildrenPackages)
                    {
                        if (string.IsNullOrEmpty(childBundlePath))
                        {
                            childBundlePath = Path.Combine(workingDirectory, child.Name.Replace(".tgz", ".dir"));
                        }
                        else
                        {
                            bundleName = child.Name;
                            childBundlePath = Path.Combine(Path.Combine(childBundlePath, child.Name.Replace(".tgz", ".dir")));
                            workingDirectory = targetArchiveDestinationFolder;
                            targetArchiveDestinationFolder = Path.Combine(workingDirectory, bundleName.Replace(".tgz", ".dir"));
                        }

                        targetArchiveFullPath = Path.Combine(workingDirectory, bundleName);

                        // Check for directory since some steps are only validating a file in a different subdirectory
                        if (!Directory.Exists(targetArchiveDestinationFolder))
                        {
                            ExtractTGZ(targetArchiveFullPath, targetArchiveDestinationFolder);
                        }

                        // Found the target bundle
                        if (!string.IsNullOrEmpty(child.Name) && !string.IsNullOrEmpty(child.AuthoritySource))
                        {
                            bool usingPackageDirectory = false;
                            foreach (string signatureFile in child.SignatureFiles)
                            {
                                if (!string.IsNullOrEmpty(child.PackageDirectory) && !usingPackageDirectory)
                                {
                                    usingPackageDirectory = true;
                                    targetArchiveDestinationFolder = Path.Combine(targetArchiveDestinationFolder, child.PackageDirectory);
                                }
                                string fileToVerify = Path.Combine(targetArchiveDestinationFolder, signatureFile);
                                string authoritySource = Path.Combine(child.AuthoritySource, signatureFile);

                                // Check file exists in REPO
                                if (!File.Exists(authoritySource)) 
                                {
                                    Console.WriteLine($"  FILE: {Utils.FormatStringAsRequired(authoritySource, filenameSpaceFill, filenameSpaceFillChar)} - NOT FOUND");
                                    Logger.info($"  FILE: {Utils.FormatStringAsRequired(authoritySource, filenameSpaceFill, filenameSpaceFillChar)} - NOT FOUND");
                                    continue;
                                }

                                // Ignore for extensions p7s
                                if (signatureFile.ToLower().EndsWith("p7s"))
                                {
                                    Console.WriteLine($"  FILE: {Utils.FormatStringAsRequired(signatureFile, filenameSpaceFill, filenameSpaceFillChar)} - FOUND");
                                    Logger.info($"  FILE: {Utils.FormatStringAsRequired(signatureFile, filenameSpaceFill, filenameSpaceFillChar)} - FOUND");
                                    continue;
                                }

                                bool fileMatch = File.ReadLines(authoritySource).SequenceEqual(File.ReadLines(fileToVerify));
                                if (fileMatch)
                                {
                                    Console.WriteLine($"  FILE: {Utils.FormatStringAsRequired(signatureFile, filenameSpaceFill, filenameSpaceFillChar)} - MATCH");
                                    Logger.info($"  FILE: {Utils.FormatStringAsRequired(signatureFile, filenameSpaceFill, filenameSpaceFillChar)} - MATCH");
                                }
                                else
                                {
                                    Console.WriteLine($"  FILE: {Utils.FormatStringAsRequired(signatureFile, filenameSpaceFill, filenameSpaceFillChar)} - DOES NOT MATCH");
                                    Logger.info($"  FILE: {Utils.FormatStringAsRequired(signatureFile, filenameSpaceFill, filenameSpaceFillChar)} - DOES NOT MATCH");
                                }
                            }
                        }
                    }
                }
            }

            // Clean up working directory
            if (Directory.Exists(bundleSchema.WorkingDirectory))
            {
                Directory.Delete(bundleSchema.WorkingDirectory, true);
            }
        }

        private static void ExtractTGZ(String gzArchiveName, String destFolder)
        {
            try
            {
                Stream inStream = File.OpenRead(gzArchiveName);
                Stream gzipStream = new GZipInputStream(inStream);

                TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                tarArchive.ExtractContents(destFolder);
                tarArchive.Close();

                gzipStream.Close();
                inStream.Close();
            }
            catch (Exception e)
            {
                Logger.error($"EXCEPTION in BundleProcessing: [{e.Message}]");
            }
        }

        private static void StartProgressBar()
        {
            DeviceProgressBar = new ProgressBar();

            cursorPositionLeft = Console.CursorLeft;
            cursorPositionTop = Console.CursorTop;

            // display progress bar
            Task.Run(async () =>
            {
                while (DeviceProgressBar != null)
                {
                    DeviceProgressBar.UpdateBar();
                    await Task.Delay(ProgressBar.TimeDelay);
                }
            });
        }

        private static void StopProgressBar()
        {
            if (DeviceProgressBar != null)
            {
                DeviceProgressBar.Dispose();
                DeviceProgressBar = null;
                Thread.Sleep(TimeDelay);
                Console.SetCursorPosition(cursorPositionLeft, cursorPositionTop);
            }
        }
    }
}
