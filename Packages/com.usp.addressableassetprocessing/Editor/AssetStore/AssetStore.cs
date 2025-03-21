using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
    using UnityEditor;
    using USP.MetaAddressables;

    public abstract class AssetStore : IAssetStore
    {
        #region Fields
        protected Dictionary<string, MetaAddressables.UserData> dataByAssetPath = new Dictionary<string, MetaAddressables.UserData>();

        protected HashSet<string> globalLabels = new HashSet<string>();
        #endregion

        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        public virtual bool IsReadOnly { get; }

        /// <summary>
        /// The Addressables data associated with the asset file path.
        /// </summary>
        public virtual IReadOnlyDictionary<string, MetaAddressables.UserData> DataByAssetPath => dataByAssetPath;

        /// <summary>
        /// The global set of labels.
        /// </summary>
        public virtual IReadOnlyCollection<string> GlobalLabels => globalLabels;
        #endregion

        #region Methods
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

            dataByAssetPath[assetFilePath] = userData;
        }

        public virtual void AddGlobalLabels(HashSet<string> labels)
        {
            AddGlobalLabels(labels as IEnumerable<string>);
        }

        protected void AddGlobalLabels(IEnumerable<string> labels)
        {
            globalLabels.UnionWith(labels);
        }
        #endregion
    }
}