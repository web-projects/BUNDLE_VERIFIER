using System;

namespace BUNDLE_VERIFIER.Config
{
    [Serializable]
    internal class AppConfig
    {
        public Application Application { get; set; }
        public LoggerManager LoggerManager { get; set; }
    }
}
