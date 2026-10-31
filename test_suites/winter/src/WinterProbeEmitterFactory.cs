namespace Cthangover.Mods.RpgPlus.Tests
{
    using Cthangover.Mods.Effects.Atmosphere;
    /// <summary>A factory supplied by a separate mod assembly, proving the public author extension path.</summary>
    public class WinterProbeEmitterFactory : IAtmosphereEmitterFactory
    {
        public string Id => "winter_test/marker";
        public IAtmosphereEmitter Create(AtmosphereEmitterInfo info)
            => new WinterProbeEmitter(info.ExtraFields["Marker"].GetInt32());
    }
}
