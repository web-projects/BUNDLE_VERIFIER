using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace BundleValidator.Execution
{
    public class IEnumerableComparer<T> : IEqualityComparer<IEnumerable<string>>
    {
        public bool Equals(IEnumerable<string> source, IEnumerable<string> target)
        {
            if (source is null || target is null)
            {
                return false;
            }

            IEnumerator enumSource = source.GetEnumerator();
            IEnumerator enumTarget = target.GetEnumerator();

            while (enumSource.MoveNext() && enumTarget.MoveNext())
            {
                string val1 = enumSource.Current as string;
                string val2 = enumTarget.Current as string;

                Debug.WriteLine($"VAL1: {val1}");
                Debug.WriteLine($"VAL2: {val2}");

                string normalized1 = val1.Trim();
                string normalized2 = val2.Trim();

                if (normalized1 != normalized2)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IEnumerable<string> obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int hash = 17;
            unchecked // Overflow is fine, just wrap
            {
                foreach (string s in obj)
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        char ch = s[i];

                        if (!char.IsWhiteSpace(ch))
                        {
                            //use this line for case-insensitivity
                            hash = hash * 23 + char.ToLowerInvariant(ch).GetHashCode();
                        }

                        //use this line for case-sensitivity
                        //hash = hash * 23 + ch.GetHashCode();
                    }
                }
            }
            return hash;
        }
    }
}
