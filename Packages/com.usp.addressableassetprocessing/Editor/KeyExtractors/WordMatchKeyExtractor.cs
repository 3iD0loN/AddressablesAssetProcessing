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
            // If there is no initial match, then:
            if (!Check(match))
            {
                // Add  original value as-is.
                Add(original, transform, KeyExtractor.SplitByCamelCase, ignored, result);

                // Do nothing else.
                return;
            }

            // Otherwise, there is at least one match.

            do
            {
                Add(match.Value, transform, KeyExtractor.SplitByCamelCase, ignored, result);

                // Move onto the next item in the matches.
                match = match.NextMatch();
            }
            while (Check(match));
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