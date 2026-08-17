using System.Collections.Generic;

namespace kekchpek.Auxiliary
{
    public readonly struct InternedString
    {

        private static int GlobalCounter = 0;

        private static readonly Dictionary<string, int> _stringIds = new();
        private static readonly Dictionary<int, string> _stringValues = new();

        private readonly int _stringId;

        public InternedString(string value)
        {
            if (_stringIds.TryGetValue(value, out var stringId))
            {
                _stringId = stringId;
            }
            else
            {
                _stringId = GlobalCounter++;
                _stringIds.Add(value, _stringId);
                _stringValues.Add(_stringId, value);
            }
        }

        public readonly void Release() 
        {
            _stringIds.Remove(_stringValues[_stringId]);
            _stringValues.Remove(_stringId);
        }

        public override readonly string ToString()
        {
            return _stringValues[_stringId];
        }
    }
}