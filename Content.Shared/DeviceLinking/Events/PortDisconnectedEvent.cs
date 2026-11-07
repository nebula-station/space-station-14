namespace Content.Shared.DeviceLinking.Events
{
    public sealed class PortDisconnectedEvent : EntityEventArgs
    {
        public readonly string Port;
        public readonly EntityUid RemovedPortUid; // Nebula edit

        public PortDisconnectedEvent(string port, EntityUid removedPortUid) // Nebula edit
        {
            Port = port;
            RemovedPortUid = removedPortUid; // Nebula edit
        }
    }
}
