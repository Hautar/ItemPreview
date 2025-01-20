namespace Tasks
{
    public enum ThreadOptions
    {
        None                     = 0,
        ForceMainThread          = 1,
        EnsureMainThread         = 2,
        MonoBehaviourUpdate      = 3,
        MonoBehaviourFixedUpdate = 4,
        ForceNewThread           = 5,
        MonoBehaviourLateUpdate  = 6,
    }
}
