# The woodland adventure: an authoring example

Start the `rpg_plus` collection. Choose any class in the prologue. WASD/arrows move, Shift runs, E/F interacts with the marked cell. The journal records concrete next steps.

## Play through

- Visit the elder in the western house. Ask about the missing light or take the wolf job directly.
- Gather branches and mushrooms in the forest; slimes drop living resin and coins. The merchant also sells these materials.
- Defeat the wolf at the eastern cave. It always drops the amber heart and enough fur for a mantle. Amber and the ward lantern cannot be dropped.
- Saves affected by missing victory loot can recover the amber at the wolf's cell by the cave (E/F). The pickup only appears after the wolf is beaten, while neither amber nor a lantern is held and the ward is unfinished; ordinary victories cannot duplicate it.
- At the well workbench, make salve (mushroom + resin), a warden mantle (2 fur + 2 resin), or the ward lantern (amber + 2 resin + 2 branches). Equip the mantle from the bag: it changes real combat stats.
- The herbalist by the western fence needs salve. Helping her leads to a missing courier in the southern forest; his note reveals what the forest wants. Return to her for payment and more resin.
- Return to the elder after the wolf for the lantern objective. Enter the cave behind the wolf and bring the lantern to the shrine. Return the light to the forest or negotiate a pact for the village. Both paths finish the adventure; the elder's final dialog reflects the choice.
- Collect the elder's final payment. Repeated visits cannot farm rewards. Rest at the hearth by the well to recover health and mana.

## Change it without C#

| What | Where |
| --- | --- |
| Dialogs, choices, rewards | `scenarios/rpg_plus/woodland/*.scenario` |
| NPCs, workbench, cave door, changing beacon | `map_events/<map>/*.json` |
| Journal stages | `quests/woodland.json` + `locale/*.properties` |
| Guaranteed and random monster loot | `patches/character.json` |
| Materials, consumables, crafted armor | `items/woodland.json` |
| Recipes and the station's menu | `recipes/woodland.json`, `stations/woodland.json` |
| Merchant's stock and buyback | `patches/shop.json` |
| Shrine ground and collision | `maps/rpg_woodland_shrine.json` |

The RPG map editor paints ground, collision and events. It now exposes map atmosphere and event move routes. The Data Editor exposes recipes and crafting stations through the crafting mod's schemas; its ordinary item form exposes gear traits through equipment's schemas.

`when "item('rpg_plus/salve') >= 1" -> :offer` checks the actual bag, including after load. `action item.remove id=rpg_plus/salve` consumes a delivery only after that check. Reward flags make side quests and the final payment repeat-safe. Journal stages are ordinary `quest.set_data_status` actions.

Event ids shared with `rpg_demo_game` replace its elder events in load order, while new ids add the herbalist, workbench and shrine door. This mod declares the owner's dependency. The demo still supplies the village, house and forest; village_pack supplies reusable art. All production content in this mod remains data and scenarios. Test C# is inert under `test_suites/` unless requested.

The `crafting` mod is reusable across genres. Woodland quests stay here; tile lighting, shadows, camera movement, interaction markers and atmosphere stay in `rpg`. The novel's art is unaffected.

## Rebuild art / verify

`comfy/rpg_life/make_jobs.py` queues six local ComfyUI renders through the workspace watcher. `comfy/rpg_life/build_woodland.py --install` builds the atlas, icons, feathered path edges and maps using Pillow/numpy on the host. Original build inputs are cached in `gen/rpg_life_source`, so reruns are deterministic. Generated assets are installed in the actual mods, not referenced from `gen/`.

Trees are placed as complete 3×3 stamps, without overlapping another tree, a building or an event. Only visible trunks retain interior tree collision; the shrine's stone piers and altar base have explicit collision. The workbench has its own atlas entry (39), separate from barrels (31). These rules live in the generator so an art rebuild preserves the fixes.

Tests: `woodland` / `woodland_pact` (both endings, side quests, real FF attack-menu lethal hits and victory loot, old-save amber recovery, complete tree stamps, visible collision, UI crafting, gear and save/load), existing `rpg_walk`, `rpg_plus`, `trade`, `rpg_editor_edit`, `mod_units`, `resource_atlas_rpg_plus` and `locale_atlas`. `--shots=<dir>` also captures the forest and both monster loot screens.
