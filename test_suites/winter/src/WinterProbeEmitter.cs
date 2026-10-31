namespace Cthangover.Mods.RpgPlus.Tests
{
    using Cthangover.Mods.Effects.Atmosphere;
    using Godot;
    /// <summary>Observable lifecycle counters without retaining nodes, textures or renderer delegates.</summary>
    public class WinterProbeEmitter : IAtmosphereEmitter
    {
        public static int Marker, Advanced, Drawn, Disposed;
        public WinterProbeEmitter(int marker) { Marker=marker; Advanced=Drawn=Disposed=0; }
        public void Advance(double delta, AtmosphereContext context) { Advanced++; }
        public void Draw(Node2D canvas, AtmosphereContext context) { Drawn++; }
        public void Dispose() { Disposed++; }
    }
}
