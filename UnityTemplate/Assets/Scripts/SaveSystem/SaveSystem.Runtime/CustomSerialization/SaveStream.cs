using System;
using System.Collections.Generic;
using System.IO;
using kekchpek.SaveSystem.Codec;

namespace kekchpek.SaveSystem.CustomSerialization
{
    public class SaveStream : ISaveStream
    {

        private static readonly Stack<SaveStream> Pool = new();

        private bool _isActive;
        private ISaveCodecAdapter _adapter;
        private Stream _stream;

        Stream ISaveStream.Stream => _stream;

        private SaveStream()
        {
        }

        internal static ISaveStream Get(
            Stream stream,
            ISaveCodecAdapter adapter)
        {
            var s = Pool.Count == 0 ? new SaveStream() : Pool.Pop();
            s.Initialize(stream, adapter);
            s._isActive = true;
            return s;
        }
        
        private void Initialize(
            Stream stream,
            ISaveCodecAdapter adapter)
        {
            _stream = stream;
            _adapter = adapter;
        }

        public void SaveStruct<T>(T val) where T : unmanaged => _adapter.WriteStruct(_stream, val);

        public void SaveSabable<T>(T val) where T : ISaveObject, new() => val.Serialize(this);
        

        public void SaveCustom<T>(T val) => _adapter.WriteCustom(_stream, val);
        
        public static void Release(SaveStream s) => s.Dispose();

        public void Dispose()
        {
            if (!_isActive) return;
            _stream = null;
            _adapter = null;
            _isActive = false;
            Pool.Push(this);
        }
    }
}