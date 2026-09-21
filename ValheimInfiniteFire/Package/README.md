# ValheimInfiniteFire

Choose which fires, crafting stations and cooking stations need fuel, and control smoke piece by piece.

- **Infinite fire** - fire pits, bonfires, hearths, torches, braziers, lanterns and candles never run out of fuel
- **Infinite fuel** - smelters, blast furnaces, eitr refineries, hot tubs and ovens run without fuel
- **Smoke control** - turn smoke off for any piece, stop smoke hurting anyone, and stop smoke choking fires
- **Schedule** - give torches, braziers and lanterns a time of day to go dark, and another to come back
- Every piece has its own toggle, vanilla and modded pieces alike, and changes apply right away
- Settings are synced from the server, so only admins can change them in multiplayer

Want to chat about the mod? Report issues?

[![discord logo](https://i.imgur.com/uE6umQE.png)](https://discord.gg/Dmr9PQTy9m) [![github logo](https://i.imgur.com/lvbP5OF.png)](https://github.com/MidnightsFX/Valheim_Infinite_Fuel)

## Installation
Download with your favorite mod manager, I recommend [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/).

If you need to install manually, this mod goes in the `Bepinex/plugins` folder, be sure to extract the download. Zip files are not loaded.
You also need to ensure you have [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) downloaded and installed.


## Load into a world before configuring

> **Most settings do not exist until you have loaded into a world.**

The mod builds one setting per piece by looking at every piece the game has loaded, including pieces from other
mods, and the game only loads them when you enter a world. Before that, the config file and the in-game
configuration manager only show `[Client config]` and `[Smoke gameplay]`.

1. Start the game and load into any world. A single player world is fine.
2. The `[InfiniteFire]`, `[InfiniteFuel]`, `[Smoke]` and `[Schedule]` sections are now in `BepInEx/config/MidnightsFX.InfiniteFire.cfg`
   and in the configuration manager.
3. Change them in game, or quit and edit the file.

If you add a mod with new fires or stations later, load into a world again to get settings for its pieces.
A dedicated server creates the settings when it starts up and loads its world.


## Infinite fire

`[InfiniteFire]` has one setting per fire source: fire pits, bonfires, hearths, standing and wall torches,
braziers, lanterns, candles and anything else the game treats as a fireplace. They all **default to on**.

A fire with this on never uses fuel and never goes out from running dry. It only removes fuel, so fires still go
out for every other vanilla reason: under water, buried in terrain, choked by smoke (see `SmokeSuffocation` below),
or, for fires you can switch on and off, rain and strong wind. Turning a setting off makes that fire need fuel
again, and fires already placed in the world update immediately.


## Schedule

`[Schedule]` gives a window of the in game day to stay dark to every fire you can switch on and off by using
it, which is the standing and wall torches, braziers and similar pieces. Fire pits, hearths and bonfires do not
get one, because the game gives you no way to relight those by hand, so a schedule could leave you with no way
to get one back. Turn on `EnableDebugMode` if you want the log to list exactly which pieces qualified.

Every setting starts empty, which means no schedule. Set one to a `Start-End` time range on a 24 hour clock:

```
[Schedule]
piece_groundtorch = 06:00-18:00
piece_walltorch = 22:00-04:00
```

The range is the time the fire stays **dark**, so `06:00-18:00` is a torch that burns through the night and
goes out at sunrise. Ranges may wrap past midnight. `6`, `6.5` and `06:30` are all accepted. A range whose start
matches its end is ignored, and so is anything the mod cannot read, with a warning in the log.

Midnight is `00:00`, sunrise `06:00`, noon `12:00` and sunset `18:00`, matching the sky rather than the clock on
the wall, so `06:00-18:00` really is daylight.

- **You can still light one by hand.** Walk up to a torch inside its dark window and use it, and it burns until
  the next scheduled change. Use it again to put it out. This is recorded on the piece itself, so everyone on
  the server sees that torch lit, not just whoever lit it.
- **Scheduled fires do not burn fuel while they are dark**, so a torch set to burn only at night lasts about
  twice as long. This needs no setting, it falls out of how the game already counts fuel.
- Rain and strong wind still put out an uncovered torch during its lit hours, exactly as in vanilla. The
  schedule does not fight that, and does not relight it early.
- `ScheduleCheckInterval` (default 10 seconds, admin only) is how often a fire rechecks the clock. The Valheim
  day runs faster at night, so 10 seconds puts a change up to about 20 in game minutes late after dark and 9
  minutes in daylight. Lower it if you want the change tighter, it costs almost nothing.

## Infinite fuel

`[InfiniteFuel]` has one setting per station that burns fuel.

| Station type | Examples | Default |
|---|---|---|
| Smelting stations | smelter, blast furnace, eitr refinery, hot tub | off |
| Cooking stations that burn fuel | oven | on |

Stations that never use fuel, like the windmill, spinning wheel and charcoal kiln, do not get a setting.

When infinite fuel is on for a station:

- It always counts as full, so it keeps producing no matter how fast it is set to work.
- Its fuel prompt reads "No Fuel Needed." and you cannot add more fuel by hand.
- Fuel that was already inside stays there. You get it back when you deconstruct the station, and if you turn
  infinite fuel off again the station goes back to burning it.
- Mods that fill stations from nearby chests treat it as full, so they do not keep pulling fuel out of your chests.
- Mods that speed up production, change fuel per product or change the fuel capacity keep working.


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

`[Smoke gameplay]` holds two separate switches. These exist before you load into a world.

- `SmokeDamage` (default on) - when false, no character takes the Smoked status effect. That covers players, tames and
  monsters, and it also clears the effect from anyone who is smoked at the moment you change the setting.
- `SmokeSuffocation` (default on) - when false, fireplaces are never reported as blocked by their own smoke, smelters never
  stall on it, and spreading fires are never put out by it. Those fires still burn out after 30 seconds and
  still die in the rain, they just get a few more chances to spread indoors first.


## Client config

- `EnableDebugMode` (default off) - logs every piece the mod registers and every setting change it applies. This one is
  not synced, each player sets it for themselves.


## Multiplayer

Every setting except `EnableDebugMode` is synced from the server and can only be changed by an admin while
connected. The server's values replace your local ones while you are connected, and yours come back when you
leave. Everyone playing together should have the mod installed, because stations are run by whichever player's
game is currently in charge of them.

The schedule works out which fires are dark from the shared world clock and the server's settings, so every
player sees the same torches lit at the same time. Lighting one by hand is recorded on the piece, so that is
shared too. A player without the mod installed sees every fire lit as usual.


## Compatibility

- [MyLittleUI](https://thunderstore.io/c/valheim/p/shudnal/MyLittleUI/) - the "No Fuel Needed." prompt is applied
  after MyLittleUI's station tooltips.
- Production speed and auto fuel mods - see [Infinite fuel](#infinite-fuel).


## Performance
- Settings are created once each time you load into a world
- Infinite fire uses the game's own infinite fuel flag, so fires cost nothing extra
- Infinite fuel is a quick settings lookup when a station checks its fuel. Nothing runs on a timer and fuel is never
  added over and over, and infinite fuel stations stop saving a new fuel level every second
- Turning smoke off removes the spawner from the game's per-frame update list rather than filtering it, so it
  is not just quieter, it is genuinely less work every frame
- The schedule reads the clock at most once every `ScheduleCheckInterval`, and only while a fire is loaded and
  asking. Nothing runs on a timer, nothing is written to your save, and with no schedule set it is a single
  comparison. Placing a thousand torches does not make the check any slower


## FAQ

I installed the mod but there are no fire or fuel settings?
A. Load into a world first, see [Load into a world before configuring](#load-into-a-world-before-configuring).

I changed a setting but nothing happened on the server?
A. The server's values win while you are connected. An admin has to change it, or change it in the server's config file.

How is this different than all of the other infinite fuel mods?
A. They all work slightly differently, most patch methods on the fire source that allow them to constantly add fuel, or trigger repeating scripts.

My scheduled torch is dark and I cannot tell why?
A. Look at it, the hover text says the hour it comes back on. Use it to light it early.

Why did I make this?
A. I wanted a clean, simple mod which provides infinite fuel to all light sources.
