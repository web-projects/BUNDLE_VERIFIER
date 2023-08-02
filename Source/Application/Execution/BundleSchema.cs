using BUNDLE_VERIFIER.Config;
using System.Collections.Generic;

namespace Application.Execution
{
    internal class BundleSchema
    {
        public string SourceDirectory { get; set; }
        public string WorkingDirectory { get; set; }
        public string BundleSource { get; set; }
        public List<Bundles> Bundles { get; set; }
    }
}
