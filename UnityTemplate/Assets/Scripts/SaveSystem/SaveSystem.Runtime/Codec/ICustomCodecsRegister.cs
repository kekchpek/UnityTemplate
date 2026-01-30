using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kekchpek.SaveSystem.Codec
{
    public interface ICustomCodecsRegister
    {
        
        void RegisterCustomCodec<T>(ICustomCodec<T> codec);

        void RemoveCustomCodec<T>();

    }
}