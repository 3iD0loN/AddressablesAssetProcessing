using Codice.CM.Client.Differences;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static UnityEngine.UI.Image;

namespace USP.AddressablesAssetProcessing
{
    public class MatchKeyExtractor : IExtractor<string, HashSet<string>>
    {
        #region Constants
        public const string AnyPattern = ".*";

        public static readonly MatchKeyExtractor IgnoreKey = new MatchKeyExtractor()
        {
            MatchPattern = string.Empty
        };
        #endregion

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
            MatchKeyExtractor.Add(match, original, transform, null, ignored, result);
        }

        public static void Add(Match match,
            string original,
            Dictionary<string, List<string>> transform,
            Func<string, List<string>> splitter,
            HashSet<string> ignored,
            HashSet<string> result)
        {
            // If there is no initial match, then:
            if (!Check(match))
            {
                // Add  original value as-is.
                MatchKeyExtractor.Add(original, transform, splitter, ignored, result);

                // Do nothing else.
                return;
            }

            // Otherwise, there is at least one match.

            do
            {
                MatchKeyExtractor.Add(match.Value, transform, splitter, ignored, result);

                // Move onto the next item in the matches.
                match = match.NextMatch();
            }
            while (Check(match));
        }

        public static void Add(string value,
            Dictionary<string, List<string>> transform,
            Func<string, List<string>> splitter,
            HashSet<string> ignored,
            HashSet<string> result)
        {
            // If the word is not contained in the set of ignored words, then the word should not be ignored.
            // If the word should not be ignored, then:
            if (ignored != null && ignored.Contains(value))
            {
                return;
            }

            // Attempt to find words that are associated with that word.
            // If there was no valid transformer, or no associated words found, then: 
            if (transform == null || !transform.TryGetValue(value, out List<string> words))
            {
                // If there is a splitter, then use it to split the value into words.
                // Otherwise, use the value as the single word.
                words = splitter != null ? splitter(value) : new List<string> { value };
            }
            

            // For every word in the list of words, perform the following.
            foreach (var word in words)
            {
                // Attempt to find words that are associated with that word.
                // If there was no valid transformer, or no associated words found, then: 
                if (transform == null || !transform.TryGetValue(word, out List<string> transformedWords))
                {
                    transformedWords = new List<string> { word };
                }

                // There is at least one word in the list of words.

                foreach (var transformedWord in transformedWords)
                {
                    // Add the word to the list of extracted keys.
                    MatchKeyExtractor.Add(transformedWord, ignored, result);
                }
            }
        }

        public static void Add(string value,
            HashSet<string> ignored, HashSet<string> result)
        {
            // If the value is in the ignore list, then:
            if (ignored != null && ignored.Contains(value))
            {
                // It will not be added. Do nothing else.
                return;
            }

            // Otherwise, the value should not be ignored.

            result.Add(value);
        }
        #endregion

        #region Properties
        /// <summary>
        /// The .Net RegEx pattern to match a string against. 
        /// If the pattern is matched, then the extractor attempts to extract a key out of it.
        /// </summary>
        public string MatchPattern { get; set; } = AnyPattern;

        /// <summary>
        /// Gets or sets a delegate to extract the information from the match.
        /// </summary>
        public Action<Match, string, Dictionary<string, List<string>>, HashSet<string>, HashSet<string>> ExtractMatch { get; set; }

        /// <summary>
        /// Gets or sets a lookup table that defines how a key is transformed into other keys.
        /// </summary>
        public Dictionary<string, List<string>> Transform { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Gets or sets a lookup table of keys to ignore if they are exactly matched.
        /// </summary>
        public HashSet<string> Ignored { get; set; } = new HashSet<string>();
        #endregion

        #region Methods
        /// <summary>
        /// Extracts the keys from the input and populates them in the output.
        /// </summary>
        /// <param name="assetFileName">The asset file name to extract from.</param>
        /// <param name="result">The container that is populated by keys.</param>
        public void Extract(string assetFileName, ref HashSet<string> result)
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