using BundleValidator.Config.Bundles;
using Common.Execution;
using Common.Helpers;
using Common.LoggerManager;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        // this indicates that the bundle doesn't need unarchiving and only the authority source changes
        private static string twinBundle = "AUTHORITY_SOURCE_CHANGED";

        public static bool HasError { get; private set; } = false;

        public static void ProcessBundles(BundleSchema bundleSchema, bool deleteWorkingDir)
        {
            Console.WriteLine($"SOURCE: {bundleSchema.BundleSource}\n");
            Logger.info($"SOURCE: {bundleSchema.BundleSource}");

            string sourceFilenamePath = Path.Combine(bundleSchema.SourceDirectory, bundleSchema.BundleSource);

            // Ensure Source exists
            if (!File.Exists(sourceFilenamePath))
            {
                Console.WriteLine($"SOURCE: {sourceFilenamePath} - NOT FOUND\n");
                Logger.info($"SOURCE: {sourceFilenamePath} - NOT FOUND");
                HasError = true;
                return;
            }

            // display progress bar
            StartProgressBar();

            // The base bundle has a tgz file extension, but it's actually a tar file.
            bool extracted = ExtractArchive(sourceFilenamePath, bundleSchema.WorkingDirectory, true);

            StopProgressBar();

            if (!extracted)
            {
                Console.WriteLine("Failed to extract archive!\r\n");
                return;
            }

            foreach (Packages package in bundleSchema.Packages)
            {
                bool fileFound = File.Exists(Path.Combine(bundleSchema.WorkingDirectory, package.Name));
                if (!fileFound)
                {
                    Console.WriteLine($"BUNDLE: {Utils.FormatStringAsRequired(package.Name)} - NOT FOUND");
                    Logger.error($"BUNDLE: {Utils.FormatStringAsRequired(package.Name)} - NOT FOUND");
                    HasError = true;
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
                        bool unarchiveChild = true;

                        if (string.IsNullOrEmpty(childBundlePath))
                        {
                            childBundlePath = Path.Combine(workingDirectory, child.Name.Replace(".tgz", ".dir"));
                        }
                        else
                        {
                            // Check for a bundle that changes authority source
                            if (string.Compare(child.Name, twinBundle, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                unarchiveChild = false;
                            }
                            else
                            {
                                bundleName = child.Name;
                                string targetReplacement = bundleName.Contains("tgz") ? ".tgz" : ".tar";
                                childBundlePath = Path.Combine(Path.Combine(childBundlePath,
                                    child.Name.Replace(targetReplacement, ".dir")));
                                workingDirectory = targetArchiveDestinationFolder;
                                targetArchiveDestinationFolder = Path.Combine(workingDirectory, 
                                    bundleName.Replace(targetReplacement, ".dir"));
                            }
                        }

                        if (unarchiveChild)
                        {
                            targetArchiveFullPath = Path.Combine(workingDirectory, bundleName);

                            // Check for directory since some steps are only validating a file in a different subdirectory
                            if (!Directory.Exists(targetArchiveDestinationFolder))
                            {
                                if (!ExtractArchive(targetArchiveFullPath, targetArchiveDestinationFolder))
                                {
                                    Console.WriteLine("Failed to extract archive!\r\n");
                                    return;
                                }
                            }
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
                                    HasError = true;
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
                                    HasError = true;

                                    List<string> offenderList = File.ReadLines(fileToVerify).Except(File.ReadLines(authoritySource)).ToList();
                                    foreach (string offender in offenderList)
                                    {
                                        string offenderString = string.Format("\"{0}\"", offender);
                                        Console.WriteLine($"    OFFENDER: {Utils.FormatStringAsRequired(offenderString, filenameSpaceFill, filenameSpaceFillChar)}");
                                        Logger.info($"    OFFENDER: {Utils.FormatStringAsRequired(offenderString, filenameSpaceFill, filenameSpaceFillChar)}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Clean up working directory
            if (deleteWorkingDir && Directory.Exists(bundleSchema.WorkingDirectory))
            {
                Directory.Delete(bundleSchema.WorkingDirectory, true);
            }
        }

        private static bool ExtractArchive(String gzArchiveName, String destFolder, bool? isTarFormat = null)
        {
            bool extracted = false;
            try
            {
                if (!isTarFormat.HasValue)
                {
                    isTarFormat = gzArchiveName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase);
                }

                Encoding encoding = Encoding.UTF8;

                using (Stream inStream = File.OpenRead(gzArchiveName))
                using (Stream tarStream = isTarFormat.Value ? new TarInputStream(inStream, encoding) : new GZipInputStream(inStream))
                using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(tarStream, encoding))
                {
                    tarArchive.ExtractContents(destFolder);
                    tarArchive.Close();

                    tarStream.Close();
                    inStream.Close();
                }
                extracted = true;
            }
            catch (Exception e)
            {
                Logger.error($"EXCEPTION in BundleProcessing: [{e.Message}]");
            }

            return extracted;
        }

        private static void CreateTarGZ(string tgzFilename, string fileName)
        {
            try
            {
                using (var outStream = File.Create(tgzFilename))
                using (var gzoStream = new GZipOutputStream(outStream))
                using (var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
                {
                    tarArchive.RootPath = Path.GetDirectoryName(fileName);

                    var tarEntry = TarEntry.CreateEntryFromFile(fileName);
                    tarEntry.Name = Path.GetFileName(fileName);

                    tarArchive.WriteEntry(tarEntry, true);
                }
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
