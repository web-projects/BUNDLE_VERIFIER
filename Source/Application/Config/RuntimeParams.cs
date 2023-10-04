namespace BundleValidator.Config
{
    public class RuntimeParams
    {
        public static readonly string BundleDirKey = "-BundleDir:";
        public static readonly string PinelineKey = "-Pipeline:";

        public string BundleDirectory { get; private set; }
        public bool InPipeline{ get; private set; } = false;

        public void ParseArguments(string[] arguments)
        {
            foreach (string arg in arguments)
            {
                if (arg.StartsWith(BundleDirKey))
                {
                    BundleDirectory = arg.Substring(BundleDirKey.Length);
                    continue;
                }

                if (arg.StartsWith(PinelineKey))
                {
                    InPipeline = arg.EndsWith(":true");
                    continue;
                }
            }
        }

        public override string ToString()
        {
            return $"BundleDirectory='{BundleDirectory}', InPipeline='{InPipeline}'";
        }
    }
}
