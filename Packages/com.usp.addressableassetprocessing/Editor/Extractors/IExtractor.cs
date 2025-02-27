namespace USP.AddressablesAssetProcessing
{
    public interface IExtractor<I, O>
    {
        /// <summary>
        /// Extracts the keys from the input and populates them in the output.
        /// </summary>
        /// <param name="value">The value to extract from.</param>
        /// <param name="result">The resulting keys.</param>
        void Extract(I value, ref O result);
    }
}