namespace Cthangover.Mods.RpgPlus.Tests
{
    using System;
    using System.Linq;
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Games;
    using Cthangover.Core.Items;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Core.Settings;
    using Cthangover.Core.Scenes;
    using Cthangover.Mods.Interface.View;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Dialog;
    using Cthangover.Mods.Effects.Atmosphere;
    using Cthangover.Mods.Rpg;
    using Cthangover.Mods.Rpg.Model;
    using Cthangover.Mods.Rpg.Render;
    using Cthangover.Mods.TileRender;
    using Godot;

    /// <summary>Player travel, reusable container state, source atlas planes and projected weather are checked in both views.</summary>
    public partial class WinterTestDriver : Node
    {
        private const string Slot = "slot_9927";
        private readonly string shots;
        private int step, ticks, passed, failed, coins, reward;
        private bool rewardTextChecked;
        private double elapsed;
        private Key? releaseKey;
        private static RpgWorldView View => RpgWorldHost.Current?.View;
        private static DialogBox Box => RpgInput.FindDialogBox();
        private static IInventory Bag => GameData.Instance.Runtime.Inventory;
        public WinterTestDriver(string shots)
        {
            this.shots = shots;
            Name = "WinterTestDriver";
            var timer = new Timer { WaitTime = 0.1, Autostart = true };
            timer.Timeout += Tick;
            AddChild(timer);
        }
        private void Tick()
        {
            if (step < 0) return;
            if (releaseKey.HasValue)
            {
                var key = releaseKey.Value;
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
                releaseKey = null;
            }
            elapsed += 0.1;
            if (elapsed > 120) { Check(false, "winter journey timed out at step " + step); Finish(); return; }
            try { RunStep(); }
            catch (Exception ex) { Check(false, ex.ToString()); Finish(); }
        }
        private void RunStep()
        {
            if (!Boot.IsReady) return;
            if (Box?.IsRunning == true)
            {
                var value = Box.Runtime.GetVariable("reward");
                if (!rewardTextChecked && step == 3 && value != null)
                {
                    rewardTextChecked = true;
                    reward = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    Check(reward >= 3 && reward <= 5, "rnd produces an inclusive integer reward in the live chest scenario");
                    Check(Box.GetNode<Label>("DialogBody/TextField").Text.Contains(value) && Bag.CheckCount("money/coin") == coins + reward, "localized ptext and inventory use the same single calculated value");
                }
                if (Box.IsWaitingAnswer) Box.TrySelectVariant(0); else Box.NextAction();
                return;
            }
            if (GodotSceneService.IsTransitioning || ++ticks < 3) return;
            switch (step)
            {
                case 0:
                    if (View?.Map.ID != "rpg_village") return;
                    View.PlacePlayer(1, 9, RpgDirection.Left); Tap(Key.Left); Next(); return;
                case 1:
                    if (View?.Map.ID != "rpg_desert") return;
                    Check(FindAtmosphere("desert_dust")?.EmitterCount == 1, "desert loads its data-defined dust profile");
                    Check(View.CanEnter(new Vector2I(0, 12)), "the desert's winter passage is open");
                    View.PlacePlayer(1, 12, RpgDirection.Left); Tap(Key.Left); Next(); return;
                case 2:
                    if (View?.Map.ID != "rpg_winter") return;
                    Check(View.Player.Cell == new Vector2I(38, 14), "real movement reaches winter at the safe landing cell");
                    var requested = OS.GetCmdlineUserArgs().First(a => a.StartsWith("--tile-renderer=")).Substring(16);
                    Check(View.RendererId == requested, "the requested renderer is actually active: " + requested);
                    Check(View.Map.Variant == "winter" && View.Session.Statics.Any(IsWinterOak), "the winter map dresses the pack's oak in its winter variant");
                    Check(FindAtmosphere("winter_snow")?.EmitterCount == 1, "snowfall comes from a normal extensible atmosphere profile");
                    Check(FindAtmosphere("desert_dust") == null, "leaving the desert releases its dust host");
                    Check(!View.CanEnter(new Vector2I(10, 17)) && !View.CanEnter(new Vector2I(18, 20)), "both winter fence orientations block walking");
                    Check(!View.CanEnter(new Vector2I(7, 6)), "the cabin has a solid lower footprint");
                    Check(View.CanEnter(new Vector2I(38, 14)) && View.CanEnter(new Vector2I(39, 14)), "the entrance and return cells are passable");
                    Check(Entities.Collect<RpgObject>().ContainsKey("village_pack/chest") && Entities.Collect<RpgObject>().ContainsKey("village_pack/container/barrel"), "containers are reusable pack objects");
                    VerifyPrimitives();
                    VerifyAuthorExtension();
                    VerifyExpressionExtension();
                    View.PlacePlayer(20, 14, RpgDirection.Down); step = 20; ticks = 0; return;
                case 20:
                    Shot("winter");
                    coins = Bag.CheckCount("money/coin");
                    View.PlacePlayer(27, 15, RpgDirection.Up); Check(View.Interact(), "the pack chest opens through normal interaction"); step = 3; ticks = 0; return;
                case 3:
                    Check(rewardTextChecked && Bag.CheckCount("money/coin") == coins + reward, "the game supplies the chest's calculated reward");
                    Check(View.Find("rpg_winter/winter_chest")?.Current.Parts?[0].Image == "container/chest_open", "the chest shows its open picture");
                    View.Interact(); Next(); return;
                case 4:
                    Check(Bag.CheckCount("money/coin") == coins + reward, "the open chest cannot grant rewards twice");
                    View.PlacePlayer(30, 15, RpgDirection.Up); Check(View.Interact(), "the reusable barrel opens"); Next(); return;
                case 5:
                    Check(Bag.CheckCount("money/coin") == coins + reward + 2, "the second container has its own reward");
                    Check(View.Find("rpg_winter/winter_barrel")?.Current.Parts?[0].Image == "container/barrel_open", "the barrel selects its open picture");
                    Check(View.Find("rpg_winter/winter_chest")?.StateIndex >= 0 && View.Find("rpg_winter/winter_barrel")?.StateIndex >= 0, "independent instance flags keep both containers open");
                    SaveService.Save(Slot); Check(GameSession.LoadAndEnter(this, Slot), "winter state can be saved and loaded"); Next(); return;
                case 6:
                    if (View?.Map.ID != "rpg_winter") return;
                    Check(Bag.CheckCount("money/coin") == coins + reward + 2 && View.Find("rpg_winter/winter_chest")?.StateIndex >= 0 && View.Find("rpg_winter/winter_barrel")?.StateIndex >= 0, "container loot and states survive save/load");
                    Check(FindAtmosphere("winter_snow")?.EmitterCount == 1, "snowfall is restored without accumulating emitters");
                    Shot("winter_open_containers");
                    View.PlacePlayer(38, 14, RpgDirection.Right); Tap(Key.Right); Next(); return;
                case 7:
                    if (View?.Map.ID != "rpg_desert") return;
                    Check(View.Player.Cell == new Vector2I(1, 12), "real movement returns to a safe desert landing cell");
                    Check(FindAtmosphere("winter_snow") == null && FindAtmosphere("desert_dust")?.EmitterCount == 1, "changing biome replaces snow with dust");
                    SaveService.DeleteSave(Slot); Finish(); return;
            }
        }
        private void VerifyAuthorExtension()
        {
            var host = new AtmosphereView();
            AddChild(host);
            try
            {
                host.Configure("winter_test/custom", new AtmosphereContext { Bounds=new Rect2(0,0,10,10), Project=ProbeProject });
                host.Advance(.1); host._Draw();
                Check(host.EmitterCount==1 && WinterProbeEmitter.Marker==42 && WinterProbeEmitter.Advanced==1 && WinterProbeEmitter.Drawn==1,
                    "a factory from another mod receives its custom JSON and participates in update/draw");
                host.Release();
                Check(WinterProbeEmitter.Disposed==1 && host.EmitterCount==0,"releasing an atmosphere disposes custom emitters exactly once");
            }
            finally { host.Release(); RemoveChild(host); host.Free(); }
        }
        private static Vector2 ProbeProject(Vector3 point) => new(point.X,point.Y);
        private void VerifyExpressionExtension()
        {
            var script = Cthangover.Mods.Dialog.Scenarios.ScenarioParser.Parse("cset value winter_test_double(6)+1\nend\n");
            var runtime = new DialogRuntime();
            runtime.SetDialogQueueAndRun(null, script.Queue, null);
            runtime.Run();
            Check(runtime.GetVariable("value") == "13" && runtime.LastExpressionError == null, "a function strategy from another mod is discovered and executes through cset");
        }
        private void VerifyPrimitives()
        {
            using var textures = new RpgTextures();
            using var builder = new RpgRenderSceneBuilder(textures.Image, textures.Charset);
            var scene = builder.Build(View.Map, View.Session.Cells, View.Session.Statics);
            Check(scene.Warnings.Count == 0, "every picture of the winter map resolves: " + string.Join("; ", scene.Warnings));
            var row = scene.Visuals.Where(v => v.WorldQuad != null && v.Anchor == new Vector2(10.5f, 18)).ToList();
            Check(row.Count == 1 && row[0].WorldQuad.All(p => p.Y == 17.5f), "a snowy fence segment in a row follows the x plane, without a duplicate quad");
            var column = scene.Visuals.Where(v => v.WorldQuad != null && v.Anchor == new Vector2(18.5f, 21)).ToList();
            Check(column.Count == 1 && column[0].WorldQuad.All(p => p.X == 18.5f), "a snowy fence segment in a column follows the y plane");
            var oak = View.Session.Statics.FirstOrDefault(IsWinterOak);
            Check(scene.Visuals.Any(v => oak.Id != null && v.Id.StartsWith(oak.Id + "#") && v.DrawOrder == TileDrawOrder.ByPosition), "winter trees retain their depth ordering");
        }
        private static bool IsWinterOak((string Id, RpgObject Object, int X, int Y) placed)
            => placed.Object.ID == "village_pack/vegetation/oak" && placed.Object.Parts?[0].Image == "vegetation/oak_winter";

        private AtmosphereView FindAtmosphere(string profile)
        {
            var nodes = GetTree().Root.FindChildren("*", "", true, false);
            return nodes.OfType<AtmosphereView>().FirstOrDefault(n => n.ProfileId == profile);
        }
        private void Tap(Key key)
        {
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
            releaseKey = key;
        }
        private void Shot(string name)
        {
            if (string.IsNullOrEmpty(shots) || DisplayServer.GetName() == "headless") return;
            DirAccess.MakeDirRecursiveAbsolute(shots);
            ViewportCapture.SavePng(GetViewport(), shots.TrimEnd('/') + "/" + name + ".png");
        }
        private void Next() { step++; ticks = 0; }
        private void Check(bool ok, string message)
        {
            if (ok) passed++; else failed++;
            GameLogger.Log("TEST", (ok ? "PASS " : "FAIL ") + message);
        }
        private void Finish()
        {
            step = -1;
            GameLogger.Log("TEST", $"[TEST_RESULT] name=winter passed={passed} failed={failed}");
            ApplicationExit.Quit(GetTree(), failed == 0 ? 0 : 1);
        }
    }
}
