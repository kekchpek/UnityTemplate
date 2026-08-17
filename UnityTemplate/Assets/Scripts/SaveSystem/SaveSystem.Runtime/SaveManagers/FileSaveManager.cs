using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

namespace kekchpek.SaveSystem.SaveManagers
{
    public class FileSaveManager : StreamSaveManager
    {
        private readonly string _folderPath;
        private const int MaxRetries = 5;
        private const int RetryDelayMs = 100;
        
        // Global coordination for file operations across all FileSaveManager instances
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

        public FileSaveManager(
            string folderPath,
            int currentCustomCodecsVersion,
            bool useFallback,
            bool useMultithreading = true,
            IMutableFactory mutableFactory = null
            )
            : base(currentCustomCodecsVersion, useMultithreading, useFallback, mutableFactory)
        {
            _folderPath = folderPath;
        }

        private static SemaphoreSlim GetOrCreateFileLock(string filePath)
        {
            return _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        }

        protected override Stream GetStreamToWrite(string saveId)
        {
            if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
            var filePath = GetSavePath(saveId);
            var normalizedPath = Path.GetFullPath(filePath);
            var fileLock = GetOrCreateFileLock(normalizedPath);
            
            // Wait for exclusive access to this file
            fileLock.Wait();
            
            try
            {
                // Retry logic to handle transient sharing violations
                for (int attempt = 0; attempt < MaxRetries; attempt++)
                {
                    try
                    {
                        // ReadWrite: AppendIntegrityHash must read the payload after Encode on the same stream.
                        // FileShare.Read allows other processes to read while we hold the lock for writing.
                        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                        return new FileLockWrapper(fileStream, fileLock);
                    }
                    catch (IOException ex) when (attempt < MaxRetries - 1)
                    {
                        Debug.LogWarning(ex);
                        Debug.LogWarning($"Sharing violation on attempt {attempt + 1} for {filePath}. Retrying...");
                        Thread.Sleep(RetryDelayMs * (attempt + 1)); // Exponential backoff
                    }
                }
                
                // Final attempt without retry
                return new FileLockWrapper(new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read), fileLock);
            }
            catch (Exception e)
            {
                // Release the lock if we failed to create the stream
                Debug.LogError(e);
                fileLock.Release();
                throw;
            }
        }

        protected override bool TryGetStreamToRead(string saveId, out Stream stream)
        {
            var fileName = GetSavePath(saveId);
            return TryGetStreamToReadInternal(fileName, out stream);
        }

        private bool TryGetStreamToReadInternal(string fileName, out Stream stream)
        {
            if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
            if (!File.Exists(fileName))
            {
                stream = null;
                return false;
            }

            // Use FileStream with FileShare.ReadWrite to allow concurrent access
            stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return true;
        }

        public override string[] GetSaves()
        {
            if (!Directory.Exists(_folderPath)) return Array.Empty<string>();
            var paths = Directory.GetFiles(_folderPath);
            for (var i = 0; i < paths.Length; i++)
            {
                paths[i] = paths[i].Replace('\\', '/');
                paths[i] = paths[i][(paths[i].LastIndexOf('/')+1)..];
            }

            return paths;
        }

        public override void RemoveSave(string saveId)
        {
            RemoveSaveFile(GetSavePath(saveId));
            RemoveSaveFile(GetFallbackSavePath(saveId));
        }

        private void RemoveSaveFile(string filePath) 
        {
            var normalizedPath = Path.GetFullPath(filePath);
            var fileLock = GetOrCreateFileLock(normalizedPath);
            fileLock.Wait();
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        protected override void ReleaseStream(Stream s)
        {
            s.Dispose();
        }

        protected override void FallbackSave(string saveId)
        {
            var filePath = GetSavePath(saveId);
            var fallbackFilePath = GetFallbackSavePath(saveId);
            if (File.Exists(filePath))
            {
                RemoveSaveFile(fallbackFilePath);
                File.Copy(filePath, fallbackFilePath);
            }
            else 
            {
                Debug.LogError($"Fail to fallback save. File {filePath} not found.");
            }
        }

        protected override bool TryGetFallbackStreamToRead(string saveId, out Stream stream)
        {
            var fileName = GetFallbackSavePath(saveId);
            return TryGetStreamToReadInternal(fileName, out stream);
        }

        private string GetSavePath(string saveId)
        {
            return $"{_folderPath}/{saveId}";
        }

        private string GetFallbackSavePath(string saveId)
        {
            return $"{_folderPath}/{saveId}___fallbacksave";
        }
    }

    // Wrapper class to ensure the file lock is released when the stream is disposed
    internal class FileLockWrapper : Stream
    {
        private readonly Stream _innerStream;
        private readonly SemaphoreSlim _lock;
        private bool _disposed;

        public FileLockWrapper(Stream innerStream, SemaphoreSlim lockObject)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _lock = lockObject ?? throw new ArgumentNullException(nameof(lockObject));
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _innerStream?.Dispose();
                }
                finally
                {
                    _lock?.Release();
                    _disposed = true;
                }
            }
            base.Dispose(disposing);
        }
    }
}