using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    public class WordMatchKeyExtractor : MatchKeyExtractor
    {
        #region Properties
        public Dictionary<string, List<string>> Transform = new Dictionary<string, List<string>>();
        #endregion

        #region Methods
        public WordMatchKeyExtractor()
        {
            MatchPattern = "[^_\\s\\d]+";
            ExtractMatch = ExtractWords;
        }

        private void ExtractWords(Match match, string original, HashSet<string> ignored, HashSet<string> result)
        {
            while (match.Success && !string.IsNullOrEmpty(match.Value))
            {
                if (ignored.Contains(match.Value))
                {
                    match = match.NextMatch();

                    continue;
                }

                bool found = Transform.TryGetValue(match.Value, out List<string> words);

                if (!found)
                {
                    words = KeyExtractor.SplitByCamelCase(match.Value);
                }

                foreach (var word in words)
                {
                    KeyExtractor.Add(word, ignored, result);
                }

                match = match.NextMatch();
            }
        }
        #endregion
    }
}