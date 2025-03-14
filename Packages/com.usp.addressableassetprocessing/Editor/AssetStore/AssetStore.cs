using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public interface IAssetStore
    {
        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        bool IsReadOnly { get; }

        IReadOnlyDictionary<string, MetaAddressables.UserData> DataByAssetPath { get; }

        ISet<string> GlobalLabels { get; }
        #endregion

        #region Methods
        void AddGlobalLabels(HashSet<string> labels);
        #endregion
    }

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

        public virtual IReadOnlyDictionary<string, MetaAddressables.UserData> DataByAssetPath => dataByAssetPath;

        public virtual ISet<string> GlobalLabels => globalLabels;
        #endregion

        #region Methods
        public virtual void AddGlobalLabels(HashSet<string> labels)
        {
            AddGlobalLabels(labels as IEnumerable<string>);
        }

        protected void AddGlobalLabels(IEnumerable<string> labels)
        {
            GlobalLabels.UnionWith(labels);
        }
        #endregion
    }
}