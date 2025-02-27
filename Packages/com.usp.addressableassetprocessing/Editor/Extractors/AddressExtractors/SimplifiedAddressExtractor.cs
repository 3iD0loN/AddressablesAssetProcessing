namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class SimplifiedAddressExtractor : IKeyExtractor<string, string>
    {
        /// <summary>
        /// Extracts the keys from the input and populates them in the output.
        /// </summary>
        /// <param name="assetFilePath">The asset file path to extract from.</param>
        /// <param name="result">The resulting address.</param>
        public void Extract(string assetFilePath, ref string result)
        {
            result = MetaAddressables.AssetData.SimplifyAddress(assetFilePath);
        }
        
    }
}