using System;
using System.IO;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.Tests
{
    public class SaveManagerTests
    {

        #region Test Helpers

        private class StreamSaveManager : BaseSaveManager
        {
            private readonly Stream _stream;

            public StreamSaveManager(Stream stream)
            {
                _stream = stream;
            }

            protected override Stream GetStreamToWrite(string saveId) => _stream;

            protected override bool TryGetStreamToRead(string saveId, out Stream s)
            {
                s = _stream;
                return true;
            }

            public override string[] GetSaves()
            {
                throw new NotImplementedException();
            }

            protected override void ReleaseStream(Stream s)
            {
                // Do nothing because stream is reused during tests.
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

            public void Deserialize(ILoadStream loadStream)
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

            public void Serialize(ISaveStream saveStream)
            {
                saveStream.SaveStruct(_value2);
                saveStream.SaveStruct(_value3);
                saveStream.SaveStruct(_value4 != null);
                if (_value4 != null)
                {
                    _value4.Serialize(saveStream);
                }
                saveStream.SaveStruct(_value1);
                saveStream.SaveStruct(_value5 != null);
                if (_value5 != null)
                {
                    saveStream.SaveCustom(_value5);
                }
            }
        }

        #endregion

        // Helper that performs the full save → load cycle and lets callers provide
        // actions for the save-phase and the load-phase respectively.
        private static void SaveThenLoad(Action<StreamSaveManager> savePhase, Action<StreamSaveManager> loadPhase)
        {
            // Prepare in-memory stream acting as our persistent storage.
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE
            // -----------------
            var saveManager = new StreamSaveManager(stream);
            saveManager.LoadOrCreate("TestSave");

            savePhase(saveManager);
            saveManager.SaveExplicitly();

            // -----------------
            // LOAD PHASE
            // -----------------
            var streamCopy = new MemoryStream(stream.ToArray());
            var loadManager = new StreamSaveManager(streamCopy);
            loadManager.LoadOrCreate("TestSave");
            loadPhase(loadManager);
        }

        [Test]
        public void TestSingleInt([Values(1, 0, -1, int.MaxValue, int.MinValue)] int expectedValue)
        {
            SaveThenLoad(
                saveManager => saveManager.DeserializeAndCaptureStructValue("TestInterger", expectedValue),
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureStructValue("TestInterger", 11);
                    Assert.AreEqual(expectedValue, value.Value);
                }
            );
        }

        [Test]
        public void TestSingleIntChange([Values(1, 0, -1, int.MaxValue, int.MinValue)] int expectedValue)
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
                }
            );
        }
        
        [Test]
        public void TestMultipleStructValues()
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
                }
            );
        }

        [Test]
        public void TestSingleSavableObject()
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
                }
            );
        }

        [Test]
        public void TestSavableObjectsAndStructs()
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
                }
            );
        }

        [Test]
        public void TestSavableObjectWithNullValue()
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
                }
            );
        }

        [Test]
        public void TestSavableObjectWithSavableValue()
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
                }
            );
        }

        [Test]
        public void TestSaveStringWithoutChanging() {
            SaveThenLoad(
                saveManager =>
                {
                    var value = saveManager.DeserializeAndCaptureCustomValue<string>("String1", () => "Hello");
                },
                loadManager =>
                {
                    var value = loadManager.DeserializeAndCaptureCustomValue<string>("String1", () => "World");
                    Assert.AreEqual("Hello", value.Value);
                }
            );
        }

        [Test]
        public void TestSaveString() {
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
                }
            );
        }

        [Test]
        public void TestSaveStringWithNullValue() {
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
                }
            );
        }


        [Test]
        public void TestSaveEmptyString() {
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
                }
            );
        }

        [Test]
        public void TestSavableObjectWithEmptyString() {
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
                }
            );
        }

        [Test]
        public void TestSavableObjectWithNullString() {
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
                }
            );
        }

        [Test]
        public void TestSavableObjectWithString() {
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
                }
            );
        }

        [Test]
        public void TestProblemCase1() {
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
                }
            );
        }

        [Test]
        public void TestSaveLoadSaveWithoutCapturingStructValue()
        {
            // Test for non-meta data: save struct value, load it, then save again without capturing
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE 1
            // -----------------
            var saveManager1 = new StreamSaveManager(stream);
            saveManager1.LoadOrCreate("TestSave");
            
            var intValue = saveManager1.DeserializeAndCaptureStructValue("TestInt", 0);
            intValue.Value = 42;
            
            saveManager1.SaveExplicitly();

            // -----------------
            // LOAD PHASE
            // -----------------
            var streamCopy1 = new MemoryStream(stream.ToArray());
            var loadManager = new StreamSaveManager(streamCopy1);
            loadManager.LoadOrCreate("TestSave");

            // Resets position here because StreamSaveManager uses the same stream for both save and load
            streamCopy1.Position = 0;

            // -----------------
            // SAVE PHASE 2 (without capturing new values)
            // -----------------
            loadManager.SaveExplicitly();

            // -----------------
            // VERIFY PHASE
            // -----------------
            var streamCopy2 = new MemoryStream(streamCopy1.ToArray());
            var verifyManager = new StreamSaveManager(streamCopy2);
            verifyManager.LoadOrCreate("TestSave");
            
            var verifiedValue = verifyManager.DeserializeAndCaptureStructValue("TestInt", 0);
            Assert.AreEqual(42, verifiedValue.Value);
        }

        [Test]
        public void TestSaveLoadSaveWithoutCapturingCustomValue()
        {
            // Test for meta data: save custom value, load it, then save again without capturing
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE 1
            // -----------------
            var saveManager1 = new StreamSaveManager(stream);
            saveManager1.LoadOrCreate("TestSave");
            
            var stringValue = saveManager1.DeserializeAndCaptureCustomValue<string>("TestString", () => "default");
            stringValue.Value = "hello world";
            
            saveManager1.SaveExplicitly();

            // -----------------
            // LOAD PHASE
            // -----------------
            var streamCopy1 = new MemoryStream(stream.ToArray());
            var loadManager = new StreamSaveManager(streamCopy1);
            loadManager.LoadOrCreate("TestSave");

            // Resets position here because StreamSaveManager uses the same stream for both save and load
            streamCopy1.Position = 0;

            // -----------------
            // SAVE PHASE 2 (without capturing new values)
            // -----------------
            loadManager.SaveExplicitly();

            // -----------------
            // VERIFY PHASE
            // -----------------
            var streamCopy2 = new MemoryStream(streamCopy1.ToArray());
            var verifyManager = new StreamSaveManager(streamCopy2);
            verifyManager.LoadOrCreate("TestSave");
            
            var verifiedValue = verifyManager.DeserializeAndCaptureCustomValue<string>("TestString", () => "default");
            Assert.AreEqual("hello world", verifiedValue.Value);
        }

        [Test]
        public void TestSaveLoadSaveWithoutCapturingMixedData()
        {
            // Test for both meta and non-meta data: save both types, load them, then save again without capturing
            var stream = new MemoryStream();

            // -----------------
            // SAVE PHASE 1
            // -----------------
            var saveManager1 = new StreamSaveManager(stream);
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
            var streamCopy1 = new MemoryStream(stream.ToArray());
            var loadManager = new StreamSaveManager(streamCopy1);
            loadManager.LoadOrCreate("TestSave");

            // Resets position here because StreamSaveManager uses the same stream for both save and load
            streamCopy1.Position = 0;

            // -----------------
            // SAVE PHASE 2 (without capturing new values)
            // -----------------
            loadManager.SaveExplicitly();

            // -----------------
            // VERIFY PHASE
            // -----------------
            var streamCopy2 = new MemoryStream(streamCopy1.ToArray());
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

        [Test]
        public void TestConcurrentSaveOperations()
        {
            // This test verifies that multiple save managers can operate on the same directory
            // without causing sharing violations
            var tempDir = Path.Combine(Application.temporaryCachePath, "ConcurrentSaveTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var saveManager1 = new SaveSystem.SaveManagers.FileSaveManager(tempDir);
                var saveManager2 = new SaveSystem.SaveManagers.FileSaveManager(tempDir);
                
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
    }
}