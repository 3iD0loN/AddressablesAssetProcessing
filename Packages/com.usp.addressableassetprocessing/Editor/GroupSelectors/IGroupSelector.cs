namespace USP.AddressablesAssetProcessing
{
    public interface IGroupSelector<T>
    {
        #region Methods
        void Apply(T value);
        #endregion
    }
}
