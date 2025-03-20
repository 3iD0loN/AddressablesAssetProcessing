namespace USP.AddressablesAssetProcessing
{
    using UnityEditor;
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;

    public class MetaAddressablesAssetStore : AssetStore
    {
        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        public override bool IsReadOnly => false;
        #endregion

        #region Methods
        public virtual void AddAsset(string assetFilePath)
        {
            bool found = dataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData userData);

            if (found)
            {
                return;
            }

            userData = MetaAddressables.Read(assetFilePath);

            if (userData == null)
            {
                return;
            }

            dataByAssetPath.Add(assetFilePath, userData);
        }
        #endregion
    }
#endif
}