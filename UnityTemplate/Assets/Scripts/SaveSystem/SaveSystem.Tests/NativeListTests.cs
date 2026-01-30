using System;
using NUnit.Framework;
using kekchpek.SaveSystem.Utils;

namespace kekchpek.SaveSystem.Tests
{

    [TestFixture]
    public class NativeListTests
    {


        private struct TestStruct
        {
            public int value;
            public float floatValue;
            public double doubleValue;
            public char charValue;
            public bool boolValue;
            public DateTime dateTimeValue;
            public TimeSpan timeSpanValue;
            
            public static TestStruct CreateRandom() {
                var random = new System.Random();
                return new TestStruct {
                    value = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                    floatValue = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
                    doubleValue = random.NextDouble(),
                    charValue = (char)random.Next(0, char.MaxValue),
                    boolValue = UnityEngine.Random.Range(0, 2) == 1,
                    dateTimeValue = DateTime.FromOADate(random.NextDouble()),
                    timeSpanValue = TimeSpan.FromSeconds(random.NextDouble()),
                };
            }

            public static bool operator ==(TestStruct a, TestStruct b) {
                return a.value == b.value &&
                    a.floatValue == b.floatValue &&
                    a.doubleValue == b.doubleValue &&
                    a.charValue == b.charValue &&
                    a.boolValue == b.boolValue &&
                    a.dateTimeValue == b.dateTimeValue &&
                    a.timeSpanValue == b.timeSpanValue;
            }

            public static bool operator !=(TestStruct a, TestStruct b) {
                return !(a == b);
            }

            public override bool Equals(object obj) {
                if (obj is TestStruct other) {
                    return this == other;
                }
                return false;
            }

            public override int GetHashCode() {
                return 
                value.GetHashCode() ^ 
                floatValue.GetHashCode() ^ 
                doubleValue.GetHashCode() ^ 
                charValue.GetHashCode() ^ 
                boolValue.GetHashCode() ^ 
                dateTimeValue.GetHashCode() ^ 
                timeSpanValue.GetHashCode();
            }

            public override string ToString() {
                return $"value: {value}, floatValue: {floatValue}, doubleValue: {doubleValue}, charValue: {charValue}, boolValue: {boolValue}, dateTimeValue: {dateTimeValue}, timeSpanValue: {timeSpanValue}";
            }

        }


        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public unsafe void TestSingleElement(int value)
        {
            var list = new NativeList(4);
            int a = value;
            list.Add(&a);
            int b = *(int*)list.Get(0);
            Assert.AreEqual(a, b);
        }

        [Test]
        public unsafe void TestSingleStructElement()
        {
            var list = new NativeList(sizeof(TestStruct));
            var a = TestStruct.CreateRandom();
            list.Add(&a);
            var b = *(TestStruct*)list.Get(0);
            Assert.AreEqual(a, b);
        }

        [Test]
        public unsafe void TestMultipleElements(
            [Values(1, -1, 0, int.MaxValue, int.MinValue)] int value, 
            [Values(1, 2, 3, 10, 50, 100, 300, 1000)] int count)
        {
            var list = new NativeList(4);
            for (int i = 0; i < count; i++)
            {
                int a = value;
                list.Add(&a);
            }
            for (int i = 0; i < count; i++)
            {
                int b = *(int*)list.Get(i);
                Assert.AreEqual(value, b);
            }
        }

        [Test]
        public unsafe void TestMultipleStructElements([Values(1, 2, 3, 10, 50, 100, 300, 1000)] int count)
        {
            var list = new NativeList(sizeof(TestStruct));
            var a = new TestStruct[count];
            for (int i = 0; i < count; i++)
            {
                var ai = TestStruct.CreateRandom();
                a[i] = ai;
                list.Add(&ai);
            }
            for (int i = 0; i < count; i++)
            {
                var b = *(TestStruct*)list.Get(i);
                Assert.AreEqual(a[i], b);
            }
        }
    }


}
