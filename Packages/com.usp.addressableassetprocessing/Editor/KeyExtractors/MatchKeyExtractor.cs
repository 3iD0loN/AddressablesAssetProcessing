using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    public class MatchKeyExtractor : IKeyExtractor<string, HashSet<string>>
    {
        #region Static Methods
        protected static bool Check(Match match)
        {
            return match.Success && !string.IsNullOrEmpty(match.Value);
        }

        public static void Add(Match match,
            string original,
            Dictionary<string, List<string>> transform,
            HashSet<string> ignored,
            HashSet<string> result)
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
                    bool found = transform.TryGetValue(match.Value, out List<string> words);

                    // If there were no associated words found, then: 
                    if (!found)
                    {
                        // Attempt to identify any camel-case words and spit them.
                        //words = KeyExtractor.SplitByCamelCase(match.Value);
                        words = new List<string>() { match.Value };
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

        #region Static Fields
        public static readonly MatchKeyExtractor IgnoreKey = new MatchKeyExtractor();
        #endregion

        #region Properties
        public string MatchPattern { get; set; }

        public Dictionary<string, List<string>> Transform = new Dictionary<string, List<string>>();

        public Action<Match, string, Dictionary<string, List<string>>, HashSet<string>, HashSet<string>> ExtractMatch { get; set; }

        public HashSet<string> Ignored { get; set; }
        #endregion

        #region Methods
        public void Extract(string assetFileName, HashSet<string> result)
        {
            if (string.IsNullOrEmpty(MatchPattern))
            {
                return;
            }

            // Match the patterns against the asset file name.
            Match match = Regex.Match(assetFileName, MatchPattern);

            ExtractMatch = ExtractMatch ?? Add;

            // Process the matches to get a set of labels.
            ExtractMatch(match, assetFileName, Transform, Ignored, result);
        }
        #endregion
    }
}