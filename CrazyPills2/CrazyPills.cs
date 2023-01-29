using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;


namespace CrazyPills
{
    public class CrazyPills
    {
        public const string Author = "Johnodon";

        public const string Name = "Crazy Pills";

        public const string Description = "Pills are crazy";

        public const string Version = "1.0.0";

        public EventHandlers EventHandlers;

        public static CrazyPills Instance { get; private set; }

        [PluginConfig] public Config Config = new();

        public Translation Translation = new Translation();

        [PluginEntryPoint(Name, Version, Description, Author)]
        public void Start()
        {
            Instance = this;
            Log.Info("Loaded CrazyPills");
            EventManager.RegisterEvents<EventHandlers>(this);
        }

        [PluginUnload]
        public void Stop()
        {
            EventManager.UnregisterEvents(this, EventHandlers);
            Log.Info("Unloaded CrazyPills");
        }
    }
}