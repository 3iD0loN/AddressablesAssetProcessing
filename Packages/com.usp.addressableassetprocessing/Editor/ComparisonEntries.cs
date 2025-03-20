using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace USP.AddressablesAssetProcessing
{
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;
#endif

    public class CompareOperand
    {
        public object value;

        public bool isReadonly;

        public CompareOperand(object value, bool isReadonly = false)
        {
            this.value = value;
            this.isReadonly = isReadonly;
        }
    }

    public struct CompareOperation
    {
        public IPropertyComparer comparer { get; }

        public CompareOperand leftHand { get; }

        public CompareOperand rightHand { get; }

        public bool result { get; }

        public CompareOperation(IPropertyComparer comparer, CompareOperand leftHand, CompareOperand rightHand)
        {
            this.comparer = comparer;
            this.leftHand = leftHand;
            this.rightHand = rightHand;
            result = false;
            this.result = Operate(this);
        }

        public static bool Operate(CompareOperation operation)
        {
            var leftHash = operation.leftHand.value != null ? operation.comparer.GetHashCode(operation.leftHand.value) : 0;
            var rightHash = operation.rightHand.value != null ? operation.comparer.GetHashCode(operation.rightHand.value) : 0;

            return leftHash == rightHash;
        }
    }

    public class ComparisonEntry
    {
        public string entryName;

        public Type entryType;

        public IPropertyComparer comparer;

        public IReadOnlyDictionary<string, CompareOperand> compareTargets;

        public IReadOnlyDictionary<string, CompareOperation> compareOperations;

        public IEnumerable<ComparisonEntry> children;
    }

    public class ComparisonEntries
    {
        private const string ProcessingDataKey = "processing-data";

        private const string MetafileDataKey = "metafile-data";

        private const string AddressablesDataKey = "addressables-data";

        private static UserDataComparer userDataComparer = new UserDataComparer();

        public static ComparisonEntry CreateEntry(CombinedAssetApplicator combinedAssetApplicator, string assetFilePath)
        {
            var properties = new Dictionary<string, CompareOperand>(3);
            
            combinedAssetApplicator.SimulatedAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData processingData);
            properties[ProcessingDataKey] = new CompareOperand(processingData, combinedAssetApplicator.SimulatedAssetStore.IsReadOnly);

            combinedAssetApplicator.MetaAddressablesAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData metafileData);
            properties[MetafileDataKey] = new CompareOperand(metafileData, combinedAssetApplicator.MetaAddressablesAssetStore.IsReadOnly);

            combinedAssetApplicator.AddressablesAssetStore.DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData addressablesData);
            properties[AddressablesDataKey] = new CompareOperand(addressablesData, combinedAssetApplicator.AddressablesAssetStore.IsReadOnly);

            var result = new ComparisonEntry();
            result.comparer = userDataComparer;
            result.entryType = typeof(MetaAddressables.UserData);
            result.entryName = assetFilePath;
            result.compareTargets = properties;
            result.compareOperations = PopulateCompare(result.comparer, result.compareTargets);
            result.children = PopulateChildren(result.entryType, result.comparer, result.compareTargets);

            return result;
        }

        private static IReadOnlyDictionary<string, CompareOperation> PopulateCompare(
            IPropertyComparer comparer, IReadOnlyDictionary<string, CompareOperand> compareTargets)
        {
            var processingData = compareTargets[ProcessingDataKey];
            var metafileData = compareTargets[MetafileDataKey];
            var addressablesData = compareTargets[AddressablesDataKey];

            var result = new Dictionary<string, CompareOperation>(2);
            result["processing-metafile-compare"] = new CompareOperation(comparer, processingData, metafileData);
            result["metafile-addressables-compare"] = new CompareOperation(comparer, metafileData, addressablesData);

            return result;
        }

        private static IEnumerable<ComparisonEntry> PopulateChildren(
            Type type, IPropertyComparer comparer,
            IReadOnlyDictionary<string, CompareOperand> compareTargets)
        {
            var comparerChildren = comparer.Children;

            if (comparerChildren == null)
            {
                if (comparer is GroupSchemaDataComparer abstractComparer)
                {
                    comparer = GroupSchemaDataComparer.GetComparer(type);
                    comparerChildren = comparer.Children;
                }
                else if (comparer is EnumerableComparer enumerableComparer)
                {
                    // Get the comparer for the elements in the container.
                    // It is now the comparer for the children, which will be the elements in the container.
                    comparer = enumerableComparer.ItemComparer;

                    IPropertyComparer itemComparer;
                    /*/
                    var comparerType = comparer.GetType();
                    if (comparerType.IsGenericType && comparerType.GetGenericTypeDefinition() == typeof(KeyValuePairComparer<,>))
                    {
                        PropertyComparerPair keyComparerPair = comparer.Children.First();
                        itemComparer = Activator.CreateInstance(comparerType, keyComparerPair.PropertyComparer) as IPropertyComparer;
                    }
                    else
                    //*/
                    {
                        itemComparer = enumerableComparer.ItemComparer;
                    }

                    var propertyComparerByHash = new Dictionary<object, PropertyComparerPair>();

                    var tupleComparer = new PropertyComparer<Tuple<object, object>>((x => x.Item1, itemComparer), (x => x.Item2, ObjectComparer.Default));
                    var x = new HashSet<Tuple<object, object>>(tupleComparer);

                    // For every object that will be compared, perform the following:
                    foreach ((string key, CompareOperand parentOperand) in compareTargets)
                    {
                        // If the current object that is being compared is not an enumerable container, then:
                        if (parentOperand.value is not IEnumerable enumerable)
                        {
                            // Skip this item.
                            continue;
                        }

                        // Otherwise, the current object being compared is an enumerable container.

                        // For every item in the container, perform the following:
                        foreach (object value in enumerable)
                        {
                            var k = new Tuple<object, object>(value, enumerable);
                            x.Add(k);

                            /*/
                            if (value is KeyValuePair<Type, MetaAddressables.GroupSchemaData> y)
                            {
                                if (y.Value is MetaAddressables.BundledAssetGroupSchemaData z)
                                {
                                    UnityEngine.Debug.Log($"A container: {ObjectComparer.Default.GetHashCode(parentOperand.value)} {z}, BundleMode: {z.BundleMode}");
                                }
                            }
                            //*/

                            bool found = propertyComparerByHash.TryGetValue(value, out PropertyComparerPair pair);

                            if (!found)
                            {
                                Expression<Func<IEnumerable, object>> z = (IEnumerable y) => GetValueOrDefault(x, value, y); 
                                pair = new PropertyComparerPair(z, comparer);
                                 
                                propertyComparerByHash.Add(value, pair);
                            }
                        }
                    }

                    comparerChildren = propertyComparerByHash.Values;

                    /*/
                    UnityEngine.Debug.Log("0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0-0");

                    foreach (PropertyComparerPair comparerChild in comparerChildren)
                    {
                        var properties = new Dictionary<string, CompareOperand>(compareTargets.Count);
                        foreach ((string key, CompareOperand parentOperand) in compareTargets)
                        {
                            if (parentOperand == null)
                            {
                                continue;
                            }

                            object childValue = (parentOperand.value != null) ? comparerChild.Access(parentOperand.value) : null;

                            var childOperand = new CompareOperand(childValue, parentOperand.isReadonly);
                            properties.Add(key, childOperand);
                        }
                    }

                    UnityEngine.Debug.Log("1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1");
                    
                    return null;
                    //*/
                }
                else
                {
                    return null;
                }
            }

            var children = new List<ComparisonEntry>();
            int i = 0;
            foreach (PropertyComparerPair comparerChild in comparerChildren)
            {
                var properties = new Dictionary<string, CompareOperand>(compareTargets.Count);
                foreach ((string key, CompareOperand parentOperand) in compareTargets)
                {
                    if (parentOperand == null)
                    {
                        continue;
                    }

                    object childValue = (parentOperand.value != null) ? comparerChild.Access(parentOperand.value) : null;

                    var childOperand = new CompareOperand(childValue, parentOperand.isReadonly);
                    properties.Add(key, childOperand);
                }

                // The first item should exist along with at least one other to compare against.
                // It should be available to compare against all other items, which are alike enough to compare.
                // Therefore, there is at least one and it would be reasonable have the same type.
                CompareOperand firstOperand = properties.First(pair => pair.Value.value != null).Value;
                object firstValue = firstOperand.value;

                var propertyInfo = comparerChild.GetMemberInfo<PropertyInfo>();

                // If the first value is valid, then use the concrete type, since a property, field, or method
                // can obscure the type of an inherited item. Otherwise, fallback to using the property type.  
                Type childType = (firstValue != null) ? firstValue.GetType() : default;

                var childEntry = new ComparisonEntry();
                childEntry.entryName = propertyInfo != null ? propertyInfo.Name : $"Element {i}";
                childEntry.entryType = childType;
                childEntry.comparer = comparerChild.PropertyComparer;
                childEntry.compareTargets = properties;
                childEntry.compareOperations = PopulateCompare(childEntry.comparer, childEntry.compareTargets);
                childEntry.children = PopulateChildren(childEntry.entryType, childEntry.comparer, childEntry.compareTargets);

                children.Add(childEntry);
                i++;
            }
            
            return children;
        }

        private static object[] Get(object value, IEnumerable enumerable)
        {
            return new object[] { value, enumerable };
        }

        private static object GetValueOrDefault(ISet<Tuple<object, object>> x, object value, IEnumerable enumerable)
        {
            var key = new Tuple<object, object>(value, enumerable);

            var v = x.Contains(key) ? value : null;

            /*/
            if (v is KeyValuePair<Type, MetaAddressables.GroupSchemaData> y)
            {
                if (y.Value is MetaAddressables.BundledAssetGroupSchemaData z)
                {
                    UnityEngine.Debug.Log($"B container: {ObjectComparer.Default.GetHashCode(enumerable)} {z}, BundleMode: {z.BundleMode}");
                }
            }
            //*/
            
            return v;
        }
    }
}
