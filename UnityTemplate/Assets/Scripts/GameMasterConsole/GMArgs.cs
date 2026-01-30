using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GMConsole
{
    /// <summary>
    /// Argument container for GameMaster commands with ordered, nameless arguments
    /// Supports string and float types with separate queues for each type
    /// Commands can set result messages through this class
    /// </summary>
    public class GMArgs
    {
        private readonly Queue<string> _stringArgs;
        private readonly Queue<float> _floatArgs;
        
        /// <summary>
        /// Result message set by the command during execution
        /// </summary>
        public string ResultMessage { get; private set; }
        
        /// <summary>
        /// Additional result data set by the command during execution
        /// </summary>
        public Dictionary<string, object> ResultData { get; private set; }
        
        /// <summary>
        /// Creates a new GMArgs instance from a dictionary of arguments
        /// </summary>
        /// <param name="arguments">Dictionary containing command arguments</param>
        public GMArgs(Dictionary<string, string> arguments)
        {
            _stringArgs = new Queue<string>();
            _floatArgs = new Queue<float>();
            ResultData = new Dictionary<string, object>();
            
            ParseArguments(arguments);
        }
        
        /// <summary>
        /// Creates a new GMArgs instance from arrays of arguments
        /// </summary>
        /// <param name="stringArgs">Array of string arguments</param>
        /// <param name="floatArgs">Array of float arguments</param>
        public GMArgs(string[] stringArgs = null, float[] floatArgs = null)
        {
            _stringArgs = new Queue<string>();
            _floatArgs = new Queue<float>();
            ResultData = new Dictionary<string, object>();
            
            if (stringArgs != null)
            {
                foreach (var arg in stringArgs)
                {
                    _stringArgs.Enqueue(arg);
                }
            }
            
            if (floatArgs != null)
            {
                foreach (var arg in floatArgs)
                {
                    _floatArgs.Enqueue(arg);
                }
            }
        }
        
        /// <summary>
        /// Gets the next string argument from the string queue
        /// </summary>
        /// <returns>Next string argument</returns>
        /// <exception cref="InvalidOperationException">Thrown when no more string arguments are available</exception>
        public string GetString()
        {
            if (_stringArgs.Count == 0)
            {
                throw new InvalidOperationException("No more string arguments available");
            }
            return _stringArgs.Dequeue();
        }
        
        /// <summary>
        /// Gets the next float argument from the float queue
        /// </summary>
        /// <returns>Next float argument</returns>
        /// <exception cref="InvalidOperationException">Thrown when no more float arguments are available</exception>
        public float GetFloat()
        {
            if (_floatArgs.Count == 0)
            {
                throw new InvalidOperationException("No more float arguments available");
            }
            return _floatArgs.Dequeue();
        }
        
        /// <summary>
        /// Tries to get the next string argument from the string queue
        /// </summary>
        /// <param name="value">The retrieved string value, or null if not available</param>
        /// <returns>True if string argument was available and retrieved, false otherwise</returns>
        public bool TryGetString(out string value)
        {
            if (_stringArgs.Count > 0)
            {
                value = _stringArgs.Dequeue();
                return true;
            }
            
            value = null;
            return false;
        }
        
        /// <summary>
        /// Tries to get the next float argument from the float queue
        /// </summary>
        /// <param name="value">The retrieved float value, or 0 if not available</param>
        /// <returns>True if float argument was available and retrieved, false otherwise</returns>
        public bool TryGetFloat(out float value)
        {
            if (_floatArgs.Count > 0)
            {
                value = _floatArgs.Dequeue();
                return true;
            }
            
            value = 0f;
            return false;
        }
        
        /// <summary>
        /// Gets the number of remaining string arguments
        /// </summary>
        /// <returns>Number of remaining string arguments</returns>
        public int GetRemainingStringCount()
        {
            return _stringArgs.Count;
        }
        
        /// <summary>
        /// Gets the number of remaining float arguments
        /// </summary>
        /// <returns>Number of remaining float arguments</returns>
        public int GetRemainingFloatCount()
        {
            return _floatArgs.Count;
        }
        
        /// <summary>
        /// Checks if more string arguments are available
        /// </summary>
        /// <returns>True if more string arguments are available, false otherwise</returns>
        public bool HasMoreStrings()
        {
            return _stringArgs.Count > 0;
        }
        
        /// <summary>
        /// Checks if more float arguments are available
        /// </summary>
        /// <returns>True if more float arguments are available, false otherwise</returns>
        public bool HasMoreFloats()
        {
            return _floatArgs.Count > 0;
        }
        
        /// <summary>
        /// Gets total number of string arguments
        /// </summary>
        public int StringCount => _stringArgs.Count;
        
        /// <summary>
        /// Gets total number of float arguments
        /// </summary>
        public int FloatCount => _floatArgs.Count;
        
        /// <summary>
        /// Gets total number of all arguments
        /// </summary>
        public int TotalCount => _stringArgs.Count + _floatArgs.Count;
        
        /// <summary>
        /// Parses arguments from a dictionary and sorts them into appropriate queues
        /// </summary>
        private void ParseArguments(Dictionary<string, string> arguments)
        {
            if (arguments == null)
                return;
            
            foreach (var kvp in arguments)
            {
                if (kvp.Value == null)
                    continue;
                    
                // Try to parse as different types
                if (TryParseAsFloat(kvp.Value, out float floatValue))
                {
                    _floatArgs.Enqueue(floatValue);
                }
                else
                {
                    // Fallback to string representation
                    _stringArgs.Enqueue(kvp.Value.ToString());
                }
            }
        }
        
        /// <summary>
        /// Tries to parse a value as a string
        /// </summary>
        private bool TryParseAsString(object value, out string result)
        {
            if (value is string str)
            {
                result = str;
                return true;
            }
            
            result = null;
            return false;
        }
        
        /// <summary>
        /// Tries to parse a value as a float
        /// </summary>
        private bool TryParseAsFloat(string value, out float result)
        {
            return float.TryParse(value, out result);
        }
        
        /// <summary>
        /// Sets a result message for the command execution
        /// </summary>
        /// <param name="message">The result message to set</param>
        public void SetResult(string message)
        {
            ResultMessage = message;
        }
        
        /// <summary>
        /// Sets a result message and additional data for the command execution
        /// </summary>
        /// <param name="message">The result message to set</param>
        /// <param name="data">Additional data to include in the result</param>
        public void SetResult(string message, Dictionary<string, object> data)
        {
            ResultMessage = message;
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    ResultData[kvp.Key] = kvp.Value;
                }
            }
        }
        
        /// <summary>
        /// Sets a single key-value pair in the result data
        /// </summary>
        /// <param name="key">The key for the data</param>
        /// <param name="value">The value to store</param>
        public void SetResultData(string key, object value)
        {
            ResultData[key] = value;
        }
        
        /// <summary>
        /// Returns a string representation of the arguments for debugging
        /// </summary>
        public override string ToString()
        {
            return $"GMArgs: {_stringArgs.Count} strings, {_floatArgs.Count} floats";
        }
    }
}
