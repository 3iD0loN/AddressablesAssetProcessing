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

        public virtual void AddAsset(MetaAddressables.UserData userData, bool overwrite = false)
        {
            if (userData == null)
            {
                return;
            }

            var assetFilePath = AssetDatabase.GUIDToAssetPath(userData.Asset.Guid);

            AddAsset(userData, assetFilePath, overwrite);
        }

        public virtual void AddAsset(MetaAddressables.UserData userData, string assetFilePath, bool overwrite = false)
        {
            if (userData == null)
            {
                return;
            }

            bool found = dataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData other);

            if (found)
            {
                if (userData == other)
                {
                    return;
                }

                if (!overwrite)
                {
                    throw new System.Exception("Collision!");
                }
            }

            dataByAssetPath.Add(assetFilePath, userData);
        }
        #endregion
    }
#endif
}