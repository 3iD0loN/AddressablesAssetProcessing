namespace USP.AddressablesAssetProcessing
{
    public class StringSeparator : IStringSeparator
    {
        #region Static Methods
        protected static string TrimStart(string value, string prefix)
        {
            int startIndex = value.IndexOf(prefix);

            if (startIndex == -1)
            {
                return value;
            }

            return value.Substring(startIndex + prefix.Length);
        }
        #endregion

        #region Properties
        public string RemovablePrefix { get; set; }

        public char[] Delimiters { get; set; }
        #endregion

        #region Methods
        public string[] Get(string assetFilePath)
        {
            // Remove the prefix string from the of the file path, if it is a substring.
            assetFilePath = TrimStart(assetFilePath, RemovablePrefix);

            // Split the string by the delimiters.
            return assetFilePath.Split(Delimiters);
        }
        #endregion
    }
}