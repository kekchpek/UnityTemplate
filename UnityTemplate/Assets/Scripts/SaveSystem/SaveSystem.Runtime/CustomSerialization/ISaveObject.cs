using System;

namespace kekchpek.SaveSystem.CustomSerialization
{
    public interface ISaveObject
    {
        
        /// <summary>
        /// Should be fired on any object changes to trigger SaveOnChanges system.
        /// </summary>
        event Action Changed;
        
        /// <summary>
        /// Should fulfill internal state of the object by data from stream.
        /// </summary>
        /// <param name="loadStream">The input stream, that contains data in same order, in which
        /// it was written in <see cref="Serialize"/>.</param>
        void Deserialize(ILoadStream loadStream);
        
        /// <summary>
        /// Should pass the data, that is is required for state restoring, to the save stream.
        /// This data can be taken in the same order for load stream in <see cref="Deserialize"/>.
        /// </summary>
        /// <param name="saveStream">The output stream for saving data.</param>
        void Serialize(ISaveStream saveStream);
    }
}