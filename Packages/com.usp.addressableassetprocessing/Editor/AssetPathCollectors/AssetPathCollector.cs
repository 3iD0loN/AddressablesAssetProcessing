using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace USP.AddressablesAssetProcessing
{
    public class AssetPathCollector
    {
        #region Fields
        private string _matchPattern;

        private string _ignoreMatchPattern;

        protected bool _recacheMatch;

        protected bool _recacheIgnore;
        #endregion

        #region Properties
        public SearchOption SearchOptions { get; set; }

        public string MatchPattern
        {
            get => _matchPattern;
            set
            {
                _recacheMatch = true;
                _matchPattern = value;
            }
        }

        public string IgnoreMatchPattern
        {
            get => _ignoreMatchPattern;
            set
            {
                _recacheIgnore = true;
                _ignoreMatchPattern = value;
            }
        }
        #endregion

        #region Methods
        public AssetPathCollector(string matchPattern = null,
            string ignoreMatchPattern = null,
            SearchOption searchOptions = SearchOption.AllDirectories)
        {
            _matchPattern = matchPattern;
            _ignoreMatchPattern = ignoreMatchPattern;
            SearchOptions = searchOptions;
            _recacheMatch = true;
            _recacheIgnore = true;
        }

        public void GetFiles(string parentDirectoryPath, ref List<string> result)
        {
            const string AllFilesWithExtensionsPattern = "*.*";

            IEnumerable<string> files = Directory.GetFiles(parentDirectoryPath, AllFilesWithExtensionsPattern, SearchOptions)
                .Where(FilepathMatches);

            result.AddRange(files);
        }

        protected virtual string GetMatchPattern() => MatchPattern;

        protected virtual string GetIgnoreMatchPattern() => IgnoreMatchPattern;

        protected virtual bool FilepathMatches(string filepath)
        {
            bool isIgnored = false;
            string ignoreMatchPattern = GetIgnoreMatchPattern();
            if (!string.IsNullOrEmpty(ignoreMatchPattern))
            {
                isIgnored = Regex.IsMatch(filepath, ignoreMatchPattern);
            }

            if (isIgnored)
            {
                return false;
            }

            bool isMatched = true;
            string matchPattern = GetMatchPattern();
            if (!string.IsNullOrEmpty(matchPattern))
            {
                isMatched = Regex.IsMatch(filepath, matchPattern);
            }

            return isMatched;
        }
        #endregion
    }
}