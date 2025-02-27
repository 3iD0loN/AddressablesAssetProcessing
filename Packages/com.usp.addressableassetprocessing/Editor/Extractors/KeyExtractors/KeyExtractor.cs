using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codice.CM.Common.Tree.Partial;
using log4net.Util;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

namespace USP.AddressablesAssetProcessing
{
    public class KeyExtractor : IExtractor<string, HashSet<string>>
    {
        #region Constants
        public static readonly MatchKeyExtractor[] IgnoreKeys = new[]
        {
            MatchKeyExtractor.IgnoreKey,
        };

        private static readonly char[] s_uppercase = CreateUppercase();
        #endregion

        #region Types
        protected delegate void CamelCase<T>(string origin, int startIndex, int length, ref T result);
        #endregion

        #region Static Methods
        #region String Manipulation
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

        public static List<int> IndexOfCamelCase(string value)
        {
            var result = new List<int>();

            SelectCamelCase(value, AddIndex, ref result);

            return result;
        }

        private static void AddIndex(string origin, int startIndex, int length, ref List<int> result)
        {
            result.Add(startIndex);
        }

        public static List<string> SplitByCamelCase(string value)
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
        #endregion

        protected static void KeyExtract(IEnumerable<string> directories,
            MatchKeyExtractor[] matchKeyExtractors,
            Dictionary<string, List<string>> transform,
            HashSet<string> ignored,
            ref HashSet<string> result)
        {
            // If there is no valid directories in the path, then:
            if (directories == null)
            {
                // Do nothing else.
                return;
            }

            // Otherwise, there are valid directories in the path.

            // For every directory in the path, perform the following:
            foreach (string directory in directories)
            {
                KeyExtract(directory, matchKeyExtractors, transform, ignored, ref result);
            }
        }

        protected static void KeyExtract(string value,
            MatchKeyExtractor[] matchKeyExtractors,
            Dictionary<string, List<string>> transform,
            HashSet<string> ignored,
            ref HashSet<string> result)
        {
            // If the match key extractors are null or empty, then:
            if (matchKeyExtractors == null || matchKeyExtractors.Length == 0)
            {
                // Process the value directly without matching anything.
                MatchKeyExtractor.Add(value, transform, null, ignored, result);

                // Do nothing else.
                return;
            }

            // Otherwise, the match key extractors are valid and populated.

            // For every match key extractor, perform the following:
            foreach (var matchKeyExtractor in matchKeyExtractors)
            {
                // If there is a valid lookup table, then
                if (ignored != null)
                {
                    // Override the individual lookup table.
                    matchKeyExtractor.Ignored = ignored;
                }

                // If there is a valid lookup table, then
                if (transform != null)
                {
                    // Override the individual lookup table.
                    matchKeyExtractor.Transform = transform;
                }

                // Extract the keys out of the value
                matchKeyExtractor.Extract(value, ref result);
            }
        }
        #endregion

        #region Properties
        public IStringSeparator Separator { get; set; }

        /// <summary>
        /// Gets or sets a collection of key extractors that process each directory.
        /// </summary>
        /// <remarks>
        /// Processes them in a "key extractor major" sequence:
        /// directory 1 <-> extractor 1,
        /// directory 1 <-> extractor 2,
        /// ...
        /// directory 1 <-> extractor N,
        /// directory 2 <-> extractor 1,
        /// ...
        /// directory M <-> extractor N
        /// </remarks>
        public MatchKeyExtractor[] DirectoryKeyExtractors { get; set; }

        /// <summary>
        /// Gets or sets a collection of key extractors that processes the filename.
        /// </summary>
        public MatchKeyExtractor[] FilenameKeyExtractors { get; set; }

        /// <summary>
        /// Gets or sets a lookup table that defines how a key is transformed into other keys.
        /// </summary>
        public Dictionary<string, List<string>> Transform { get; set; }

        /// <summary>
        /// Gets or sets a lookup table of keys to ignore if they are exactly matched.
        /// </summary>
        public HashSet<string> Ignored { get; set; }

        /// <summary>
        /// Gets or sets a set of keys to add consistently across.
        /// </summary>
        public HashSet<string> Added { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new instance of the <see cref="KeyExtractor"/> class.
        /// </summary>
        public KeyExtractor()
        {
            Separator = default;

            // Defaults add every directory in the path as a key.
            DirectoryKeyExtractors = default;

            // Defaults add the filename in the path as a key.
            FilenameKeyExtractors = default;

            Added = new HashSet<string>();
        }

        /// <summary>
        /// Extracts the keys from the input and populates them in the output.
        /// </summary>
        /// <param name="assetFileName">The asset file name to extract from.</param>
        /// <param name="result">The container that is populated by keys.</param>
        public void Extract(string assetFilePath, ref HashSet<string> result)
        {
            // If there is no valid separator, then:
            if (Separator == null)
            {
                // Do nothing.
                return;
            }

            // Otherwise, there is a valid separator.

            // Split the string by the delimiters.
            string[] splitAssetFilePath = Separator.Get(assetFilePath);

            // Remove the asset file name, which is the last element
            // from the split path to get just the directories.
            IEnumerable<string> directories = splitAssetFilePath.SkipLast(1);

            KeyExtract(directories, DirectoryKeyExtractors, Transform, Ignored, ref result);

            // Get the last item in the array, which is the asset file name.
            int lastIndex = splitAssetFilePath.Length - 1;
            string last = splitAssetFilePath[lastIndex];

            // Remove the file extension from the asset file name.
            string assetFileName = Path.GetFileNameWithoutExtension(last);

            KeyExtract(assetFileName, FilenameKeyExtractors, Transform, Ignored, ref result);

            // Add the constant keys to the result.
            result.UnionWith(Added);
        }
        #endregion
    }
}