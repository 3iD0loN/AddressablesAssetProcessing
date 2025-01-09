using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codice.CM.Common.Tree.Partial;
using log4net.Util;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

public class KeyExtractor : IKeyExtractor<string, HashSet<string>>
{
    protected delegate void CamelCase<T>(string origin, int startIndex, int length, ref T result);

    #region Static Methods
    private static char[] CreateUppercase()
    {
        const int Length = ('Z' - 'A') + 1;

        char[] result = new char[Length];

        for (int i = 0; i < Length; i++)
        {
            result[i] = (char)(i + 'A');
        }

        return result;
    }

    private static void SelectCamelCase<T>(string value, CamelCase<T> transform, ref T result)
    {
        int startIndex = 0;

        do
        {
            int index = value.IndexOfAny(s_uppercase, startIndex + 1);

            if (index == -1)
            {
                index = value.Length;
            }

            transform(value, startIndex, index - startIndex, ref result);

            startIndex = index;
        }
        while (startIndex < value.Length);
    }

    protected static List<int> IndexOfCamelCase(string value)
    {
        var result = new List<int>();

        SelectCamelCase(value, AddIndex, ref result);

        return result;
    }

    private static void AddIndex(string origin, int startIndex, int length, ref List<int> result)
    {
        result.Add(startIndex);
    }

    protected static List<string> SplitByCamelCase(string value)
    {
        var result = new List<string>();

        SelectCamelCase(value, AddWord, ref result);

        return result;
    }

    private static void AddWord(string origin, int startIndex, int length, ref List<string> result)
    {
        var sub = origin.Substring(startIndex, length);

        result.Add(sub);
    }

    protected static string SpaceCamelCase(string value)
    {
        SelectCamelCase(value, SpaceWord, ref value);

        return value;
    }

    private static void SpaceWord(string origin, int startIndex, int length, ref string result)
    {
        if (startIndex == 0)
        {
            result = origin;

            return;
        }

        result = origin.Insert(startIndex, " ");
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

    protected static void KeyExtract(IEnumerable<string> directories,
        MatchKeyExtractor[] matchKeyExtractors,
        HashSet<string> ignored,
        HashSet<string> result)
    {
        if (directories == null)
        {
            return;
        }

        foreach (string directory in directories)
        {
            KeyExtract(directory, matchKeyExtractors, ignored, result);
        }
    }

    protected static void KeyExtract(string assetFileName,
        MatchKeyExtractor[] matchKeyExtractors,
        HashSet<string> ignored,
        HashSet<string> result)
    {
        if (matchKeyExtractors == null || matchKeyExtractors.Length == 0)
        {
            Add(assetFileName, ignored, result);

            return;
        }

        foreach (var matchKeyExtractor in matchKeyExtractors)
        {
            matchKeyExtractor.Ignored = ignored;
            matchKeyExtractor.Extract(assetFileName, result);
        }
    }
    #endregion

    #region Static Fields
    private static char[] s_uppercase = CreateUppercase();

    public static readonly MatchKeyExtractor[] IgnoreKeys = new[]
    {
        MatchKeyExtractor.IgnoreKey,
    };
    #endregion

    #region Properties
    public IStringSeparator Separator { get; set; }

    public HashSet<string> Added { get; set; }

    public MatchKeyExtractor[] DirectoryKeyExtractors { get; set; }

    public MatchKeyExtractor[] FilenameKeyExtractors { get; set; }

    public HashSet<string> Ignored { get; set; }
    #endregion

    #region Methods
    public KeyExtractor()
    {
        Separator = default;
        Added = new HashSet<string>();
        DirectoryKeyExtractors = new MatchKeyExtractor[0];
        FilenameKeyExtractors = new MatchKeyExtractor[0];
        Ignored = new HashSet<string>();
    }

    public void Extract(string assetFilePath, HashSet<string> result)
    {
        if (Separator == null)
        {
            return;
        }

        // Split the string by the delimiters.
        string[] splitAssetFilePath = Separator.Get(assetFilePath);

        // Get the last item in the array, which is the asset file name.
        int lastIndex = splitAssetFilePath.Length - 1;
        string last = splitAssetFilePath[lastIndex];

        // Remove the file extension from the asset file name.
        string assetFileName = Path.GetFileNameWithoutExtension(last);

        KeyExtract(assetFileName, FilenameKeyExtractors, Ignored, result);

        // Remove the asset file name, which is the last element
        // from the split path to get just the directories.
        IEnumerable<string> directories = splitAssetFilePath.SkipLast(1);

        KeyExtract(directories, DirectoryKeyExtractors, Ignored, result);

        result.UnionWith(Added);
    }
    #endregion
}
