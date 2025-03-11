using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

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

        public void GetFiles(string parentDirectoryPath, ref IEnumerable<string> result)
        {
            IEnumerable<string> files = GetFiles(parentDirectoryPath);

            result = Enumerable.Concat(result, files);

            //result = result != null ? Enumerable.Concat(result, files) : files;
        }

        /*/
        public void GetFiles(string parentDirectoryPath, ref IEnumerable<(string, string)> result)
        {
            IEnumerable<string> files = GetFiles(parentDirectoryPath);

            IEnumerable<(string, string)> x = files
                .Where(file => File.Exists(file))
                .Select((string file) =>
                {
                    string guid = AssetDatabase.AssetPathToGUID(file);

                    return (guid, file);
                });

            result = result != null ? Enumerable.Concat(result, x) : x;
        }
        //*/

        public void GetFiles(string parentDirectoryPath, ref Dictionary<string, string> result)
        {
            IEnumerable<string> files = GetFiles(parentDirectoryPath);

            foreach (string file in files)
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(file);

                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                result.Add(guid, file);
            }
        }

        private IEnumerable<string> GetFiles(string parentDirectoryPath)
        {
            const string AllFilesWithExtensionsPattern = "*.*";

            return Directory.GetFiles(parentDirectoryPath, AllFilesWithExtensionsPattern, SearchOptions)
                .Where(FilepathMatches);
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