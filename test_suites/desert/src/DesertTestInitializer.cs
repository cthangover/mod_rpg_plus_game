namespace Cthangover.Mods.RpgPlus.Tests
{
    using System.Linq;
    using Cthangover.Core.Mods;
    using Godot;

    /// <summary>Mounts the opt-in desert playthrough without adding production gameplay code.</summary>
    public class DesertTestInitializer : IModInitializer
    {
        private static bool queued;
        public void OnModLoaded(string id) { }
        public void OnModResourcesReady()
        {
            var args = OS.GetCmdlineUserArgs();
            if (queued || !args.Contains("--itest=desert")) return;
            queued = true;
            string shots = null;
            foreach (var arg in args)
                if (arg.StartsWith("--shots=")) shots = arg.Substring(8);
            ModRoot.AddDeferred(new DesertTestDriver(shots));
        }
    }
}
