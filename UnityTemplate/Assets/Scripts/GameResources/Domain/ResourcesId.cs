using System.Collections.Generic;
using UnityEngine;

namespace GameResources.Domain
{
    public readonly struct ResourceId
    {

        private static readonly Dictionary<int, string> _ids = new();
        
        public static readonly ResourceId Scarabs = new(nameof(Scarabs));        public static ResourceId FromString(string s) => new(s);

        private readonly int _id;

        private ResourceId(string id)
        {
            var idHash = id.GetHashCode();
            while (_ids.TryGetValue(idHash, out var existingId))
            {
                if (existingId != id)
                {
                    Debug.LogError($"ResourceId {id} has hash collision! Please rename it to any other.");
                    idHash++;
                }
                else
                {
                    _id = idHash;
                    return;
                }
            }
            _id = idHash;
            _ids.Add(idHash, id);
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceId id && Equals(id);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public bool Equals(ResourceId other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public override string ToString()
        {
            if (_ids.TryGetValue(_id, out var id))
                return id;
            return "<UNKNOWN_RESOURCE_ID>";
        }

        public static bool operator ==(ResourceId resourceId1, ResourceId resourceId2)
        {
            return resourceId1._id == resourceId2._id;
        }

        public static bool operator !=(ResourceId resourceId1, ResourceId resourceId2)
        {
            return !(resourceId1 == resourceId2);
        }
    }
}