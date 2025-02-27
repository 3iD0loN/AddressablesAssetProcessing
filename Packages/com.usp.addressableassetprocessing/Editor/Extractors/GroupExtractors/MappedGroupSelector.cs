using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public abstract class MappedGroupSelector<I, O> : IGroupExtractor<I>
    {
        #region Properties
        public IDictionary<O, AddressableAssetGroupTemplate> GroupTemplateByKey
        {
            get;
            set;
        }
        #endregion

        #region Methods
        public MappedGroupSelector()
        {
            GroupTemplateByKey = new Dictionary<O, AddressableAssetGroupTemplate>();
        }

        public void Extract(I inputKey, ref AddressableAssetGroupTemplate value)
        {
            O internalKey = GetInternalKey(inputKey);

            // Get template associated with the key.
            value = Get(internalKey);
        }

        protected abstract O GetInternalKey(I groups);

        public virtual AddressableAssetGroupTemplate Get(O key)
        {
            GroupTemplateByKey.TryGetValue(key,
                    out AddressableAssetGroupTemplate result);

            return result;
        }

        public void Set(O key, AddressableAssetGroupTemplate value)
        {
            GroupTemplateByKey.Add(key, value);
        }
        #endregion
    }
}