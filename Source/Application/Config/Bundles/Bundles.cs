using System.Collections.Generic;

namespace BundleValidator.Config.Bundles
{
    internal class Bundles
    {
        public string BundlesSource { get; set; }
        public List<Packages> Packages { get; set; }
    }
}
