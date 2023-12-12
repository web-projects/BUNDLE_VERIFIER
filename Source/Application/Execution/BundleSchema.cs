using BundleValidator.Config.Bundles;
using System.Collections.Generic;

namespace Application.Execution
{
    internal class BundleSchema
    {
        public string SourceDirectory { get; set; }
        public string WorkingDirectory { get; set; }
        public string BundleSource { get; set; }
        public List<Packages> Packages { get; set; }
    }
}
