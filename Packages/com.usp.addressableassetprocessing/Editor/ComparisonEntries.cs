using System;
using System.Collections.Generic;

namespace USP.AddressablesAssetProcessing
{
    using System.Collections;
    using System.Linq.Expressions;
    using System.Reflection;
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;
#endif

    public class ComparisonEntry
    {
        public IPropertyComparer comparer;

        public Type entryType;

        public string entryName;

        public object fileProcessingAsset;

        public bool leftCompare;

        public object metaDataAsset;

        public bool rightCompare;

        public object addressablesAsset;

        public IEnumerable<ComparisonEntry> children;
    }

    public class ComparisonEntries
    {
        private static UserDataComparer userDataComparer = new UserDataComparer();

        public static ComparisonEntry CreateEntry(CombinedAssetApplicator combinedAssetApplicator, string assetFilePath)
        {
            var result = new ComparisonEntry();
            result.comparer = userDataComparer;
            result.entryType = typeof(MetaAddressables.UserData);
            result.entryName = assetFilePath;

            if (combinedAssetApplicator.SimulatedAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData fileProcessingAsset))
            {
                result.fileProcessingAsset = fileProcessingAsset;
            }
            if (combinedAssetApplicator.MetaAddressablesAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData metaDataAsset))
            {
                result.metaDataAsset = metaDataAsset;
            }
            if (combinedAssetApplicator.AddressablesAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData addressablesAsset))
            {
                result.addressablesAsset = addressablesAsset;
            }

            PopulateCompare(result);

            PopulateChildren(result);

            return result;
        }

        private static void PopulateCompare(ComparisonEntry comparisonEntry)
        {
            IPropertyComparer comparer = comparisonEntry.comparer;

            var fileProcessingAssetHash = comparisonEntry.fileProcessingAsset != null ? comparer.GetHashCode(comparisonEntry.fileProcessingAsset) : 0;
            var metaDataAssetHash = comparisonEntry.metaDataAsset != null ? comparer.GetHashCode(comparisonEntry.metaDataAsset) : 0;
            var addressablesAssetHash = comparisonEntry.addressablesAsset != null ? comparer.GetHashCode(comparisonEntry.addressablesAsset) : 0;

            //bool leftCompareA = comparer.Equals(comparisonEntry.fileProcessingAsset, comparisonEntry.metaDataAsset);
            bool leftCompareB = fileProcessingAssetHash == metaDataAssetHash;
            //UnityEngine.Debug.Assert(leftCompareA == leftCompareB, "compare between file processing and meta files don't match.");
            comparisonEntry.leftCompare = leftCompareB;

            //bool rightCompareA = comparer.Equals(comparisonEntry.metaDataAsset, comparisonEntry.addressablesAsset);
            bool rightCompareB = metaDataAssetHash == addressablesAssetHash;
            //UnityEngine.Debug.Assert(rightCompareA == rightCompareB, "compare between meta file and addressables don't match.");
            comparisonEntry.rightCompare = rightCompareB;
        }

        private static void PopulateChildren(ComparisonEntry comparisonEntry)
        {
            if (comparisonEntry.comparer.Children == null)
            {
                if (comparisonEntry.comparer is GroupSchemaDataComparer groupSchemaDataComparer)
                {
                    var type = comparisonEntry.fileProcessingAsset.GetType();
                    var comparer = GroupSchemaDataComparer.GetComparer(type);

                    var oldComparer = comparisonEntry.comparer;
                    comparisonEntry.comparer = comparer;

                    PopulateChildren(comparisonEntry);
                    return;
                }

                if (comparisonEntry.comparer is EnumerableComparer enumerableComparer)
                {
                    var entriesByHash = new Dictionary<int, ComparisonEntry>();

                    AddEnumerableElements(comparisonEntry, enumerableComparer, x => x.fileProcessingAsset, entriesByHash);
                    AddEnumerableElements(comparisonEntry, enumerableComparer, x => x.metaDataAsset, entriesByHash);
                    AddEnumerableElements(comparisonEntry, enumerableComparer, x => x.addressablesAsset, entriesByHash);

                    int i = 0;
                    foreach (var pair in entriesByHash)
                    {
                        var elementComparisonEntry = pair.Value;
                        elementComparisonEntry.entryName = $"Element {i}";

                        PopulateCompare(elementComparisonEntry);
                        PopulateChildren(elementComparisonEntry);
                        ++i;
                    }

                    comparisonEntry.children = entriesByHash.Values;
                }
            }
            else
            {
                var children = new List<ComparisonEntry>();
                foreach (PropertyComparerPair child in comparisonEntry.comparer.Children)
                {
                    var propertyInfo = child.GetMemberInfo<PropertyInfo>();

                    var childComparisonEntry = new ComparisonEntry();

                    childComparisonEntry.comparer = child.PropertyComparer;
                    childComparisonEntry.entryType = propertyInfo.PropertyType;
                    childComparisonEntry.entryName = propertyInfo.Name;

                    childComparisonEntry.fileProcessingAsset = comparisonEntry.fileProcessingAsset != null ? child.Access(comparisonEntry.fileProcessingAsset) : null;
                    childComparisonEntry.metaDataAsset = comparisonEntry.metaDataAsset != null ? child.Access(comparisonEntry.metaDataAsset) : null;
                    childComparisonEntry.addressablesAsset = comparisonEntry.addressablesAsset != null ? child.Access(comparisonEntry.addressablesAsset) : null;

                    PopulateCompare(childComparisonEntry);

                    PopulateChildren(childComparisonEntry);

                    children.Add(childComparisonEntry);
                }

                comparisonEntry.children = children;
            }
        }

        private static void AddEnumerableElements(ComparisonEntry comparisonEntry, EnumerableComparer enumerableComparer,
            Expression<Func<ComparisonEntry, object>> accessor, Dictionary<int, ComparisonEntry> entriesByHash)
        {
            if (accessor.Body is not MemberExpression memberExpression)
            {
                throw new InvalidCastException();
            }

            if (memberExpression.Member is not FieldInfo fieldInfo)
            {
                throw new InvalidCastException();
            }

            var enumerable = fieldInfo.GetValue(comparisonEntry) as IEnumerable;

            if (enumerable == null)
            {
                return;
            }

            IEnumerator enumerator = enumerable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int hash = GetHashCode(enumerator.Current);

                if (!entriesByHash.TryGetValue(hash, out ComparisonEntry childComparisonEntry))
                {
                    childComparisonEntry = new ComparisonEntry();

                    childComparisonEntry.comparer = enumerableComparer.ItemComparer;
                    childComparisonEntry.entryType = enumerator.Current.GetType();

                    entriesByHash.Add(hash, childComparisonEntry);
                }

                fieldInfo.SetValue(childComparisonEntry, enumerator.Current);
            }
        }

        private static int GetHashCode(object value)
        {
            if (value is KeyValuePair<Type, MetaAddressables.GroupSchemaData> pair)
            {
                return pair.Key.GetHashCode();
            }

            return value.GetHashCode();
        }

        /*/
        Dictionary<string, int> leftCompare = new Dictionary<string, int>();

        Dictionary<string, int> rightCompare = new Dictionary<string, int>();

        public void GetAssets()
        {
            var userDataComparer = new UserDataComparer();
            foreach ((string assetPath, MetaAddressables.UserData userData) in simulatedAssetStore.DataByAssetPath)
            {
                int hash = userDataComparer.GetHashCode(userData);

                AddCompare(leftCompare, assetPath, hash);
            }

            foreach ((string assetPath, MetaAddressables.UserData userData) in metaAddressablesAssetStore.DataByAssetPath)
            {
                int hash = userDataComparer.GetHashCode(userData);

                AddCompare(leftCompare, assetPath, hash);
                AddCompare(rightCompare, assetPath, hash);
            }

            foreach ((string assetPath, MetaAddressables.UserData userData) in addressablesAssetStore.DataByAssetPath)
            {
                int hash = userDataComparer.GetHashCode(userData);

                AddCompare(rightCompare, assetPath, hash);
            }
        }

        private static void AddCompare(Dictionary<string, int> compare, string assetPath, int hash)
        {
            if (!compare.TryGetValue(assetPath, out int value))
            {
                compare.Add(assetPath, hash);
            }
            else
            {
                compare[assetPath] = value & ~hash;
            }
        }
        //*/
    }
}
