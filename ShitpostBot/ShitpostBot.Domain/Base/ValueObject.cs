using System.Collections.Generic;
using System.Linq;

namespace ShitpostBot.Domain
{
    public abstract class ValueObject<T> where T : ValueObject<T>
    {
    }
}