namespace Cthangover.Mods.RpgPlus.Tests
{
    using System.Linq;
    using Cthangover.Core.Mods;
    using Godot;

    /// <summary>Activates the opt-in winter journey; production content contains no test driver.</summary>
    public class WinterTestInitializer : IModInitializer
    {
        private static bool queued;
        public void OnModLoaded(string id) { }
        public void OnModResourcesReady()
        {
            var args = OS.GetCmdlineUserArgs();
            if (queued || !args.Contains("--itest=winter")) return;
            queued = true;
            ModRoot.AddDeferred(new WinterTestDriver(args.FirstOrDefault(a => a.StartsWith("--shots="))?.Substring(8)));
        }
    }
}
