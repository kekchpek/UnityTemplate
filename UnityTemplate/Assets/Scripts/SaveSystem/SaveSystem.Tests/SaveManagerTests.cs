using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.Data;
using NUnit.Framework;
using UnityEngine;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.SaveManagers;

namespace kekchpek.SaveSystem.Tests
{
    public class SaveManagerTests
    {
        /// <summary>Matches <see cref="FileSaveManager"/> fallback file naming.</summary>
        private const string FallbackSaveSuffix = "___fallbacksave";

        #region Test Helpers

        private sealed class VersionedTestPayload
        {
            public int Value;
        }

        private sealed class VersionedTestPayloadCodec : ICustomCodec<VersionedTestPayload>
        {
            private const byte Version1Marker = 0x7D;

            public void Serialize(ISaveStream stream, object value, int? saveVersionForCustomCodecs)
            {
                var payload = (VersionedTestPayload)value;
                var ver = saveVersionForCustomCodecs ?? 0;
                if (ver >= 1)
                {
                    stream.SaveStruct(Version1Marker);
                }

                stream.SaveStruct(payload.Value);
            }

            public VersionedTestPayload Deserialize(ILoadStream stream, int? saveVersionForCustomCodecs)
            {
                var ver = saveVersionForCustomCodecs ?? 0;
                if (ver >= 1)
                {
                    var marker = stream.LoadStruct<byte>();
                    if (marker != Version1Marker)
                    {
                        throw new InvalidDataException(
                            $"VersionedTestPayload v{ver}: expected marker 0x{Version1Marker:X2}, got 0x{marker:X2}.");
                    }
                }

                return new VersionedTestPayload { Value = stream.LoadStruct<int>() };
            }
        }

        private class StreamSaveManager : SaveSystem.StreamSaveManager
        {
            private readonly Stream _stream;
            private Stream _fallbackStream;

            public StreamSaveManager(
                Stream stream,
                Stream fallbackStream = null,
                int currentCustomCodecsVersion = 0) : base(currentCustomCodecsVersion)
            {
                _stream = stream;
                _fallbackStream = fallbackStream;
            }

            protected override Stream GetStreamToWrite(string saveId)
            {
                if (_stream.CanSeek && _stream.CanWrite)
                {
                    _stream.Position = 0;
                }

                return _stream;
            }

            protected override bool TryGetStreamToRead(string saveId, out Stream s)
            {
                s = _stream;
                if (s != null && s.CanSeek)
                {
                    s.Position = 0;
                }

                return true;
            }

            public override string[] GetSaves()
            {
                // Do nothing
                return Array.Empty<string>();
            }

            public override void RemoveSave(string saveId)
            {
                // Do nothing
            }

            protected override void ReleaseStream(Stream s)
            {
                // Do nothing because stream is reused during tests.
            }

            protected override void FallbackSave(string saveId)
            {
                _fallbackStream = _stream;
            }

            protected override bool TryGetFallbackStreamToRead(string saveId, out Stream stream)
            {
                stream = _fallbackStream;
                if (stream != null && stream.CanSeek)
                {
                    stream.Position = 0;
                }

                return stream != null;
            }
        }

        private sealed class VersionedPayloadSaveManager : StreamSaveManager
        {
            public VersionedPayloadSaveManager(MemoryStream stream, int customCodecVersion, MemoryStream fallback = null)
                : base(stream, fallback, customCodecVersion)
            {
                RegisterCustomCodec(new VersionedTestPayloadCodec());
            }
        }

        private sealed class CorruptPrimaryFallbackSaveManager : SaveSystem.StreamSaveManager
        {
            private readonly MemoryStream _primaryRead;
            private readonly MemoryStream _fallbackRead;

            public CorruptPrimaryFallbackSaveManager(
                MemoryStream primaryRead, MemoryStream fallbackRead, int currentCustomCodecsVersion = 0)
                : base(currentCustomCodecsVersion)
            {
                _primaryRead = primaryRead;
                _fallbackRead = fallbackRead;
            }

            protected override Stream GetStreamToWrite(string saveId) => new MemoryStream();

            protected override bool TryGetStreamToRead(string saveId, out Stream stream)
            {
                stream = _primaryRead;
                if (stream != null)
                {
                    stream.Position = 0;
                }

                return stream != null && stream.Length > 0;
            }

            protected override bool TryGetFallbackStreamToRead(string saveId, out Stream stream)
            {
                if (_fallbackRead == null)
                {
                    stream = null;
                    return false;
                }

                stream = _fallbackRead;
                stream.Position = 0;
                return stream.Length > 0;
            }

            public override string[] GetSaves() => Array.Empty<string>();

            public override void RemoveSave(string saveId)
            {
            }

            protected override void ReleaseStream(Stream s) => s.Dispose();

            protected override void FallbackSave(string saveId)
            {
            }
        }

        private class TestSavableObject : ISaveObject
        {

            public int _value1;
            public float _value2;
            public DateTime _value3;
            public TestSavableObject _value4;
            public string _value5;

            public event Action Changed;

            public void Deserialize(ILoadStream loadStream, int? customCodecVersion)
            {
                _value2 = loadStream.LoadStruct<float>();
                _value3 = loadStream.LoadStruct<DateTime>();
                if (loadStream.LoadStruct<bool>())
                {
                    _value4 = loadStream.LoadSavable<TestSavableObject>();
                }
                _value1 = loadStream.LoadStruct<int>();
                if (loadStream.LoadStruct<bool>())
                {
                    _value5 = loadStream.LoadCustom<string>();
                }
            }

            public void Serialize(ISaveStream saveStream, int? customCodecVersion)
            {
                saveStream.SaveStruct(_value2);
                saveStream.SaveStruct(_value3);
                saveStream.SaveStruct(_value4 != null);
                if (_value4 != null)
                {
                    _value4.Serialize(saveStream, customCodecVersion);
                }
                saveStream.SaveStruct(_value1);
                saveStream.SaveStruct(_value5 != null);
                if (_value5 != null)
                {
                    saveStream.SaveCustom(_value5, customCodecVersion);
                }
            }
        }

        #endregion

        /// <summary>
        /// Copies buffer into a new <see cref="MemoryStream"/> that can grow.
        /// <see cref="MemoryStream(byte[])"/> is fixed-length and throws when v1+ saves append the integrity hash past the previous file size.
        /// </summary>
        private static MemoryStream ExpandableCopy(MemoryStream source)
        {
            var bytes = source.ToArray();
            var copy = new MemoryStream();
            if (bytes.Length > 0)
            {
                copy.Write(bytes, 0, bytes.Length);
            }

            copy.Position = 0;
            return copy;
        }

        /// <summary>
        /// <paramref name="saveVersion"/> — format version used when writing the save.
        /// <paramref name="loadVersion"/> — explicit save-system version on the load manager before LoadOrCreate (payload decode still follows version bytes in the file).
        /// </summary>
        private static void SaveThenLoad(
            Action<StreamSaveManager> savePhase,
            Action<StreamSaveManager> loadPhase,
            int saveVersion,
            int loadVersion)
        {
            var stream = new MemoryStream();

            var saveManager = new StreamSaveManager(stream);
            saveManager.UseExplicitSaveSystemVersion(saveVersion);

            saveManager.LoadOrCreate("TestSave");

            savePhase(saveManager);
            saveManager.SaveExplicitly();

            var streamCopy = ExpandableCopy(stream);
            var loadManager = new StreamSaveManager(streamCopy);
            loadManager.UseExplicitSaveSystemVersion(loadVersion);

            loadManager.LoadOrCreate("TestSave");
            loadPhase(loadManager);
        }

        /// <summary>Common pairs for regression: default (1,1) and legacy file saved as v0 then opened with manager preferring v1.</summary>
        private static IEnumerable<TestCaseData> SaveLoadVersionPairsDefaultAndV0SaveV1Load()
        {
            yield return new TestCaseData(1, 1);
            yield return new TestCaseData(0, 1);
        }

        private static IEnumerable<TestCaseData> SingleIntExpectedValueWithSaveLoadVersionPairs()
        {
            int[] expectedValues = { 1, 0, -1, int.MaxValue, int.MinValue };
            foreach (int expectedValue in expectedValues)
            {
                yield return new TestCaseData(expectedValue, 1, 1);
                yield return new TestCaseData(expectedValue, 0, 1);
            }
        }

        [Test]
        [TestCaseSource(nameof(SingleIntExpectedValueWithSaveLoadVersionPairs))]
        public void TestSingleInt(int expectedValue, int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager => saveManager.DeserializeAndCaptureStructValue("TestInterger", expectedValue),
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureStructValue("TestInterger", 11);
                    Assert.AreEqual(expectedValue, value.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SingleIntExpectedValueWithSaveLoadVersionPairs))]
        public void TestSingleIntChange(int expectedValue, int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureStructValue("TestInterger", 0);
                    value.Value = expectedValue;
                },
                loadManager =>
                {
                    var loadedValue = loadManager.DeserializeAndCaptureStructValue("TestInterger", 11);
                    Assert.AreEqual(expectedValue, loadedValue.Value);
                },
                saveVersion,
                loadVersion);
        }
        
        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestMultipleStructValues(int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager =>
                {
                    // Capture and modify several different struct values
                    var int1 = saveManager.DeserializeAndCaptureStructValue("Int1", 0);
                    int1.Value = 100;

                    var int2 = saveManager.DeserializeAndCaptureStructValue("Int2", 0);
                    int2.Value = -50;

                    // For float we rely on default value assignment
                    saveManager.DeserializeAndCaptureStructValue("Float1", 123.456f);

                    var boolVal = saveManager.DeserializeAndCaptureStructValue("Bool1", false);
                    boolVal.Value = true;

                    var vectorVal = saveManager.DeserializeAndCaptureStructValue("Vector3", new Vector3(1, 2, 3));
                    vectorVal.Value = new Vector3(4, 5, 6);
                },
                loadManager =>
                {
                    var int1L = loadManager.DeserializeAndCaptureStructValue("Int1", 0);
                    var int2L = loadManager.DeserializeAndCaptureStructValue("Int2", 0);
                    var float1L = loadManager.DeserializeAndCaptureStructValue("Float1", 0f);
                    var boolL = loadManager.DeserializeAndCaptureStructValue("Bool1", false);
                    var vectorL = loadManager.DeserializeAndCaptureStructValue("Vector3", new Vector3(0, 0, 0));    
                    Assert.AreEqual(100, int1L.Value);
                    Assert.AreEqual(-50, int2L.Value);
                    Assert.AreEqual(123.456f, float1L.Value);
                    Assert.AreEqual(true, boolL.Value);
                    Assert.AreEqual(new Vector3(4, 5, 6), vectorL.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSingleSavableObject(int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager => 
                {
                    var value = saveManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    value._value1 = 1;
                    value._value2 = 2;
                    value._value3 = new DateTime(2021, 1, 1);
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    Assert.AreEqual(1, value._value1);
                    Assert.AreEqual(2, value._value2);
                    Assert.AreEqual(new DateTime(2021, 1, 1), value._value3);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSavableObjectsAndStructs(int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager => {
                    var floatVal = saveManager.DeserializeAndCaptureStructValue("Float1", 0f);
                    floatVal.Value = 123.456f;

                    var value = saveManager.DeserializeAndCaptureSavableObject("TestSavableObject", () => new TestSavableObject());
                    value._value1 = 122;
                    value._value2 = 322;
                    value._value3 = new DateTime(2021, 1, 1, 12, 34, 56);

                    var intVal = saveManager.DeserializeAndCaptureStructValue("Int1", 0);
                    intVal.Value = 100;

                    var boolVal = saveManager.DeserializeAndCaptureStructValue("Bool1", false);
                    boolVal.Value = true;

                    var value2 = saveManager.DeserializeAndCaptureSavableObject("TestSavableObject2", () => new TestSavableObject());
                    value2._value1 = 2;
                    value2._value2 = 3;
                    value2._value3 = new DateTime(2021, 1, 2);
                    
                    var vectorVal = saveManager.DeserializeAndCaptureStructValue("Vector3", new Vector3(1, 2, 3));
                    vectorVal.Value = new Vector3(4, 5, 6);
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject("TestSavableObject", () => new TestSavableObject());
                    Assert.AreEqual(122, value._value1);
                    Assert.AreEqual(322, value._value2);
                    Assert.AreEqual(new DateTime(2021, 1, 1, 12, 34, 56), value._value3);

                    var value2 = loadManager.DeserializeAndCaptureSavableObject("TestSavableObject2", () => new TestSavableObject());
                    Assert.AreEqual(2, value2._value1);
                    Assert.AreEqual(3, value2._value2);
                    Assert.AreEqual(new DateTime(2021, 1, 2), value2._value3);

                    var vectorL = loadManager.DeserializeAndCaptureStructValue("Vector3", new Vector3(0, 0, 0));
                    Assert.AreEqual(new Vector3(4, 5, 6), vectorL.Value);

                    var floatL = loadManager.DeserializeAndCaptureStructValue("Float1", 0f);
                    Assert.AreEqual(123.456f, floatL.Value);

                    var intL = loadManager.DeserializeAndCaptureStructValue("Int1", 0);
                    Assert.AreEqual(100, intL.Value);

                    var boolL = loadManager.DeserializeAndCaptureStructValue("Bool1", false);
                    Assert.AreEqual(true, boolL.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSavableObjectWithNullValue(int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureSavableObject("TestSavableObject", () => new TestSavableObject());
                    value._value4 = null;
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject("TestSavableObject", () => new TestSavableObject());
                    Assert.IsNull(value._value4);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSavableObjectWithSavableValue(int saveVersion, int loadVersion)
        {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureSavableObject("TestSavableObject", () => new TestSavableObject());
                    value._value4 = new TestSavableObject() { _value1 = 123, _value2 = 456, _value3 = new DateTime(2021, 1, 1) };
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject("TestSavableObject", () => new TestSavableObject());
                    Assert.IsNotNull(value._value4);
                    Assert.AreEqual(123, value._value4._value1);
                    Assert.AreEqual(456, value._value4._value2);
                    Assert.AreEqual(new DateTime(2021, 1, 1), value._value4._value3);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveStringWithoutChanging(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureCustomValue<string>("String1", () => "World");
                    Assert.AreEqual("Hello", value.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveString(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                    value.Value = "World";
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                    Assert.AreEqual("World", value.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveStringWithNullValue(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                    value.Value = null;
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                    Assert.IsNull(value.Value);
                },
                saveVersion,
                loadVersion);
        }


        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveEmptyString(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                    value.Value = "";
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                    Assert.AreEqual("", value.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSavableObjectWithEmptyString(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    value._value5 = "";
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    Assert.AreEqual("", value._value5);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSavableObjectWithNullString(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    value._value5 = null;
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    Assert.IsNull(value._value5);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSavableObjectWithString(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    value._value5 = "Hello";
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavableObject", () => new TestSavableObject());
                    Assert.AreEqual("Hello", value._value5);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestProblemCase1(int saveVersion, int loadVersion) {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureCustomValue<string>("SelectedProfile", null);
                    value.Value = "eerery";
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureCustomValue<string>("SelectedProfile", null);
                    Assert.AreEqual("eerery", value.Value);
                },
                saveVersion,
                loadVersion);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveLoadSaveWithoutCapturingStructValue(int saveVersion, int loadVersion)
        {
            // Test for non-meta data: save struct value, load it, then save again without capturing
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE 1
            // -----------------
            var saveManager1 = new StreamSaveManager(stream);
            saveManager1.UseExplicitSaveSystemVersion(saveVersion);

            saveManager1.LoadOrCreate("TestSave");
            
            var intValue = saveManager1.DeserializeAndCaptureStructValue("TestInt", 0);
            intValue.Value = 42;
            
            saveManager1.SaveExplicitly();

            // -----------------
            // LOAD PHASE
            // -----------------
            var streamCopy1 = ExpandableCopy(stream);
            var loadManager = new StreamSaveManager(streamCopy1);
            loadManager.UseExplicitSaveSystemVersion(loadVersion);

            loadManager.LoadOrCreate("TestSave");

            // -----------------
            // SAVE PHASE 2 (without capturing new values)
            // -----------------
            loadManager.SaveExplicitly();

            // -----------------
            // VERIFY PHASE
            // -----------------
            var streamCopy2 = ExpandableCopy(streamCopy1);
            var verifyManager = new StreamSaveManager(streamCopy2);
            verifyManager.LoadOrCreate("TestSave");
            
            var verifiedValue = verifyManager.DeserializeAndCaptureStructValue("TestInt", 0);
            Assert.AreEqual(42, verifiedValue.Value);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveLoadSaveWithoutCapturingCustomValue(int saveVersion, int loadVersion)
        {
            // Test for meta data: save custom value, load it, then save again without capturing
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE 1
            // -----------------
            var saveManager1 = new StreamSaveManager(stream);
            saveManager1.UseExplicitSaveSystemVersion(saveVersion);

            saveManager1.LoadOrCreate("TestSave");
            
            var stringValue = saveManager1.DeserializeAndCaptureCustomValue<string>("TestString", () => "default");
            stringValue.Value = "hello world";
            
            saveManager1.SaveExplicitly();

            // -----------------
            // LOAD PHASE
            // -----------------
            var streamCopy1 = ExpandableCopy(stream);
            var loadManager = new StreamSaveManager(streamCopy1);
            loadManager.UseExplicitSaveSystemVersion(loadVersion);

            loadManager.LoadOrCreate("TestSave");

            // -----------------
            // SAVE PHASE 2 (without capturing new values)
            // -----------------
            loadManager.SaveExplicitly();

            // -----------------
            // VERIFY PHASE
            // -----------------
            var streamCopy2 = ExpandableCopy(streamCopy1);
            var verifyManager = new StreamSaveManager(streamCopy2);
            verifyManager.LoadOrCreate("TestSave");
            
            var verifiedValue = verifyManager.DeserializeAndCaptureCustomValue<string>("TestString", () => "default");
            Assert.AreEqual("hello world", verifiedValue.Value);
        }

        [Test]
        [TestCaseSource(nameof(SaveLoadVersionPairsDefaultAndV0SaveV1Load))]
        public void TestSaveLoadSaveWithoutCapturingMixedData(int saveVersion, int loadVersion)
        {
            // Test for both meta and non-meta data: save both types, load them, then save again without capturing
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE 1
            // -----------------
            var saveManager1 = new StreamSaveManager(stream);
            saveManager1.UseExplicitSaveSystemVersion(saveVersion);

            saveManager1.LoadOrCreate("TestSave");
            
            // Non-meta data (struct)
            var intValue = saveManager1.DeserializeAndCaptureStructValue("TestInt", 0);
            intValue.Value = 123;
            
            var vectorValue = saveManager1.DeserializeAndCaptureStructValue("TestVector", Vector3.zero, true);
            vectorValue.Value = new Vector3(1, 2, 3);
            
            // Meta data (custom)
            var stringValue = saveManager1.DeserializeAndCaptureCustomValue<string>("TestString", () => "default");
            stringValue.Value = "test data";
            
            // Savable object
            var savableValue = saveManager1.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavable", () => new TestSavableObject());
            savableValue._value1 = 999;
            savableValue._value2 = 88.8f;
            savableValue._value5 = "savable string";
            
            saveManager1.SaveExplicitly();

            // -----------------
            // LOAD PHASE
            // -----------------
            var streamCopy1 = ExpandableCopy(stream);
            var loadManager = new StreamSaveManager(streamCopy1);
            loadManager.UseExplicitSaveSystemVersion(loadVersion);

            loadManager.LoadOrCreate("TestSave");

            // -----------------
            // SAVE PHASE 2 (without capturing new values)
            // -----------------
            loadManager.SaveExplicitly();

            // -----------------
            // VERIFY PHASE
            // -----------------
            var streamCopy2 = ExpandableCopy(streamCopy1);
            var verifyManager = new StreamSaveManager(streamCopy2);
            verifyManager.LoadOrCreate("TestSave");
            
            var verifiedInt = verifyManager.DeserializeAndCaptureStructValue("TestInt", 0);
            var verifiedVector = verifyManager.DeserializeAndCaptureStructValue("TestVector", Vector3.zero, true);
            var verifiedString = verifyManager.DeserializeAndCaptureCustomValue<string>("TestString", () => "default");
            var verifiedSavable = verifyManager.DeserializeAndCaptureSavableObject<TestSavableObject>("TestSavable", () => new TestSavableObject());
            
            Assert.AreEqual(123, verifiedInt.Value);
            Assert.AreEqual(new Vector3(1, 2, 3), verifiedVector.Value);
            Assert.AreEqual("test data", verifiedString.Value);
            Assert.AreEqual(999, verifiedSavable._value1);
            Assert.AreEqual(88.8f, verifiedSavable._value2);
            Assert.AreEqual("savable string", verifiedSavable._value5);
        }

        private static byte[] CreateValidSaveFileBytesWithStructValue(int value, string valueKey = "K")
        {
            var goodStream = new MemoryStream();
            var builder = new StreamSaveManager(goodStream);
            builder.LoadOrCreate("T");
            builder.DeserializeAndCaptureStructValue(valueKey, 0).Value = value;
            builder.SaveExplicitly();
            return goodStream.ToArray();
        }

        private static byte[] CreateValidSaveFileBytesWithMetaStructValue(int value, string valueKey = "MetaK")
        {
            var goodStream = new MemoryStream();
            var builder = new StreamSaveManager(goodStream);
            builder.LoadOrCreate("T");
            builder.DeserializeAndCaptureStructValue(valueKey, 0, isMetaValue: true).Value = value;
            builder.SaveExplicitly();
            return goodStream.ToArray();
        }

        [Test]
        public void LoadOrCreate_LoadsFromFallback_WhenPrimarySaveIsInvalid()
        {
            const string valueKey = "K";
            var goodBytes = CreateValidSaveFileBytesWithStructValue(42, valueKey);
            var primary = new MemoryStream(new byte[] { 1, 2, 3 });
            var fallback = new MemoryStream(goodBytes);
            var loader = new CorruptPrimaryFallbackSaveManager(primary, fallback);
            loader.LoadOrCreate("T");
            var loaded = loader.DeserializeAndCaptureStructValue(valueKey, 0);
            Assert.AreEqual(42, loaded.Value);
        }

        [Test]
        public void LoadOrCreate_LoadsFromFallback_WhenPrimarySaveIsMissing()
        {
            const string valueKey = "K";
            var goodBytes = CreateValidSaveFileBytesWithStructValue(55, valueKey);
            var fallback = new MemoryStream(goodBytes);
            var loader = new CorruptPrimaryFallbackSaveManager(null, fallback);
            loader.LoadOrCreate("T");
            var loaded = loader.DeserializeAndCaptureStructValue(valueKey, 0);
            Assert.AreEqual(55, loaded.Value);
        }

        [Test]
        public void LoadOrCreate_LoadsFromFallback_WhenPrimarySaveIsEmpty()
        {
            const string valueKey = "K";
            var goodBytes = CreateValidSaveFileBytesWithStructValue(66, valueKey);
            var primary = new MemoryStream();
            var fallback = new MemoryStream(goodBytes);
            var loader = new CorruptPrimaryFallbackSaveManager(primary, fallback);
            loader.LoadOrCreate("T");
            var loaded = loader.DeserializeAndCaptureStructValue(valueKey, 0);
            Assert.AreEqual(66, loaded.Value);
        }

        [Test]
        public void FileSaveManager_WritesFallbackCopy_AfterSuccessfulSave()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackSaveTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "mySave";
                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("X", 0).Value = 7;
                manager.SaveExplicitly();
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                Assert.IsTrue(File.Exists(mainPath), "Main save file should exist.");
                Assert.IsTrue(File.Exists(fallbackPath), "Fallback save file should exist after successful save.");
                CollectionAssert.AreEqual(File.ReadAllBytes(mainPath), File.ReadAllBytes(fallbackPath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void FileSaveManager_RemoveSave_DeletesMainAndFallbackFiles()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackRemoveTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "saveToRemove";
                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("N", 0).Value = 1;
                manager.SaveExplicitly();
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                Assert.IsTrue(File.Exists(fallbackPath));
                manager.RemoveSave(saveId);
                Assert.IsFalse(File.Exists(mainPath));
                Assert.IsFalse(File.Exists(fallbackPath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void FileSaveManager_LoadsFromFallback_WhenMainFileIsCorrupted()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackLoadTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "corruptMain";
                const string valueKey = "Score";
                var goodBytes = CreateValidSaveFileBytesWithStructValue(100, valueKey);
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                File.WriteAllBytes(fallbackPath, goodBytes);
                File.WriteAllBytes(mainPath, new byte[] { 0xDE, 0xAD });

                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                var loaded = manager.DeserializeAndCaptureStructValue(valueKey, 0);
                Assert.AreEqual(100, loaded.Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void FileSaveManager_LoadsFromFallback_WhenMainFileIsMissing()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackMissingMainTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "missingMain";
                const string valueKey = "Score";
                var goodBytes = CreateValidSaveFileBytesWithStructValue(101, valueKey);
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                File.WriteAllBytes(fallbackPath, goodBytes);
                Assert.IsFalse(File.Exists(mainPath), "Main save file should be absent for this scenario.");

                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                var loaded = manager.DeserializeAndCaptureStructValue(valueKey, 0);
                Assert.AreEqual(101, loaded.Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void FileSaveManager_LoadsFromFallback_WhenMainFileIsEmpty()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackEmptyMainTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "emptyMain";
                const string valueKey = "Score";
                var goodBytes = CreateValidSaveFileBytesWithStructValue(102, valueKey);
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                File.WriteAllBytes(fallbackPath, goodBytes);
                File.WriteAllBytes(mainPath, Array.Empty<byte>());

                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                var loaded = manager.DeserializeAndCaptureStructValue(valueKey, 0);
                Assert.AreEqual(102, loaded.Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public async Task GetMetaData_LoadsFromFallback_WhenMainFileIsMissing()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackMissingMainMetaTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "missingMainMeta";
                const string valueKey = "MetaScore";
                var goodBytes = CreateValidSaveFileBytesWithMetaStructValue(201, valueKey);
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                File.WriteAllBytes(fallbackPath, goodBytes);
                Assert.IsFalse(File.Exists(mainPath), "Main save file should be absent for this scenario.");

                var manager = new FileSaveManager(tempDir, 0, true);
                using IDataContainer metaContainer = await manager.GetMetaData(saveId);
                Assert.IsNotNull(metaContainer);
                Assert.AreEqual(201, metaContainer.DeserializeStructValue(valueKey, false, 0));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public async Task GetMetaData_LoadsFromFallback_WhenMainFileIsEmpty()
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, "FallbackEmptyMainMetaTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            try
            {
                const string saveId = "emptyMainMeta";
                const string valueKey = "MetaScore";
                var goodBytes = CreateValidSaveFileBytesWithMetaStructValue(202, valueKey);
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                File.WriteAllBytes(fallbackPath, goodBytes);
                File.WriteAllBytes(mainPath, Array.Empty<byte>());

                var manager = new FileSaveManager(tempDir, 0, true);
                using IDataContainer metaContainer = await manager.GetMetaData(saveId);
                Assert.IsNotNull(metaContainer);
                Assert.AreEqual(202, metaContainer.DeserializeStructValue(valueKey, false, 0));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private static IEnumerable<TestCaseData> SaveFormatAndCustomCodecVersionPairs()
        {
            yield return new TestCaseData(0, 0);
            yield return new TestCaseData(1, 0);
            yield return new TestCaseData(1, 1);
        }

        [Test]
        [TestCaseSource(nameof(SaveFormatAndCustomCodecVersionPairs))]
        public void RoundTrip_CustomPayload_RespectsCodecVersionInFile(int saveFileFormatVersion, int customCodecVersionOnSave)
        {
            const string saveId = "codecVersionRoundTrip";
            const string payloadKey = "VersionedPayload";
            const int expected = 4242;
            var stream = new MemoryStream();
            var saveManager = new VersionedPayloadSaveManager(stream, customCodecVersionOnSave);
            saveManager.UseExplicitSaveSystemVersion(saveFileFormatVersion);
            saveManager.LoadOrCreate(saveId);

            var mutable = saveManager.DeserializeAndCaptureCustomValue<VersionedTestPayload>(
                payloadKey,
                () => new VersionedTestPayload());
            mutable.Value.Value = expected;
            saveManager.SaveExplicitly();

            var copy = ExpandableCopy(stream);
            var loadManager = new VersionedPayloadSaveManager(copy, customCodecVersionOnSave);
            loadManager.UseExplicitSaveSystemVersion(saveFileFormatVersion);
            loadManager.LoadOrCreate(saveId);

            var loaded = loadManager.DeserializeAndCaptureCustomValue<VersionedTestPayload>(
                payloadKey,
                () => new VersionedTestPayload());
            Assert.AreEqual(expected, loaded.Value.Value);
        }

        [Test]
        public void Load_V1Format_UsesCustomCodecVersionFromFile_NotManagerCtor()
        {
            const string saveId = "codecVersionCtorIgnored";
            const string payloadKey = "VersionedPayload";
            const int expected = 9001;
            var stream = new MemoryStream();
            var saveManager = new VersionedPayloadSaveManager(stream, 1);
            saveManager.UseExplicitSaveSystemVersion(1);
            saveManager.LoadOrCreate(saveId);
            saveManager.DeserializeAndCaptureCustomValue<VersionedTestPayload>(payloadKey, () => new VersionedTestPayload()).Value.Value = expected;
            saveManager.SaveExplicitly();

            var copy = ExpandableCopy(stream);
            var loadManager = new VersionedPayloadSaveManager(copy, 999);
            loadManager.UseExplicitSaveSystemVersion(1);
            loadManager.LoadOrCreate(saveId);

            var loaded = loadManager.DeserializeAndCaptureCustomValue<VersionedTestPayload>(
                payloadKey,
                () => new VersionedTestPayload());
            Assert.AreEqual(expected, loaded.Value.Value);
        }

        [Test]
        public void Resave_V1Format_WritesManagersCustomCodecVersionInNewHeader()
        {
            const string saveId = "codecVersionResave";
            const string payloadKey = "VersionedPayload";
            var stream = new MemoryStream();
            var saveV0 = new VersionedPayloadSaveManager(stream, 0);
            saveV0.UseExplicitSaveSystemVersion(1);
            saveV0.LoadOrCreate(saveId);
            saveV0.DeserializeAndCaptureCustomValue<VersionedTestPayload>(payloadKey, () => new VersionedTestPayload()).Value.Value = 11;
            saveV0.SaveExplicitly();

            var afterLoad = ExpandableCopy(stream);
            var migrate = new VersionedPayloadSaveManager(afterLoad, 1);
            migrate.UseExplicitSaveSystemVersion(1);
            migrate.LoadOrCreate(saveId);
            Assert.AreEqual(11, migrate.DeserializeAndCaptureCustomValue<VersionedTestPayload>(payloadKey, () => null).Value.Value);
            migrate.SaveExplicitly();

            var verifyStream = ExpandableCopy(afterLoad);
            var verify = new VersionedPayloadSaveManager(verifyStream, 0);
            verify.UseExplicitSaveSystemVersion(1);
            verify.LoadOrCreate(saveId);
            Assert.AreEqual(11, verify.DeserializeAndCaptureCustomValue<VersionedTestPayload>(payloadKey, () => null).Value.Value);
        }

        [Test]
        public void TestConcurrentSaveOperations()
        {
            // This test verifies that multiple save managers can operate on the same directory
            // without causing sharing violations
            var tempDir = Path.Combine(Application.temporaryCachePath, "ConcurrentSaveTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var saveManager1 = new FileSaveManager(tempDir, 0, true);
                var saveManager2 = new FileSaveManager(tempDir, 0, true);
                
                // Initialize both save managers with different save IDs
                saveManager1.LoadOrCreate("save1");
                saveManager2.LoadOrCreate("save2");
                
                // Create some data in both save managers
                var value1 = saveManager1.DeserializeAndCaptureStructValue("TestValue", 42);
                var value2 = saveManager2.DeserializeAndCaptureStructValue("TestValue", 84);
                
                // Trigger concurrent saves
                saveManager1.SaveExplicitly();
                saveManager2.SaveExplicitly();
                
                // Verify the files were created
                Assert.IsTrue(File.Exists(Path.Combine(tempDir, "save1")), "Save file 1 should exist");
                Assert.IsTrue(File.Exists(Path.Combine(tempDir, "save2")), "Save file 2 should exist");
            }
            finally
            {
                // Clean up
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #region DataContainerAggregator Tests

        private sealed class RecordingDataContainer : IDataContainer
        {
            public string LastRequestedKey;

            public T DeserializeStructValue<T>(string valueKey, bool removeAfterDeserialization, T defaultValue = default)
                where T : unmanaged
            {
                LastRequestedKey = valueKey;
                return defaultValue;
            }

            public T DeserializeSavableObject<T>(
                string valueKey,
                bool removeAfterDeserialization,
                Func<T> factoryMethod = null) where T : ISaveObject, new()
            {
                LastRequestedKey = valueKey;
                return factoryMethod == null ? new T() : factoryMethod();
            }

            public T DeserializeCustomValue<T>(
                string valueKey,
                bool remvoeAfterDeserialization,
                Func<T> defaultValueFactory = null)
            {
                LastRequestedKey = valueKey;
                return defaultValueFactory == null ? default : defaultValueFactory();
            }

            IEnumerable<(string key, Utils.NativeList data)> IDataContainer.GetNativeData()
            {
                yield break;
            }

            public void Dispose()
            {
            }
        }

        [Test]
        public void DataContainerAggregator_RoutesPlainKeyToDefaultContainer()
        {
            var defaultContainer = new RecordingDataContainer();
            var aggregator = new DataContainerAggregator();
            aggregator.AddContainer("defaultData", defaultContainer);

            aggregator.DeserializeStructValue("Score", false, 0);

            Assert.AreEqual("Score", defaultContainer.LastRequestedKey);
        }

        [Test]
        public void DataContainerAggregator_RoutesPrefixedKeyToNamedContainer()
        {
            var inventoryContainer = new RecordingDataContainer();
            var aggregator = new DataContainerAggregator();
            aggregator.AddContainer("inventory", inventoryContainer);

            aggregator.DeserializeStructValue("inventory:Gold", false, 0);

            Assert.AreEqual("Gold", inventoryContainer.LastRequestedKey);
        }

        [Test]
        public void DataContainerAggregator_ReturnsDefaultWhenContainerMissing()
        {
            var aggregator = new DataContainerAggregator();

            Assert.AreEqual(5, aggregator.DeserializeStructValue("missing:Key", false, 5));
        }

        #endregion

        #region MultifileSaveManager Tests

        private const string ProfileFallbackSaveSuffix = "___fallbacksave";
        private const string ProfileIntegrityHashFileName = "integrityhash";

        private static string CreateTempSaveDirectory(string prefix)
        {
            var tempDir = Path.Combine(Application.temporaryCachePath, prefix + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        [Test]
        public void MultifileSaveManager_SaveExplicitly_WritesIntegrityHashAndFallbackProfile()
        {
            var tempDir = CreateTempSaveDirectory("MultifileSave_");
            try
            {
                const string saveId = "profile1";
                var manager = new MultifileSaveManager(tempDir, 0);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("Score", 0).Value = 99;

                var hash = manager.SaveExplicitly();

                Assert.IsNotNull(hash);
                Assert.AreEqual(32, hash.Length);

                var profilePath = Path.Combine(tempDir, saveId);
                var integrityHashPath = Path.Combine(profilePath, ProfileIntegrityHashFileName);
                var fallbackProfilePath = Path.Combine(tempDir, saveId + ProfileFallbackSaveSuffix);

                Assert.IsTrue(File.Exists(integrityHashPath));
                CollectionAssert.AreEqual(hash, File.ReadAllBytes(integrityHashPath));
                Assert.IsTrue(Directory.Exists(fallbackProfilePath));
                Assert.IsTrue(File.Exists(Path.Combine(fallbackProfilePath, "defaultData")));
                Assert.IsTrue(File.Exists(Path.Combine(fallbackProfilePath, ProfileIntegrityHashFileName)));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void MultifileSaveManager_GetSaveHash_ReturnsStoredIntegrityHash()
        {
            var tempDir = CreateTempSaveDirectory("MultifileHash_");
            try
            {
                const string saveId = "profile1";
                var manager = new MultifileSaveManager(tempDir, 0);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("Score", 0).Value = 11;

                var savedHash = manager.SaveExplicitly();
                var readHash = manager.GetSaveHash(saveId);

                Assert.IsNotNull(readHash);
                CollectionAssert.AreEqual(savedHash, readHash);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void MultifileSaveManager_SaveAndLoad_RoundTripsDefaultAndNamedFiles()
        {
            var tempDir = CreateTempSaveDirectory("MultifileRoundTrip_");
            try
            {
                const string saveId = "profile1";
                var manager = new MultifileSaveManager(tempDir, 0);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("Score", 0).Value = 10;
                manager.DeserializeAndCaptureStructValue("stats:Wins", 0).Value = 5;
                manager.SaveExplicitly();

                Assert.IsTrue(File.Exists(Path.Combine(tempDir, saveId, "stats")));

                var loader = new MultifileSaveManager(tempDir, 0);
                loader.LoadOrCreate(saveId);

                Assert.AreEqual(10, loader.DeserializeAndCaptureStructValue("Score", 0).Value);
                Assert.AreEqual(5, loader.DeserializeAndCaptureStructValue("stats:Wins", 0).Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void MultifileSaveManager_LoadOrCreate_RestoresFromFallback_WhenPrimaryProfileIsCorrupted()
        {
            var tempDir = CreateTempSaveDirectory("MultifileFallbackRestore_");
            try
            {
                const string saveId = "profile1";
                const string valueKey = "Score";
                var manager = new MultifileSaveManager(tempDir, 0);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue(valueKey, 0).Value = 42;
                manager.SaveExplicitly();

                var primaryDefaultDataPath = Path.Combine(tempDir, saveId, "defaultData");
                var fallbackDefaultDataPath = Path.Combine(tempDir, saveId + ProfileFallbackSaveSuffix, "defaultData");
                File.WriteAllBytes(primaryDefaultDataPath, new byte[] { 0xDE, 0xAD });

                var loader = new MultifileSaveManager(tempDir, 0);
                loader.LoadOrCreate(saveId);

                Assert.AreEqual(42, loader.DeserializeAndCaptureStructValue(valueKey, 0).Value);
                CollectionAssert.AreEqual(
                    File.ReadAllBytes(fallbackDefaultDataPath),
                    File.ReadAllBytes(primaryDefaultDataPath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void MultifileSaveManager_RemoveSave_DeletesPrimaryAndFallbackProfiles()
        {
            var tempDir = CreateTempSaveDirectory("MultifileRemove_");
            try
            {
                const string saveId = "profile1";
                var manager = new MultifileSaveManager(tempDir, 0);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("Score", 0).Value = 1;
                manager.SaveExplicitly();

                var profilePath = Path.Combine(tempDir, saveId);
                var fallbackProfilePath = Path.Combine(tempDir, saveId + ProfileFallbackSaveSuffix);
                Assert.IsTrue(Directory.Exists(profilePath));
                Assert.IsTrue(Directory.Exists(fallbackProfilePath));

                manager.RemoveSave(saveId);

                Assert.IsFalse(Directory.Exists(profilePath));
                Assert.IsFalse(Directory.Exists(fallbackProfilePath));
                Assert.IsNull(manager.CurrentSaveId);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void MultifileSaveManager_RegisterCustomCodec_AppliesToLazyCreatedFileManager()
        {
            var tempDir = CreateTempSaveDirectory("MultifileCodec_");
            try
            {
                const string saveId = "profile1";
                const string payloadKey = "extra:Payload";
                var manager = new MultifileSaveManager(tempDir, 0);
                manager.RegisterCustomCodec(new VersionedTestPayloadCodec());
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureCustomValue<VersionedTestPayload>(payloadKey, () => new VersionedTestPayload())
                    .Value.Value = 77;
                manager.SaveExplicitly();

                var loader = new MultifileSaveManager(tempDir, 0);
                loader.RegisterCustomCodec(new VersionedTestPayloadCodec());
                loader.LoadOrCreate(saveId);

                Assert.AreEqual(
                    77,
                    loader.DeserializeAndCaptureCustomValue<VersionedTestPayload>(payloadKey, () => null).Value.Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #endregion

        [Test]
        public void FileSaveManager_SaveExplicitly_ReturnsIntegrityHash()
        {
            var tempDir = CreateTempSaveDirectory("SaveHashReturn_");
            try
            {
                const string saveId = "hashSave";
                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("Score", 0).Value = 3;

                var hash = manager.SaveExplicitly();

                Assert.IsNotNull(hash);
                Assert.AreEqual(32, hash.Length);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void FileSaveManager_GetSaveHash_ReturnsTrailingIntegrityHash()
        {
            var tempDir = CreateTempSaveDirectory("GetSaveHash_");
            try
            {
                const string saveId = "hashSave";
                var manager = new FileSaveManager(tempDir, 0, true);
                manager.LoadOrCreate(saveId);
                manager.DeserializeAndCaptureStructValue("Score", 0).Value = 8;

                var savedHash = manager.SaveExplicitly();
                var readHash = manager.GetSaveHash(saveId);

                Assert.IsNotNull(readHash);
                CollectionAssert.AreEqual(savedHash, readHash);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void FileSaveManager_DoesNotLoadFallback_WhenUseFallbackDisabled()
        {
            var tempDir = CreateTempSaveDirectory("NoFallbackLoad_");
            try
            {
                const string saveId = "noFallback";
                const string valueKey = "Score";
                var goodBytes = CreateValidSaveFileBytesWithStructValue(100, valueKey);
                var mainPath = Path.Combine(tempDir, saveId);
                var fallbackPath = Path.Combine(tempDir, saveId + FallbackSaveSuffix);
                File.WriteAllBytes(fallbackPath, goodBytes);
                File.WriteAllBytes(mainPath, new byte[] { 0xDE, 0xAD });

                var manager = new FileSaveManager(tempDir, 0, useFallback: false);
                manager.LoadOrCreate(saveId);
                var loaded = manager.DeserializeAndCaptureStructValue(valueKey, 0);

                Assert.AreEqual(0, loaded.Value);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}