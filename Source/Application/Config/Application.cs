using System;
using System.Collections.Generic;

namespace BUNDLE_VERIFIER.Config
{
    internal class Application
    {
        public Colors Colors { get; set; }
        public bool EnableColors { get; set; }
        public string BundlesSource { get; set; }
        public List<Bundles> Bundles { get; set; }
    }

    [Serializable]
    public class Colors
    {
        public string ForeGround { get; set; } = "WHITE";
        public string BackGround { get; set; } = "BLUE";
    }
}
