using System;
using System.Collections.Generic;
using BundleValidator.Config.Bundles;

namespace BUNDLE_VERIFIER.Config
{
    [Serializable]
    internal class AppConfig
    {
        public Application Application { get; set; }
        public LoggerManager LoggerManager { get; set; }
        public List<Bundles> Bundles { get; set; }
    }
}
