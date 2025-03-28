using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace USP.AddressablesAssetProcessing
{
    using UnityEditor.AddressableAssets.Settings;
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;
    using IData = IDictionary<string, USP.MetaAddressables.MetaAddressables.UserData>;
    using IReadOnlyData = IReadOnlyDictionary<string, USP.MetaAddressables.MetaAddressables.UserData>;
#endif

    public class X
    {
        public readonly IAssetApplicator AssetApplicator;

        public readonly AddressableAssetSettings Settings;

        public readonly string AssetFilePath;

        public X(IAssetApplicator assetApplicator, AddressableAssetSettings settings, string assetFilePath)
        {
            this.AssetApplicator = assetApplicator;
            this.Settings = settings;
            this.AssetFilePath = assetFilePath;
        }

        public void Reapply()
        {
            bool found = AssetApplicator.AssetStore.DataByAssetPath.TryGetValue(AssetFilePath, out MetaAddressables.UserData userData);

            if (!found)
            {
                throw new Exception("This should not be happening, ever.");
            }

            AssetApplicator.ApplyAsset(Settings, userData);
        }
    }

    public class CompareOperand
    {
        #region Fields
        public object parentValue;

        public Func<object, object> getter;

        public Action<object, object> setter;

        public X x;
        #endregion

        #region Properties
        public bool IsReadOnly => setter == null;

        public object Value
        {
            get
            {
                if (this.parentValue == null)
                {
                    return null;
                }

                return getter?.Invoke(this.parentValue);
            }
            set
            {
                if (this.parentValue == null)
                {
                    return;
                }

                setter?.Invoke(this.parentValue, value);

                x?.Reapply();
            }
        }
        #endregion

        #region Methods
        public CompareOperand(X x, object parentValue, Func<object, object> getter, Action<object, object> setter = null)
        {
            this.x = x;
            this.parentValue = parentValue;
            this.getter = getter;
            this.setter = setter;
        }
        #endregion
    }

    public struct CompareOperation
    {
        public CompareOperand leftHand { get; }

        public CompareOperand rightHand { get; }

        public bool result { get; }

        public CompareOperation(IPropertyComparer comparer, CompareOperand leftHand, CompareOperand rightHand) :
            this(leftHand, rightHand, Operate(comparer, leftHand, rightHand))
        {
        }

        public CompareOperation(CompareOperand leftHand, CompareOperand rightHand, bool result)
        {
            this.leftHand = leftHand;
            this.rightHand = rightHand;
            this.result = result;
        }

        public static bool Operate(IPropertyComparer comparer, CompareOperand leftHand, CompareOperand rightHand)
        {
            var leftHash = leftHand.Value != null ? comparer.GetHashCode(leftHand.Value) : 0;
            var rightHash = rightHand.Value != null ? comparer.GetHashCode(rightHand.Value) : 0;

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
        #region Constants
        private const string ProcessingDataKey = "processing-data";

        private const string MetafileDataKey = "metafile-data";

        private const string AddressablesDataKey = "addressables-data";
        
        private static UserDataComparer userDataComparer = new UserDataComparer();
        #endregion

        #region Methods
        #region Create All Entries Parent
        public static IEnumerable<ComparisonEntry> CreateEntry(IEnumerable<ComparisonEntry> comparisonEntries)
        {
            var result = new ComparisonEntry();
            result.entryName = "All Entries";
            result.entryType = typeof(IEnumerable<ComparisonEntry>);
            result.compareTargets = PopulateTargets(comparisonEntries);
            result.compareOperations = PopulateCompare(comparisonEntries);
            result.children = comparisonEntries;

            return new[] { result };
        }

        private static IReadOnlyDictionary<string, CompareOperand> PopulateTargets(IEnumerable<ComparisonEntry> comparisonEntries)
        {
            var result = new Dictionary<string, CompareOperand>();

            // For every comparison entry passed in, perform the following:
            foreach (ComparisonEntry comparisonEntry in comparisonEntries)
            {
                // For every compare operation, perform the following:
                foreach ((string key, CompareOperand operation) in comparisonEntry.compareTargets)
                {
                    result[key] = null;
                }
            }

            return result;
        }

        private static IReadOnlyDictionary<string, CompareOperation> PopulateCompare(IEnumerable<ComparisonEntry> comparisonEntries)
        {
            var operationDataByComparsons = new Dictionary<string, (bool, (bool, List<CompareOperand>), (bool, List<CompareOperand>))>();

            // For every comparison entry passed in, perform the following:
            foreach (ComparisonEntry comparisonEntry in comparisonEntries)
            {
                // For every compare operation, perform the following:
                foreach ((string key, CompareOperation operation) in comparisonEntry.compareOperations)
                {
                    if (!operationDataByComparsons.TryGetValue(key, out (bool isSame, (bool isReadOnly, List<CompareOperand> operands) leftHand, (bool isReadOnly, List<CompareOperand> operands) rightHand) value))
                    {
                        value = (true, (operation.leftHand.IsReadOnly, new List<CompareOperand>()), (operation.rightHand.IsReadOnly, new List<CompareOperand>()));
                        operationDataByComparsons.Add(key, value);
                    }

                    if (value.isSame)
                    {
                        value.isSame &= operation.result;
                    }

                    if (operation.result == false)
                    {
                        value.leftHand.operands.Add(operation.leftHand);
                        value.rightHand.operands.Add(operation.rightHand);
                    }

                    operationDataByComparsons[key] = value;
                }
            }

            var result = new Dictionary<string, CompareOperation>(operationDataByComparsons.Count);
            foreach ((string key, (bool isSame, (bool isReadOnly, List<CompareOperand> operands) leftHand, (bool isReadOnly, List<CompareOperand> operands) rightHand)) in operationDataByComparsons)
            {
                var lhs = new CompareOperand(null, leftHand.operands, Get, leftHand.isReadOnly ? null : Set);
                var rhs = new CompareOperand(null, rightHand.operands, Get, rightHand.isReadOnly ? null : Set);
                result[key] = new CompareOperation(lhs, rhs, isSame);
            }

            return result;
        }

        private static object Get(object target)
        {
            return target;
        }

        private static void Set(object destination, object source)
        {
            Set(source as List<CompareOperand>, destination as List<CompareOperand>);
        }

        private static void Set(List<CompareOperand> source, List<CompareOperand> destination)
        {
            if (source.Count != destination.Count)
            {
                return;
            }

            var d = destination.GetEnumerator();
            var s = source.GetEnumerator();

            while (d.MoveNext() && s.MoveNext())
            {
                d.Current.Value = s.Current.Value;
            }
        }
        #endregion

        #region Create UserData Entry
        public static ComparisonEntry CreateEntry(AddressableAssetSettings settings, CombinedAssetApplicator combinedAssetApplicator, string assetFilePath)
        {
            var properties = new Dictionary<string, CompareOperand>(3);
            
            properties[ProcessingDataKey] = CreateTarget(combinedAssetApplicator.AssetStore, combinedAssetApplicator.SimulatedAssetApplicator, settings, assetFilePath);
            properties[MetafileDataKey] = CreateTarget(combinedAssetApplicator.MetaAddressablesAssetStore, combinedAssetApplicator.FileProcessingToMetaFileApplicator, settings, assetFilePath);
            properties[AddressablesDataKey] = CreateTarget(combinedAssetApplicator.AddressablesAssetStore, combinedAssetApplicator.MetaFileToAddressablesApplicator, settings, assetFilePath);

            var result = new ComparisonEntry();
            result.comparer = userDataComparer;
            result.entryType = typeof(MetaAddressables.UserData);
            result.entryName = assetFilePath;
            result.compareTargets = properties;
            result.compareOperations = PopulateCompare(result.comparer, result.compareTargets);
            result.children = PopulateChildren(result.entryType, result.comparer, result.compareTargets);

            return result;
        }

        private static CompareOperand CreateTarget(IAssetStore assetStore, IAssetApplicator assetApplicator, AddressableAssetSettings settings, string assetFilePath)
        {
            if (assetStore.DataByAssetPath is not IData dataByAssetPath)
            {
                return new CompareOperand(null, null, null);
            }

            var x = new X(assetApplicator, settings, assetFilePath);

            Func<object, object> getter = target => Get(target as IReadOnlyData, assetFilePath);

            Action<object, object> setter = assetStore.IsReadOnly ? null :
                (target, value) => Set(target as IData, assetFilePath, value as MetaAddressables.UserData);

            return new CompareOperand(x, dataByAssetPath, getter, setter);
        }

        private static MetaAddressables.UserData Get(IReadOnlyData dataByAssetPath, string assetFilePath)
        {
            dataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData userData);

            return userData;
        }

        private static void Set(IData dataByAssetPath, string assetFilePath, MetaAddressables.UserData userData)
        {
            dataByAssetPath[assetFilePath] = userData;
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
                        if (parentOperand.Value is not IEnumerable enumerable)
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
                var propertyInfo = comparerChild.GetMemberInfo<PropertyInfo>();
                Func<object, object> propertyGetter = null;
                if (propertyInfo != null)
                {
                    propertyGetter = propertyInfo.GetValue;
                }
                else
                {
                    Delegate getter = comparerChild.Item1.Compile();
                    propertyGetter = target => getter.DynamicInvoke(target); 
                }

                Action<object, object> propertySetter = propertyInfo != null ? propertyInfo.SetValue : null;

                var properties = new Dictionary<string, CompareOperand>(compareTargets.Count);
                foreach ((string key, CompareOperand parentOperand) in compareTargets)
                {
                    if (parentOperand == null)
                    {
                        continue;
                    }

                    Action<object, object> targetPropertySetter = !parentOperand.IsReadOnly ? propertySetter : null;

                    var childOperand = new CompareOperand(parentOperand.x, parentOperand.Value, propertyGetter, targetPropertySetter);
                    properties.Add(key, childOperand);
                }

                // The first item should exist along with at least one other to compare against.
                // It should be available to compare against all other items, which are alike enough to compare.
                // Therefore, there is at least one and it would be reasonable have the same type.
                CompareOperand firstOperand = properties.First(pair => pair.Value.Value != null).Value;
                object firstValue = firstOperand.Value;                

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
        #endregion
        #endregion
    }
}
