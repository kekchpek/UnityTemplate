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

        NativeList ILoadStream.Data => _data;
        Stream ILoadStream.Stream => _stream;

        private LoadStream()
        {
        }

        internal static ILoadStream Get(
            Stream stream,
            NativeList data,
            ILoadCodecAdapter loadCodecAdapter)
        {
            var s = Pool.Count == 0 ? new LoadStream() : Pool.Pop();
            s.Initialize(stream, data, loadCodecAdapter);
            s._isActive = true;
            return s;
        }
        
        private void Initialize(
            Stream stream,
            NativeList data,
            ILoadCodecAdapter loadCodecAdapter)
        {
            _stream = stream;
            _data = data;
            _adapter = loadCodecAdapter;
        }

        public static void Release(SaveStream s)
        {
            s.Dispose();
        }

        public T LoadStruct<T>() where T : unmanaged => _adapter.ReadStruct<T>(_stream);

        public T LoadSavable<T>() where T : ISaveObject, new()
        {
            var val = new T();
            val.Deserialize(this);
            return val;
        }
        
        public T LoadCustom<T>() => _adapter.ReadCustom<T>(_stream);

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