using System.Collections.Generic;

namespace BUNDLE_VERIFIER.Config
{
    internal class Bundles
    {
        public string Name { get; set; }
        public List<Bundles> ChildrenBundles { get; set; }
        public List<string> SignatureFiles { get; set; }
        public string PackageDirectory { get; set; }
        public string AuthoritySource { get; set; }
    }
}
