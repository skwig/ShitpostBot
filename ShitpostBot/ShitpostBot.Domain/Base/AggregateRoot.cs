using System;

namespace ShitpostBot.Domain
{
    public abstract class AggregateRoot<TId> : Entity<TId> where TId : IEquatable<TId>
    {
        
    }
}