using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    public class MatchKeyExtractor : IKeyExtractor<string, HashSet<string>>
    {
        #region Static Methods
        public static void Add(Match match, string original,
            HashSet<string> ignored, HashSet<string> result)
        {
            if (!match.Success)
            {
                return;
            }

            KeyExtractor.Add(match.Value, ignored, result);
        }
        #endregion

        #region Static Fields
        public static readonly MatchKeyExtractor IgnoreKey = new MatchKeyExtractor();
        #endregion

        #region Properties
        public string MatchPattern { get; set; }

        public Action<Match, string, HashSet<string>, HashSet<string>> ExtractMatch { get; set; }

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
            ExtractMatch(match, assetFileName, Ignored, result);
        }
        #endregion
    }
}