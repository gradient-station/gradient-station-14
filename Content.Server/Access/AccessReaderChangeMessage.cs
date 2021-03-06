using Robust.Shared.GameObjects;

namespace Content.Server.Access
{
    public sealed class AccessReaderChangeMessage : EntityEventArgs
    {
        public IEntity Sender { get; }

        public bool Enabled { get; }

        public AccessReaderChangeMessage(IEntity entity, bool enabled)
        {
            Sender = entity;
            Enabled = enabled;
        }
    }
}
