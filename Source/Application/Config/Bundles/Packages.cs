using System.Collections.Generic;

namespace BundleValidator.Config.Bundles
{
    internal class Packages
    {
        public string Name { get; set; }
        public List<Packages> ChildrenPackages { get; set; }
        public List<string> SignatureFiles { get; set; }
        public string PackageDirectory { get; set; }
        public string AuthoritySource { get; set; }
    }
}
