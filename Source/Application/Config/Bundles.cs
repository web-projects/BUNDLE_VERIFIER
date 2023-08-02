using BundleValidator.Config;
using System.Collections.Generic;

namespace BUNDLE_VERIFIER.Config
{
    internal class Bundles
    {
        public string BundlesSource { get; set; }
        public List<Packages> Packages { get; set; }
    }
}
