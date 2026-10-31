namespace Cthangover.Mods.RpgPlus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cthangover.Core.Battle;
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Games;
    using Cthangover.Core.Items;
    using Cthangover.Core.Scenes;
    using Cthangover.Core.Settings;
    using Cthangover.Core.UI;
    using Cthangover.Core.Utils;
    using Cthangover.Mods.Dialog;
    using Cthangover.Mods.Interface.View;
    using Cthangover.Core.Mods.Resolvers;
    using Cthangover.Mods.Rpg;
    using Cthangover.Mods.Rpg.Model;
    using Cthangover.FFBattle.UI;
    using Godot;

    /// <summary>
    /// Exercises authored data through real interaction, battle rewards and save/load.
    /// Enemy HP is reduced only after the battle starts, keeping the UI victory/loot path.
    /// </summary>
    public partial class DesertTestDriver : Node
    {
        private const string Slot = "slot_9918";
        private const string VillageBarrel = "rpg_village/supply_rpg_village_19_11";
        private readonly Queue<int> choices = new(new[] { 0, 1 });
        private readonly string shots;
        private int step, ticks, passed, failed, coins, branches, elixirs, stones, resin;
        private double elapsed;
        private ulong nextAttack;
        private BattleSceneContext testedBattle;
        private Key? releaseKey;
        private static RpgWorldView View => RpgWorldHost.Current?.View;
        private static DialogBox Box => RpgInput.FindDialogBox();
        private static IInventory Bag => GameData.Instance.Runtime.Inventory;

        public DesertTestDriver(string shots)
        {
            this.shots = shots;
            Name = "DesertTestDriver";
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
            if (elapsed > 180) { Check(false, $"timeout at step {step}"); Finish(); return; }
            try { RunStep(); }
            catch (Exception ex) { Check(false, ex.ToString()); Finish(); }
        }

        private void RunStep()
        {
            var scene = GetTree().Root.GetNodeOrNull<SceneManager>("SceneManager")?.CurrentSceneName;
            if (!Boot.IsReady || string.IsNullOrEmpty(scene)) return;
            if (Box?.IsRunning == true)
            {
                if (Box.IsWaitingAnswer) Box.TrySelectVariant(choices.Count > 0 ? choices.Dequeue() : 0);
                else Box.NextAction();
                return;
            }
            if (GodotSceneService.IsTransitioning || ++ticks < 3) return;
            switch (step)
            {
                case 0:
                    if (View?.Map.ID != "rpg_village") return;
                    Check(View.Find("rpg_village/village_to_desert")?.Current.Interact?.Teleport?.Map == "rpg_desert", "west exit points to desert");
                    Check(View.CanEnter(new Vector2I(0, 9)), "west village border is passable");
                    coins = Bag.CheckCount("money/coin"); branches = Bag.CheckCount("rpg_plus/branches");
                    View.PlacePlayer(19, 12, RpgDirection.Up);
                    Check(View.Interact(), "closed supply barrel can be opened");
                    Next(); return;
                case 1:
                    Check(Bag.CheckCount("money/coin") == coins + 2 && Bag.CheckCount("rpg_plus/branches") == branches + 1, "barrel grants the authored small loot");
                    Check(View.Find(VillageBarrel)?.Current.Parts?[0].Image == "container/barrel_open", "barrel switches to open graphic");
                    View.Interact(); Next(); return;
                case 2:
                    Check(Bag.CheckCount("money/coin") == coins + 2 && Bag.CheckCount("rpg_plus/branches") == branches + 1, "opening twice does not duplicate loot");
                    SaveService.Save(Slot);
                    Check(GameSession.LoadAndEnter(this, Slot), "barrel save reloads");
                    Next(); return;
                case 3:
                    if (View?.Map.ID != "rpg_village") return;
                    Check(View.Find(VillageBarrel)?.StateIndex >= 0, "open barrel survives reload");
                    Check(Bag.CheckCount("money/coin") == coins + 2, "barrel coins survive reload without duplication");
                    View.PlacePlayer(1, 9, RpgDirection.Left); Tap(Key.Left); Next(); return;
                case 4:
                    if (View?.Map.ID != "rpg_desert") return;
                    Check(View.Player.Cell == new Vector2I(38, 12), "walking west arrives at the east desert entrance");
                    Check(View.Session.Cells.Ground(5, 5)?.ID?.StartsWith("village_pack/ground/") == true, "desert uses the pack's ground materials");
                    Check(View.Find("rpg_desert/desert_scorpion_1")?.Visible == true && View.Find("rpg_desert/desert_beetle_2")?.Visible == true, "both desert monster species are present");
                    Check(View.CanEnter(new Vector2I(39, 12)), "return passage is open");
                    Shot("desert");
                    Check(Entities.Collect<RpgObject>().TryGetValue("village_pack/ground/oasis_water", out var water) && water.Solid == true && water.Surface?.Image == "ground/oasis_water", "oasis water is a real solid water material");
                    Check(View.Map.Legend["o"] == "village_pack/ground/oasis_silt" && View.Map.Legend["e"] == "village_pack/ground/desert_path", "silt and the path are separate ground materials");
                    Check(!View.CanEnter(new Vector2I(25, 24)), "oasis water blocks walking");
                    Check(View.CanEnter(new Vector2I(20, 24)) && View.CanEnter(new Vector2I(21, 10)), "silt bank and sandy trail are walkable");
                    View.PlacePlayer(25, 20, RpgDirection.Down); step = 40; ticks = 0; return;
                case 40:
                    Shot("desert_oasis");
                    coins = Bag.CheckCount("money/coin"); elixirs = Bag.CheckCount("rpg_plus/elixir");
                    View.PlacePlayer(17, 9, RpgDirection.Up); Check(View.Interact(), "desert supply barrel opens"); step = 5; ticks = 0; return;
                case 5:
                    Check(Bag.CheckCount("money/coin") == coins + 3 && Bag.CheckCount("rpg_plus/elixir") == elixirs + 1, "desert barrel grants coins and an elixir");
                    View.Interact(); Next(); return;
                case 6:
                    Check(Bag.CheckCount("money/coin") == coins + 3 && Bag.CheckCount("rpg_plus/elixir") == elixirs + 1, "desert barrel is repeat safe");
                    stones = Bag.CheckCount("rpg_plus/stones");
                    View.PlacePlayer(26, 12, RpgDirection.Right);
                    Check(View.Interact(), "scorpion interaction starts its battle"); Next(); return;
                case 7:
                    if (!Win("rpg_desert_scorpion")) return;
                    Next(); return;
                case 8:
                    if (!ContinueVictory()) return;
                    Next(); return;
                case 9:
                    if (View?.Map.ID != "rpg_desert") return;
                    Check(View.Find("rpg_desert/desert_scorpion_1")?.Visible == false, "defeated scorpion disappears");
                    Check(Bag.CheckCount("rpg_plus/stones") >= stones + 1, "scorpion victory grants its loot");
                    resin = Bag.CheckCount("rpg_plus/resin");
                    View.PlacePlayer(22, 14, RpgDirection.Up);
                    Check(View.Interact(), "beetle interaction starts its battle"); Next(); return;
                case 10:
                    if (!Win("rpg_desert_beetle")) return;
                    Next(); return;
                case 11:
                    if (!ContinueVictory()) return;
                    Next(); return;
                case 12:
                    if (View?.Map.ID != "rpg_desert") return;
                    Check(View.Find("rpg_desert/desert_beetle_2")?.Visible == false, "defeated beetle disappears");
                    Check(Bag.CheckCount("rpg_plus/resin") >= resin + 1, "beetle grants its loot");
                    SaveService.Save(Slot);
                    Check(GameSession.LoadAndEnter(this, Slot), "desert progress reloads");
                    Next(); return;
                case 13:
                    if (View?.Map.ID != "rpg_desert") return;
                    Check(View.Find("rpg_desert/desert_scorpion_1")?.Visible == false && View.Find("rpg_desert/desert_beetle_2")?.Visible == false, "defeated monsters stay gone after reload");
                    Check(View.Find("rpg_desert/desert_barrel_supply")?.StateIndex >= 0, "desert barrel stays open after reload");
                    View.PlacePlayer(38, 12, RpgDirection.Right); Tap(Key.Right); Next(); return;
                case 14:
                    if (View?.Map.ID != "rpg_village") return;
                    Check(View.Player.Cell == new Vector2I(1, 9), "east desert exit returns inside west village edge");
                    Check(View.Find(VillageBarrel)?.StateIndex >= 0, "village barrel remains open after the round trip");
                    SaveService.DeleteSave(Slot); Finish(); return;
            }
        }

        private bool Win(string id)
        {
            if (GetTree().CurrentScene is not BattleSceneContext battle) return false;
            if (battle.IsDestroyed) return true;
            if (battle.ActiveBattleCore is not IBattleAutoPlayable auto || !auto.IsWaitingForPlayerInput || Time.GetTicksMsec() < nextAttack) return false;
            var enemies = battle.FindChild("FFEnemyPanel", true, false) as FFEnemyPanel;
            var players = battle.FindChild("FFPlayerPanel", true, false) as FFPlayerPanel;
            var menu = battle.FindChildren("*", "", true, false).OfType<FFMenuPanel>().FirstOrDefault();
            if (enemies == null || players == null || menu == null) return false;
            if (testedBattle != battle)
            {
                testedBattle = battle;
                Check(enemies.Widgets.Count == 1 && enemies.Widgets[0].Card.ID == id, "battle contains " + id);
                foreach (var enemy in enemies.Widgets) { enemy.Card.Attributes.Defence.Value = 0; enemy.Card.Attributes.Health.Value = 1; }
            }
            var target = enemies.Widgets.FirstOrDefault(w => !w.IsDead);
            var player = players.Widgets.FirstOrDefault(w => !w.IsDead);
            if (target == null || player == null) return false;
            player.Card.Attributes.Point.Value = 100;
            Click(player);
            menu.SelectedIndex = menu.Entries.FindIndex(e => e.Key == "attack"); menu.ConfirmSelection();
            menu.SelectedIndex = menu.Entries.FindIndex(e => e.Data is Cthangover.Core.Characters.ActionCharacter action && action.Type == Cthangover.Core.Characters.ActionCharacterType.ToEnemy);
            menu.ConfirmSelection(); Click(target); nextAttack = Time.GetTicksMsec() + 1600;
            return false;
        }

        private bool ContinueVictory()
        {
            if (GetTree().Root.FindChild("ContinueButton", true, false) is not Button button || !button.IsVisibleInTree()) return false;
            button.EmitSignal(BaseButton.SignalName.Pressed); return true;
        }
        private static void Click(Control c) => c.EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        private void Tap(Key key)
        {
            // Movement samples held input in _Process; retain the press until
            // the next timer tick so at least one real simulation frame sees it.
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
            GameLogger.Log("TEST", $"[TEST_RESULT] name=desert passed={passed} failed={failed}");
            Cthangover.Core.Bootstrap.ApplicationExit.Quit(GetTree(), failed == 0 ? 0 : 1);
        }
    }
}
