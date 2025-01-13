using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    public class WordMatchKeyExtractor : MatchKeyExtractor
    {
        #region Static Methods
        private static bool Check(Match match)
        {
            return match.Success && !string.IsNullOrEmpty(match.Value);
        }
        #endregion

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
            // If there is initial match, then:
            if (!Check(match))
            {
                // Add the value as-is.
                KeyExtractor.Add(original, ignored, result);

                // Do nothing else.
                return;
            }

            // Otherwise, there is one match.

            do
            {
                // If the word is not contained in the set of ignored words, then the word should not be ignored.
                // If the word should not be ignored, then:
                if (!ignored.Contains(match.Value))
                {
                    // Attempt to find words that are associated with that word.
                    bool found = Transform.TryGetValue(match.Value, out List<string> words);
                    
                    // If there were no associated words found, then: 
                    if (!found)
                    {
                        // Attempt to identify any camel-case words and spit them. 
                        words = KeyExtractor.SplitByCamelCase(match.Value);
                    }

                    // There is at least one word in the list of words.

                    // For every word in the list of words, perform the following.
                    foreach (var word in words)
                    {
                        // Add the word to the list of extracted keys.
                        KeyExtractor.Add(word, ignored, result);
                    }
                }

                // Move onto the next item in the matches.
                match = match.NextMatch();
            }
            while (Check(match));
        }
        #endregion
    }
}