# ValheimInfiniteFire

This is a simple mod, it allows you to make all fireplaces, lights etc need fuel or not.

- Configs are server synced, and changes are applied immediately
- It supports added lightsources and all vanilla ones

Want to chat about the mod? Report issues?

[![discord logo](https://i.imgur.com/uE6umQE.png)](https://discord.gg/Dmr9PQTy9m) [![github logo](https://i.imgur.com/lvbP5OF.png)](https://github.com/MidnightsFX/Valheim_Armory)

## Installation
Download with your favorite mod manager, I recommend [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/).

If you need to install manually, this mod goes in the `Bepinex/plugins` folder, be sure to extract the download. Zip files are not loaded.
You also need to ensure you have [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) downloaded and installed.


## Smoke

The `[Smoke]` section has one toggle per buildable piece that produces smoke. Turning one off deactivates that
piece's smoke spawner, which drops it out of the list the game ticks every frame, so a piece with smoke off
costs nothing at all. Smoke that is already in the air finishes fading; new puffs stop immediately.

Smoke is not just decoration on some pieces, so be aware of what turning it off changes:

- Fire pits, bonfires, hearths, smelters, charcoal kilns, blast furnaces, hot tubs and battering rams use their
  own smoke for the vanilla "blocked by my own smoke" check. Turning smoke off for those means they can no
  longer choke on it. The roof and terrain checks still apply, so a buried fire pit still will not burn.
- On braziers, ovens, forges, black forges and shield generators smoke is purely cosmetic, and turning it off
  changes nothing else.
- Forges, black forges and battering rams ship with their smoke already switched off in vanilla. Their toggle
  is there for consistency but will not turn smoke on.

`[Smoke gameplay]` holds two separate switches:

- `SmokeDamage` - when false, no character takes the Smoked status effect. That covers players, tames and
  monsters, and it also clears the effect from anyone who is smoked at the moment you change the setting.
- `SmokeSuffocation` - when false, fireplaces are never reported as blocked by their own smoke, smelters never
  stall on it, and spreading fires are never put out by it. Those fires still burn out after 30 seconds and
  still die in the rain, they just get a few more chances to spread indoors first.


## Performance
- This mod runs once when you are loading into the world, it does not run constantly
- It does not change network values constantly
- Turning smoke off removes the spawner from the game's per-frame update list rather than filtering it, so it
  is not just quieter, it is genuinely less work every frame


## FAQ

How is this different than all of the other infinite fuel mods?
A. They all work slightly differently, most patch methods on the fire source that allow them to constantly add fuel, or trigger repeating scripts.

Why did I make this?
A. I wanted a clean, simple mod which provides infinite fuel to all light sources.
