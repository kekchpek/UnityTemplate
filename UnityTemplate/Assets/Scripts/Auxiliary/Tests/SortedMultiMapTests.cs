using System.Collections.Generic;
using System.Linq;
using kekchpek.Auxiliary.Collections;
using NUnit.Framework;

namespace Auxiliary.Tests
{
    public class SortedMultiMapTests
    {
        [Test]
        public void Add_SingleValue_ValueIsRetrievable()
        {
            var map = new SortedMultiMap<int, string>();
            
            map.Add(1, "one");
            
            Assert.AreEqual("one", map[1]);
        }

        [Test]
        public void Add_MultipleValuesForSameKey_AllValuesStored()
        {
            var map = new SortedMultiMap<int, string>();
            
            map.Add(1, "first");
            map.Add(1, "second");
            map.Add(1, "third");

            Assert.IsTrue(map.TryGetValues(1, out var values));
            var valueList = values.ToList();
            Assert.AreEqual(3, valueList.Count);
            Assert.AreEqual("first", valueList[0]);
            Assert.AreEqual("second", valueList[1]);
            Assert.AreEqual("third", valueList[2]);
        }

        [Test]
        public void Add_MultipleKeys_KeysAreSorted()
        {
            var map = new SortedMultiMap<int, string>();
            
            map.Add(5, "five");
            map.Add(1, "one");
            map.Add(3, "three");
            map.Add(2, "two");
            map.Add(4, "four");

            var keys = map.Keys.ToList();
            Assert.AreEqual(5, keys.Count);
            Assert.AreEqual(1, keys[0]);
            Assert.AreEqual(2, keys[1]);
            Assert.AreEqual(3, keys[2]);
            Assert.AreEqual(4, keys[3]);
            Assert.AreEqual(5, keys[4]);
        }

        [Test]
        public void First_MultipleKeys_ReturnsSmallestKey()
        {
            var map = new SortedMultiMap<int, string>();
            
            map.Add(5, "five");
            map.Add(1, "one");
            map.Add(3, "three");

            var first = map.First;
            Assert.AreEqual(1, first.Key);
            Assert.AreEqual("one", first.Value);
        }

        [Test]
        public void First_EmptyMap_ReturnsDefault()
        {
            var map = new SortedMultiMap<int, string>();

            var first = map.First;
            
            Assert.AreEqual(default(KeyValuePair<int, string>), first);
        }

        [Test]
        public void ContainsKey_ExistingKey_ReturnsTrue()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");

            Assert.IsTrue(map.ContainsKey(1));
        }

        [Test]
        public void ContainsKey_NonExistingKey_ReturnsFalse()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");

            Assert.IsFalse(map.ContainsKey(2));
        }

        [Test]
        public void TryGetValue_ExistingKey_ReturnsTrueAndFirstValue()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");

            var result = map.TryGetValue(1, out var value);
            
            Assert.IsTrue(result);
            Assert.AreEqual("first", value);
        }

        [Test]
        public void TryGetValue_NonExistingKey_ReturnsFalse()
        {
            var map = new SortedMultiMap<int, string>();

            var result = map.TryGetValue(1, out var value);
            
            Assert.IsFalse(result);
            Assert.AreEqual(default(string), value);
        }

        [Test]
        public void TryGetValues_ExistingKey_ReturnsTrueAndAllValues()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");

            var result = map.TryGetValues(1, out var values);
            
            Assert.IsTrue(result);
            var valueList = values.ToList();
            Assert.AreEqual(2, valueList.Count);
            Assert.AreEqual("first", valueList[0]);
            Assert.AreEqual("second", valueList[1]);
        }

        [Test]
        public void TryGetValues_NonExistingKey_ReturnsFalseAndEmptyEnumerable()
        {
            var map = new SortedMultiMap<int, string>();

            var result = map.TryGetValues(1, out var values);
            
            Assert.IsFalse(result);
            Assert.AreEqual(0, values.Count());
        }

        [Test]
        public void Remove_ByKey_RemovesAllValuesForKey()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");
            map.Add(2, "other");

            var result = map.Remove(1);
            
            Assert.IsTrue(result);
            Assert.IsFalse(map.ContainsKey(1));
            Assert.IsTrue(map.ContainsKey(2));
            Assert.AreEqual(1, map.CountKeys);
        }

        [Test]
        public void Remove_ByKeyAndValue_RemovesOnlySpecificValue()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");
            map.Add(1, "third");

            var result = map.Remove(1, "second");
            
            Assert.IsTrue(result);
            Assert.IsTrue(map.ContainsKey(1));
            Assert.IsTrue(map.TryGetValues(1, out var values));
            var valueList = values.ToList();
            Assert.AreEqual(2, valueList.Count);
            Assert.AreEqual("first", valueList[0]);
            Assert.AreEqual("third", valueList[1]);
        }

        [Test]
        public void Remove_LastValueForKey_RemovesKey()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "only");

            var result = map.Remove(1, "only");
            
            Assert.IsTrue(result);
            Assert.IsFalse(map.ContainsKey(1));
            Assert.AreEqual(0, map.CountKeys);
        }

        [Test]
        public void Remove_NonExistingKey_ReturnsFalse()
        {
            var map = new SortedMultiMap<int, string>();

            var result = map.Remove(1);
            
            Assert.IsFalse(result);
        }

        [Test]
        public void Remove_NonExistingValue_ReturnsFalse()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");

            var result = map.Remove(1, "two");
            
            Assert.IsFalse(result);
        }

        [Test]
        public void AddRange_AddsMultipleValues()
        {
            var map = new SortedMultiMap<int, string>();
            
            map.AddRange(1, new[] { "first", "second", "third" });

            Assert.IsTrue(map.TryGetValues(1, out var values));
            var valueList = values.ToList();
            Assert.AreEqual(3, valueList.Count);
            Assert.AreEqual("first", valueList[0]);
            Assert.AreEqual("second", valueList[1]);
            Assert.AreEqual("third", valueList[2]);
        }

        [Test]
        public void AddRange_ToExistingKey_AppendsValues()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "existing");
            
            map.AddRange(1, new[] { "new1", "new2" });

            Assert.IsTrue(map.TryGetValues(1, out var values));
            var valueList = values.ToList();
            Assert.AreEqual(3, valueList.Count);
            Assert.AreEqual("existing", valueList[0]);
            Assert.AreEqual("new1", valueList[1]);
            Assert.AreEqual("new2", valueList[2]);
        }

        [Test]
        public void Clear_RemovesAllEntries()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");
            map.Add(2, "two");
            map.Add(3, "three");

            map.Clear();

            Assert.AreEqual(0, map.Count);
            Assert.AreEqual(0, map.CountKeys);
            Assert.IsFalse(map.ContainsKey(1));
        }

        [Test]
        public void Count_ReturnsNumberOfTotalValues()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");
            map.Add(2, "other");

            Assert.AreEqual(3, map.Count);
        }

        [Test]
        public void CountKeys_ReturnsNumberOfUniqueKeys()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");
            map.Add(2, "other");

            Assert.AreEqual(2, map.CountKeys);
        }

        [Test]
        public void Contains_ExistingPair_ReturnsTrue()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");

            Assert.IsTrue(map.Contains(new KeyValuePair<int, string>(1, "one")));
        }

        [Test]
        public void Contains_NonExistingPair_ReturnsFalse()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");

            Assert.IsFalse(map.Contains(new KeyValuePair<int, string>(1, "two")));
            Assert.IsFalse(map.Contains(new KeyValuePair<int, string>(2, "one")));
        }

        [Test]
        public void Enumerator_IteratesInSortedOrder()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(3, "three-a");
            map.Add(1, "one");
            map.Add(3, "three-b");
            map.Add(2, "two");

            var pairs = new List<KeyValuePair<int, string>>();
            foreach (var pair in map)
            {
                pairs.Add(pair);
            }

            Assert.AreEqual(4, pairs.Count);
            Assert.AreEqual(1, pairs[0].Key);
            Assert.AreEqual("one", pairs[0].Value);
            Assert.AreEqual(2, pairs[1].Key);
            Assert.AreEqual("two", pairs[1].Value);
            Assert.AreEqual(3, pairs[2].Key);
            Assert.AreEqual("three-a", pairs[2].Value);
            Assert.AreEqual(3, pairs[3].Key);
            Assert.AreEqual("three-b", pairs[3].Value);
        }

        [Test]
        public void AsPairs_ReturnsAllPairsInSortedOrder()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(2, "two");
            map.Add(1, "one-a");
            map.Add(1, "one-b");

            var pairs = map.AsPairs().ToList();

            Assert.AreEqual(3, pairs.Count);
            Assert.AreEqual(1, pairs[0].Key);
            Assert.AreEqual("one-a", pairs[0].Value);
            Assert.AreEqual(1, pairs[1].Key);
            Assert.AreEqual("one-b", pairs[1].Value);
            Assert.AreEqual(2, pairs[2].Key);
            Assert.AreEqual("two", pairs[2].Value);
        }

        [Test]
        public void Values_ReturnsAllValuesInSortedKeyOrder()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(2, "two");
            map.Add(1, "one-a");
            map.Add(1, "one-b");

            var values = map.Values.ToList();

            Assert.AreEqual(3, values.Count);
            Assert.AreEqual("one-a", values[0]);
            Assert.AreEqual("one-b", values[1]);
            Assert.AreEqual("two", values[2]);
        }

        [Test]
        public void Indexer_Set_SingleValue_UpdatesValue()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "original");

            map[1] = "updated";

            Assert.AreEqual("updated", map[1]);
        }

        [Test]
        public void Indexer_Set_MultipleValues_ThrowsException()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "first");
            map.Add(1, "second");

            Assert.Throws<System.InvalidOperationException>(() => map[1] = "updated");
        }

        [Test]
        public void CustomComparer_SortsAccordingly()
        {
            var map = new SortedMultiMap<int, string>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            map.Add(1, "one");
            map.Add(3, "three");
            map.Add(2, "two");

            var keys = map.Keys.ToList();

            Assert.AreEqual(3, keys[0]);
            Assert.AreEqual(2, keys[1]);
            Assert.AreEqual(1, keys[2]);
        }

        [Test]
        public void CopyTo_CopiesAllPairs()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(2, "two");
            map.Add(1, "one");

            var array = new KeyValuePair<int, string>[2];
            map.CopyTo(array, 0);

            Assert.AreEqual(1, array[0].Key);
            Assert.AreEqual("one", array[0].Value);
            Assert.AreEqual(2, array[1].Key);
            Assert.AreEqual("two", array[1].Value);
        }

        [Test]
        public void LargeDataSet_MaintainsSortedOrder()
        {
            var map = new SortedMultiMap<int, int>();
            var random = new System.Random(42);
            var insertedKeys = new List<int>();

            for (int i = 0; i < 1000; i++)
            {
                int key = random.Next(0, 500);
                map.Add(key, i);
                insertedKeys.Add(key);
            }

            var keys = map.Keys.ToList();
            for (int i = 1; i < keys.Count; i++)
            {
                Assert.LessOrEqual(keys[i - 1], keys[i], "Keys should be in ascending order");
            }
        }

        [Test]
        public void LargeDataSet_RemoveOperations_MaintainsIntegrity()
        {
            var map = new SortedMultiMap<int, int>();

            for (int i = 0; i < 100; i++)
            {
                map.Add(i, i * 10);
            }

            for (int i = 0; i < 100; i += 2)
            {
                map.Remove(i);
            }

            Assert.AreEqual(50, map.CountKeys);
            
            var keys = map.Keys.ToList();
            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(i * 2 + 1, keys[i]);
            }
        }

        [Test]
        public void StressTest_MixedOperations()
        {
            var map = new SortedMultiMap<int, string>();

            map.Add(5, "five");
            map.Add(3, "three");
            map.Add(7, "seven");
            map.Add(1, "one");
            map.Add(9, "nine");
            map.Add(5, "five-again");

            Assert.AreEqual(1, map.First.Key);
            Assert.AreEqual(5, map.CountKeys);
            Assert.AreEqual(6, map.Count);

            map.Remove(3);
            Assert.AreEqual(4, map.CountKeys);
            Assert.IsFalse(map.ContainsKey(3));

            map.Remove(5, "five");
            Assert.IsTrue(map.ContainsKey(5));
            Assert.AreEqual("five-again", map[5]);

            map.Remove(5, "five-again");
            Assert.IsFalse(map.ContainsKey(5));

            Assert.AreEqual(1, map.First.Key);
            var keys = map.Keys.ToList();
            Assert.AreEqual(3, keys.Count);
            Assert.AreEqual(1, keys[0]);
            Assert.AreEqual(7, keys[1]);
            Assert.AreEqual(9, keys[2]);
        }

        [Test]
        public void NodesPool_InitialCapacity_Is128()
        {
            var map = new SortedMultiMap<int, string>();

            Assert.AreEqual(128, map.Capacity);
        }

        [Test]
        public void NodesPool_AddingUpToCapacity_DoesNotExpand()
        {
            var map = new SortedMultiMap<int, string>();
            var initialCapacity = map.Capacity;

            for (int i = 0; i < initialCapacity; i++)
            {
                map.Add(i, $"value{i}");
            }

            Assert.AreEqual(initialCapacity, map.Capacity);
            Assert.AreEqual(initialCapacity, map.CountKeys);
        }

        [Test]
        public void NodesPool_ExceedingCapacity_ExpandsPool()
        {
            var map = new SortedMultiMap<int, string>();
            var initialCapacity = map.Capacity;

            for (int i = 0; i <= initialCapacity; i++)
            {
                map.Add(i, $"value{i}");
            }

            Assert.Greater(map.Capacity, initialCapacity);
            Assert.AreEqual(initialCapacity + 1, map.CountKeys);
        }

        [Test]
        public void NodesPool_AddingMultipleValuesToSameKey_DoesNotExpandPool()
        {
            var map = new SortedMultiMap<int, string>();
            var initialCapacity = map.Capacity;

            map.Add(1, "first");
            for (int i = 0; i < 1000; i++)
            {
                map.Add(1, $"value{i}");
            }

            Assert.AreEqual(initialCapacity, map.Capacity);
            Assert.AreEqual(1, map.CountKeys);
        }

        [Test]
        public void NodesPool_RemoveAndReAdd_ReusesPoolNodes()
        {
            var map = new SortedMultiMap<int, string>();
            var initialCapacity = map.Capacity;

            for (int i = 0; i < initialCapacity; i++)
            {
                map.Add(i, $"value{i}");
            }

            for (int i = 0; i < initialCapacity / 2; i++)
            {
                map.Remove(i);
            }

            for (int i = initialCapacity; i < initialCapacity + initialCapacity / 2; i++)
            {
                map.Add(i, $"value{i}");
            }

            Assert.AreEqual(initialCapacity, map.Capacity);
            Assert.AreEqual(initialCapacity, map.CountKeys);
        }

        [Test]
        public void First_CachedCorrectly_AfterInsertions()
        {
            var map = new SortedMultiMap<int, string>();

            map.Add(5, "five");
            Assert.AreEqual(5, map.First.Key);

            map.Add(3, "three");
            Assert.AreEqual(3, map.First.Key);

            map.Add(7, "seven");
            Assert.AreEqual(3, map.First.Key);

            map.Add(1, "one");
            Assert.AreEqual(1, map.First.Key);

            map.Add(2, "two");
            Assert.AreEqual(1, map.First.Key);
        }

        [Test]
        public void First_CachedCorrectly_AfterRemovingFirstElement()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");
            map.Add(3, "three");
            map.Add(5, "five");

            Assert.AreEqual(1, map.First.Key);

            map.Remove(1);
            Assert.AreEqual(3, map.First.Key);

            map.Remove(3);
            Assert.AreEqual(5, map.First.Key);

            map.Remove(5);
            Assert.AreEqual(default(KeyValuePair<int, string>), map.First);
        }

        [Test]
        public void First_CachedCorrectly_AfterRemovingNonFirstElement()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");
            map.Add(3, "three");
            map.Add(5, "five");

            map.Remove(3);
            Assert.AreEqual(1, map.First.Key);

            map.Remove(5);
            Assert.AreEqual(1, map.First.Key);
        }

        [Test]
        public void First_CachedCorrectly_AfterClear()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one");
            map.Add(2, "two");

            map.Clear();
            Assert.AreEqual(default(KeyValuePair<int, string>), map.First);

            map.Add(5, "five");
            Assert.AreEqual(5, map.First.Key);
        }

        [Test]
        public void First_CachedCorrectly_WithRemoveKeyValue()
        {
            var map = new SortedMultiMap<int, string>();
            map.Add(1, "one-a");
            map.Add(1, "one-b");
            map.Add(3, "three");

            map.Remove(1, "one-a");
            Assert.AreEqual(1, map.First.Key);
            Assert.AreEqual("one-b", map.First.Value);

            map.Remove(1, "one-b");
            Assert.AreEqual(3, map.First.Key);
        }
    }
}
