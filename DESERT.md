# Desert expansion

From the village, walk west along the existing horizontal road to cell (0,9).
This enters `rpg_desert` at (38,12). Its eastern edge at (39,12) returns to
village (1,9); the landing cells are outside the trigger cells.

The 40x24 map uses the separate `desert` tileset: sand and rocky surface patches,
an adobe house, a caravan shelter, a ruined tower, date palms, cacti and scrub.
Buildings are complete multi-cell compositions. Their occupied footprints
block movement; no interior map is provided. Door/notice interactions are at
the front of the buildings.

Five visible monsters use two new character templates:
`rpg_desert_scorpion` and `rpg_desert_beetle`. Interact with E/F to fight them.
Victories grant coins and small resources through normal battle loot and set
per-monster flags; defeated monsters stay gone across save/load.

Original barrel tiles were converted to actor events in the village and
elder's house. Two more supply barrels are beside village buildings and
three are in the desert. Every barrel has independent closed/open pages,
its own saved flag, a distinct open sprite, and one-time small rewards.
Reopening only displays the empty message. Existing chests are unchanged.
Barrel placements: `placements/<map>/barrels.json` and desert `events.json` (rpg_placement, model v2).

## Art choices

The map uses the `desert` tileset (built-in image_gen source). Two alternative
ComfyUI renders with identical logical ids (`qwen_image_21_clean_00110_.png`,
`qwen_image_21_clean_00111_.png`) were used by no map and were removed from the
mod on 2026-10-29; their installed tilesets are archived in
`../art_sources/cthangover/village_pack/retired_tilesets_2026-10-29/`, and
`build_desert_v1.cjs` still builds them into `gen/desert_v1/<name>/` for comparison.
Installing one again as a tileset swaps the desert art without moving buildings or
changing event/collision coordinates.

The original ComfyUI PNGs and embedded workflows/prompts are archived under
`gen/desert_v1/`. Background extraction used built-in image_gen; its separate
outputs and exact extraction prompts are beside the originals. Full-resolution
selected sheets and individual alternative objects live in
`../art_sources/cthangover/village_pack/art_sources/desert_qwen110/` and `desert_qwen111/`.
The main source sheets are in `art_sources/desert_v1/`.
Prompts for the main generation are in `comfy/rpg_life/desert_v1_prompts.json`.

`comfy/rpg_life/build_desert_v1.cjs` assembles images, charsets, metadata and the
authored content (Node with `sharp` required). It also writes
`gen/desert_v1/map_preview.png` and `variants_preview.png`.
It is an authoring builder: rerunning restores its authored map placements.
It does not modify game collection definitions.

## Verification

`--itest=desert` / TestRunner `--test desert` checks one-time barrel rewards,
save/load, both transitions by real movement, both monster battles and loot,
and persistent defeated-monster/open-barrel states. Optional
`--shots=<directory>` captures the desert in a windowed run.
The test uses the reserved save slot `slot_9918` and removes it on success.

2026-10-28 host verification: desert 29/29, rpg_walk 34/34,
resource_atlas_rpg_plus 39/39, locale_atlas 89/89 (zero locale errors).
The existing shutdown RID diagnostics are separate from those passing assertions.
