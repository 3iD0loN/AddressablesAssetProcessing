namespace USP.AddressablesAssetProcessing
{
    public interface IStringSeparator
    {
        #region Properties
        string RemovablePrefix { get; set; }

        char[] Delimiters { get; set; }
        #endregion

        #region Methods
        string[] Get(string assetFilePath);
        #endregion
    }
}
