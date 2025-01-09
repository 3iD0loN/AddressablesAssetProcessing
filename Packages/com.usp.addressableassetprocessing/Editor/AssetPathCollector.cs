using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class AssetPathCollector
{
    #region Fields
    protected bool _recacheMatch = true;

    private string _matchPattern;

    protected bool _recacheIgnore = true;

    private string _ignoreMatchPattern;
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
    protected virtual string GetMatchPattern() => MatchPattern;

    protected virtual string GetIgnoreMatchPattern() => IgnoreMatchPattern;

    public IEnumerable<string> GetFiles(string parentDirectoryPath)
    {
        const string AllFilesWithExtensionsPattern = "*.*";

        var files = Directory.GetFiles(parentDirectoryPath, AllFilesWithExtensionsPattern, SearchOptions)
            .Where(FilepathMatches);

        return files;
    }

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