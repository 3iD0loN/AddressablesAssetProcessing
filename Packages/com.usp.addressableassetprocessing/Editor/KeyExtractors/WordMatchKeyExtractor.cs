using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    public class WordMatchKeyExtractor : MatchKeyExtractor
    {
        #region Static Methods
        private static void ExtractWords(Match match,
            string original,
            Dictionary<string, List<string>> transform,
            HashSet<string> ignored,
            HashSet<string> result)
        {
            MatchKeyExtractor.Add(match, original, transform, KeyExtractor.SplitByCamelCase, ignored, result);
        }
        #endregion

        #region Methods
        public WordMatchKeyExtractor()
        {
            MatchPattern = "[^_\\s\\d]+";
            ExtractMatch = ExtractWords;
        }
        #endregion
    }
}