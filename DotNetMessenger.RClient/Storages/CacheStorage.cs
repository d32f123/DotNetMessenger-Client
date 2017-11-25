using System;
using System.Collections.Generic;

namespace DotNetMessenger.RClient.Storages
{
    public class CacheStorage<TKey, TValue>
    {
        private const int CacheDefaultSize = 50;
        private readonly int _cacheSize;

        private readonly object _lock = new object();
        private readonly LinkedList<TKey> _containedKeys;
        private readonly Dictionary<TKey, TValue> _cacheDictionary;

        public CacheStorage() : this(CacheDefaultSize){}

        public CacheStorage(int cacheSize)
        {
            _cacheSize = cacheSize;
            _containedKeys = new LinkedList<TKey>();
            _cacheDictionary = new Dictionary<TKey, TValue>(_cacheSize);
        }

        public TValue GetValue(TKey key)
        {
            lock (_lock)
                return _cacheDictionary.ContainsKey(key) ? _cacheDictionary[key] : throw new ArgumentException();
        }

        public void SetValue(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (!_cacheDictionary.ContainsKey(key))
                    throw new ArgumentException();
                _cacheDictionary[key] = value;
            }
        }

        public TValue this[TKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (_cacheDictionary.ContainsKey(key))
                    throw new ArgumentException();
                if (_containedKeys.Count == _cacheSize)
                {
                    _cacheDictionary.Remove(_containedKeys.First.Value);
                    _containedKeys.RemoveFirst();
                }
                _containedKeys.AddLast(key);
                _cacheDictionary.Add(key, value);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (_cacheDictionary.ContainsKey(key)) return false;
                _containedKeys.Remove(key);
                _cacheDictionary.Remove(key);
                return true;
            }
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> keyValues)
        {
            foreach (var keyValue in keyValues)
            {
                Add(keyValue.Key, keyValue.Value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_lock)
                return _cacheDictionary.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            lock (_lock)
                return _cacheDictionary.ContainsValue(value);
        }

        public int Count
        {
            get
            {
                lock (_lock)
                    return _cacheDictionary.Count;
            }
        }

        public IEnumerable<TValue> Values {
            get
            {
                lock (_lock)
                    return _cacheDictionary.Values;
            }
        }
    }
}