using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    /// <summary>
    /// Represents a <see cref="MatchKeyExtractor"/> that will also detect CamelCase in a string and split the string by it,
    /// in addition to RegEx matching.
    /// </summary>
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
        /// <summary>
        /// Creates a new instance of the <see cref="WordMatchKeyExtractor"/> class.
        /// </summary>
        public WordMatchKeyExtractor()
        {
            // The filename matches if at least one character that is not an underscore, a space, or a number digit.
            MatchPattern = "[^_\\s\\d]+";

            // Handle how to extract the information from the match by 
            ExtractMatch = ExtractWords;
        }
        #endregion
    }
}