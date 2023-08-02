using Application.Config;
using Application.Execution;
using BUNDLE_VERIFIER.Config;

namespace BUNDLE_VERIFIER
{
    class Program
    {
        private static AppConfig configuration;

        static void Main(string[] args)
        {
            configuration = SetupEnvironment.SetEnvironment();

            BundleProcessing.ProcessBundles(new BundleSchema()
            {
                SourceDirectory = SetupEnvironment.GetSourceDirectory(),
                WorkingDirectory = SetupEnvironment.GetWorkingDirectory(),
                BundleSource = configuration.Application.BundlesSource,
                Bundles = configuration.Application.Bundles
            });
        }
    }
}
