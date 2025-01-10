namespace USP.AddressablesAssetProcessing
{
    public interface IKeyExtractor<I, O>
    {
        void Extract(I value, O result);
    }
}
