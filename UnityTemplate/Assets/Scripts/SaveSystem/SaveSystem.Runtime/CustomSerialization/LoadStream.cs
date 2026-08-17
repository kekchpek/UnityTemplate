using System.Collections.Generic;
using System.IO;
using kekchpek.SaveSystem.Utils;

namespace kekchpek.SaveSystem.CustomSerialization
{
    public class LoadStream : ILoadStream
    {
        
        private static readonly Stack<LoadStream> Pool = new();

        private bool _isActive;
        private ILoadCodecAdapter _adapter;
        private Stream _stream;
        private NativeList _data;
        private int? _customCodecVersion;

        public int? CustomCodecVersion => _customCodecVersion;

        NativeList ILoadStream.Data => _data;
        Stream ILoadStream.Stream => _stream;

        private LoadStream()
        {
        }

        internal static ILoadStream Get(
            Stream stream,
            NativeList data,
            ILoadCodecAdapter loadCodecAdapter,
            int? customCodecVersion)
        {
            var s = Pool.Count == 0 ? new LoadStream() : Pool.Pop();
            s.Initialize(stream, data, loadCodecAdapter, customCodecVersion);
            s._isActive = true;
            return s;
        }
        
        private void Initialize(
            Stream stream,
            NativeList data,
            ILoadCodecAdapter loadCodecAdapter,
            int? customCodecVersion
            )
        {
            _stream = stream;
            _data = data;
            _adapter = loadCodecAdapter;
            _customCodecVersion = customCodecVersion;
        }

        public static void Release(SaveStream s)
        {
            s.Dispose();
        }

        public T LoadStruct<T>() where T : unmanaged => _adapter.ReadStruct<T>(_stream);

        public T LoadSavable<T>() where T : ISaveObject, new()
        {
            var val = new T();
            val.Deserialize(this, _customCodecVersion);
            return val;
        }
        
        public T LoadCustom<T>() => _adapter.ReadCustom<T>(_stream, _customCodecVersion);

        public void Dispose()
        {
            if (!_isActive)
                return;
            _stream = null;
            _adapter = null;
            if (_data != null) {
                StaticBufferPool.Release(_data);
            }
            _data = null;
            _isActive = false;
            Pool.Push(this);
        }

        public bool IsEnd()
        {
            return _stream.Position == _stream.Length;
        }
    }
}