using System.Collections.Generic;
using System.Linq;

namespace ShitpostBot.Domain
{
    public abstract record ValueObject<T> where T : ValueObject<T>
    {
    }
}