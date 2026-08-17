using System;
using kekchpek.Auxiliary.Collections;
using NUnit.Framework;

namespace Auxiliary.Tests
{
    public class HyperListTests
    {
        [Test]
        public void DefaultConstructor_IsEmpty()
        {
            var list = new HyperList<int>();

            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(0, list.Buffer.Length);
        }

        [Test]
        public void Constructor_WithPositiveCapacity_HasBufferOfAtLeastThatSize()
        {
            var list = new HyperList<int>(16);

            Assert.AreEqual(0, list.Count);
            Assert.GreaterOrEqual(list.Buffer.Length, 16);
        }

        [Test]
        public void Constructor_WithInitialBuffer_UsesLengthAsCount()
        {
            var backing = new[] { 10, 20, 30 };
            var list = new HyperList<int>(backing);

            Assert.AreEqual(3, list.Count);
            Assert.AreSame(backing, list.Buffer);
            Assert.AreEqual(10, list[0]);
            Assert.AreEqual(20, list[1]);
            Assert.AreEqual(30, list[2]);
        }

        [Test]
        public void Add_IncrementsCountAndStoresValues()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void Add_ManyTimes_GrowsInternalBuffer()
        {
            var list = new HyperList<int>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }

            Assert.AreEqual(100, list.Count);
            Assert.GreaterOrEqual(list.Buffer.Length, 100);
            Assert.AreEqual(99, list[99]);
        }

        [Test]
        public void AddRange_AppendsSpanContents()
        {
            var list = new HyperList<int>();
            list.Add(0);
            var extra = new[] { 1, 2, 3 };
            list.AddRange(extra);

            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(0, list[0]);
            Assert.AreEqual(1, list[1]);
            Assert.AreEqual(2, list[2]);
            Assert.AreEqual(3, list[3]);
        }

        [Test]
        public void AddRange_EmptySpan_LeavesListUnchanged()
        {
            var list = new HyperList<int>();
            list.Add(5);
            ReadOnlySpan<int> empty = stackalloc int[0];
            list.AddRange(empty);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(5, list[0]);
        }

        [Test]
        public void Indexer_Get_OutOfRange_Throws()
        {
            var list = new HyperList<int>();
            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[1]);
        }

        [Test]
        public void Indexer_Set_OutOfRange_Throws()
        {
            var list = new HyperList<int>();
            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[1] = 0);
        }

        [Test]
        public void Indexer_Set_UpdatesElement()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list[0] = 99;

            Assert.AreEqual(99, list[0]);
            Assert.AreEqual(2, list[1]);
        }

        [Test]
        public void GetRef_AllowsInPlaceMutation()
        {
            var list = new HyperList<int>();
            list.Add(1);
            ref var r = ref list.GetRef(0);
            r = 42;

            Assert.AreEqual(42, list[0]);
        }

        [Test]
        public void GetRef_OutOfRange_Throws()
        {
            var list = new HyperList<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => list.GetRef(0));
        }

        [Test]
        public void Clear_ResetsCount()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Clear();

            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void ClearWithoutReleasingGcReferences_ResetsCount()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.ClearWithoutReleasingGcReferences();

            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void RemoveAt_LastElement_DecrementsCount()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.RemoveAt(1);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void RemoveAt_MiddleElement_ShiftsTail()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveAt(1);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(3, list[1]);
        }

        [Test]
        public void RemoveAt_OutOfRange_Throws()
        {
            var list = new HyperList<int>();
            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(1));
        }

        [Test]
        public void Insert_AtEnd_Appends()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Insert(1, 2);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
        }

        [Test]
        public void Insert_AtStart_ShiftsExisting()
        {
            var list = new HyperList<int>();
            list.Add(2);
            list.Insert(0, 1);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
        }

        [Test]
        public void Insert_OutOfRange_Throws()
        {
            var list = new HyperList<int>();
            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(2, 0));
        }

        [Test]
        public void InsertRange_AtStart_PrependsSpan()
        {
            var list = new HyperList<int>();
            list.Add(3);
            list.InsertRange(0, new[] { 1, 2 });

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void InsertRange_AtEnd_AppendsSpan()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.InsertRange(1, new[] { 2, 3 });

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void InsertRange_InMiddleOfList_SplitsPrefixAndSuffix()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);
            list.InsertRange(2, new[] { 20, 21 });

            Assert.AreEqual(7, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(20, list[2]);
            Assert.AreEqual(21, list[3]);
            Assert.AreEqual(3, list[4]);
            Assert.AreEqual(4, list[5]);
            Assert.AreEqual(5, list[6]);
        }

        [Test]
        public void InsertRange_AtEveryIndexFromZeroToCount_PreservesFullSequence()
        {
            var baseItems = new[] { 0, 1, 2, 3 };
            var inserted = new[] { 100, 101 };
            for (int insertIndex = 0; insertIndex <= baseItems.Length; insertIndex++)
            {
                var list = new HyperList<int>();
                for (int b = 0; b < baseItems.Length; b++)
                {
                    list.Add(baseItems[b]);
                }

                list.InsertRange(insertIndex, inserted);

                Assert.AreEqual(baseItems.Length + inserted.Length, list.Count, "wrong count at insertIndex " + insertIndex);
                for (int i = 0; i < insertIndex; i++)
                {
                    Assert.AreEqual(baseItems[i], list[i], "prefix at insertIndex " + insertIndex);
                }

                for (int j = 0; j < inserted.Length; j++)
                {
                    Assert.AreEqual(inserted[j], list[insertIndex + j], "inserted block at insertIndex " + insertIndex);
                }

                for (int k = 0; k < baseItems.Length - insertIndex; k++)
                {
                    Assert.AreEqual(
                        baseItems[insertIndex + k],
                        list[insertIndex + inserted.Length + k],
                        "suffix at insertIndex " + insertIndex);
                }
            }
        }

        [Test]
        public void InsertRange_OutOfRange_Throws()
        {
            var list = new HyperList<int>();
            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(2, new[] { 0 }));
        }

        [Test]
        public void RemoveRange_Interior_RemovesSliceAndShifts()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.RemoveRange(1, 2);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(4, list[1]);
        }

        [Test]
        public void RemoveRange_WhenRangeExtendsPastEnd_TrimsFromIndex()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveRange(1, 100);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void RemoveRangeWithoutReleasingGcReferences_WhenRangeExtendsPastEnd_TrimsFromIndex()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveRangeWithoutReleasingGcReferences(1, 100);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void PopBack_ReturnsLastAndRemovesIt()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            var last = list.PopBack();

            Assert.AreEqual(2, last);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void PopBack_WhenEmpty_Throws()
        {
            var list = new HyperList<int>();

            Assert.Throws<InvalidOperationException>(() => list.PopBack());
        }

        [Test]
        public void IndexOf_FindsFirstMatch()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(2);

            Assert.AreEqual(1, list.IndexOf(2));
        }

        [Test]
        public void IndexOf_NoMatch_ReturnsMinusOne()
        {
            var list = new HyperList<int>();
            list.Add(1);

            Assert.AreEqual(-1, list.IndexOf(99));
        }

        [Test]
        public void IndexOf_WithCustomComparer_UsesComparer()
        {
            var list = new HyperList<string>();
            list.Add("a");
            list.Add("B");
            var comparer = StringComparer.OrdinalIgnoreCase;

            Assert.AreEqual(1, list.IndexOf("b", comparer));
        }

        [Test]
        public void Contains_ReturnsTrueWhenPresent()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);

            Assert.IsTrue(list.Contains(2));
            Assert.IsFalse(list.Contains(3));
        }

        [Test]
        public void TakeCopy_ReplacesLogicalContentsFromSpan()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.TakeCopyFrom(new[] { 9, 8, 7 });

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(9, list[0]);
            Assert.AreEqual(8, list[1]);
            Assert.AreEqual(7, list[2]);
        }

        [Test]
        public void TakeCopy_ShorterThanExisting_ReusesBufferIfLargeEnough()
        {
            var list = new HyperList<int>(8);
            for (int i = 0; i < 5; i++)
            {
                list.Add(i);
            }

            var bufferBefore = list.Buffer;
            list.TakeCopyFrom(new[] { 1, 2 });

            Assert.AreEqual(2, list.Count);
            Assert.AreSame(bufferBefore, list.Buffer);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
        }

        [Test]
        public void TrimEnd_ReducesCountWhenGreaterThanClearFrom()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.TrimEnd(1);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void TrimEnd_WhenClearFromNotLessThanCount_DoesNothing()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.TrimEnd(5);

            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void TrimEndWithoutReleasingGcReferences_ReducesCount()
        {
            var list = new HyperList<int>();
            list.Add(1);
            list.Add(2);
            list.TrimEndWithoutReleasingGcReferences(1);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void GetSpan_AndGetReadOnlySpan_ExposeActiveRange()
        {
            var list = new HyperList<int>();
            list.Add(10);
            list.Add(20);

            var span = list.GetSpan();
            var ro = list.GetReadOnlySpan();

            Assert.AreEqual(2, span.Length);
            Assert.AreEqual(2, ro.Length);
            Assert.AreEqual(10, span[0]);
            Assert.AreEqual(20, ro[1]);
        }
    }
}
