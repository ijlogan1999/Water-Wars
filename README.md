# Water Wars

An original 5v5 round-based tactical hero shooter: neighborhood kids, a 1990s
summer afternoon, and water toys. Nobody gets hurt, they just get soaked and
sit on the curb.

This repo is the Unity project. Right now it holds the **first offline
prototype**: a grey-box map, first-person movement, two water toys, the Soak
meter, towels, the allowance buy phase, and the full Big Splash round loop
against bots. Online play over Steam comes next.

---

## 1. Install Unity (one time)

1. Download and install **Unity Hub**: https://unity.com/download
2. Open Unity Hub, sign in (a free Personal license is fine).
3. Go to **Installs → Install Editor** and install **Unity 6.3 LTS**
   (any Unity 6 version starting with `6000.` works; LTS means "long-term
   support", the stable one). When it asks about modules, you can skip them
   all for now. Add "Windows Build Support" later when you want to make a
   build to share.

## 2. Get the project on your computer

Easiest way, with GitHub Desktop (https://desktop.github.com):

1. In GitHub Desktop: **File → Clone repository**, pick `water-wars`.
2. Remember the folder it clones into.

## 3. Open it in Unity

1. In Unity Hub: **Projects → Add → Add project from disk**, and pick the
   `water-wars` folder (the one that contains `Assets` and `Packages`).
2. Click the project to open it. **The first open takes a few minutes**
   while Unity builds its `Library` folder. That's normal.
3. If Unity Hub says the project was made with a different Unity version,
   choose your installed Unity 6 version and click **Continue / Open**.
4. If a box pops up about the **new Input System** and asks to restart, click
   **Yes**. The game works with either input setting.
5. Unity opens the scene `Assets/Scenes/Prototype.unity` by itself (it creates
   it the first time). If not, use the menu **Water Wars → Open Prototype
   Scene** at the top of the editor.

## 4. Play

1. Press the **Play** button (▶, top middle of the editor).
2. Click inside the Game view so it captures your mouse.
3. Pick a hero, then play. Press **Esc** to get your mouse back. Press Play
   again to stop.

Tip: in the Game view's top bar, turn on **Maximize On Play** for a bigger
screen.

### Controls

| Key | What it does |
| --- | --- |
| Mouse | Look around |
| Left click | Squirt |
| W A S D | Move |
| Space | Jump |
| Ctrl or C | Crouch (steadier aim) |
| Shift | Walk (steadier aim) |
| R | Reload |
| 1 / 2 | Pump-Action Soaker / Squirt Pistol |
| Q | Hero ability |
| E (hold) | Plant the Big One (attackers) or disarm it (defenders) |
| G | Drop the Big One for a teammate |
| B | Buy menu ("Lemonade Stand"), during the allowance phase |
| Enter | Skip the rest of the allowance phase |
| Tab | Scoreboard |
| Esc | Free the mouse, settings, restart |
| [ and ] | Mouse sensitivity down / up |

## 5. What's in the prototype

* **Map:** "Maple Court (grey-box)". Three houses. Attackers (you, at first)
  spawn at the Fort House at the south end. Defenders hold House A (site A
  is its Treehouse) and House B (site B is its open garage and driveway).
  A hedge with three gaps splits the middle. Only garages and treehouses can
  be entered. Sites are the light blue circles, with big **A** and **B**
  labels floating above them.
* **Soak meter:** 0 to 100. Face hits count double, leg hits count 0.75. At
  100 you're Soaked, sit on the curb, and watch a teammate until next round.
  No drying on its own mid-round.
* **Towels:** Hand Towel ($4) absorbs 25 soak, Beach Towel ($10) absorbs 50.
* **Water toys:** Squirt Pistol (free, instant hit, one click per shot) and
  Pump-Action Soaker ($26, hold to fire, visible water that arcs and falls,
  so aim a little high at long range). Normal reloads for now.
* **Two placeholder heroes** (to be replaced by your own): Balloon Kid
  (Water Balloon: thrown, bursts and soaks nearby enemies) and Juice Kid
  (Juice Box: dries you and nearby teammates by 40). Abilities are bought
  each round.
* **Big Splash:** 30 s allowance phase, 1:40 rounds, 4 s plant, 40 s fuse,
  7 s disarm with a 3.5 s checkpoint, "Streetlights" when time runs out,
  first to 7, sides swap after round 6, sudden death at 6-6.
* **Allowance:** start $8, win $30, loss $19 (+$5 per loss in a row, up to
  $29), $2 per soak, $3 to every attacker on a plant, max $90.
* **Bots:** 4 teammates and 5 opponents so you can play a full 5v5 alone.

## 6. How the code is organized

Everything lives in `Assets/WaterWars/Scripts`. Each file starts with a
plain-English note about what it does.

| File | What it does |
| --- | --- |
| `Tuning.cs` | **All the numbers.** Timers, prices, soak amounts, speeds, bot difficulty. Start here to change how the game feels. |
| `Definitions.cs` | The water toys and placeholder heroes (names, costs, stats). |
| `GameBootstrap.cs` | The starting point. Builds the light, map, kids, referee and HUD when you press Play. |
| `MapBuilder.cs` | Builds the grey-box map out of blocks. Move a fence here and it moves in the game. |
| `Kid.cs` | Anyone playing: body, Soak meter, towel, money, toys, moving and shooting. |
| `PlayerController.cs` | Turns your mouse and keyboard into actions, and runs your camera. |
| `BotBrain.cs` | The computer players. |
| `WaterShots.cs` | What happens when water leaves a toy: instant hits, arcing water, balloons, splashes. |
| `BigOne.cs` | The Big One water balloon: carried, dropped, planted, disarmed, burst. |
| `RoundManager.cs` | The referee: phases, win conditions, score, allowance, the buy menu rules. |
| `Hud.cs` | Everything drawn on screen. |
| `NavGraph.cs` | How bots find their way around the map. |
| `WWInput.cs` | Reads the keyboard and mouse (works with both of Unity's input systems). |
| `Editor/PrototypeSceneSetup.cs` | Makes the Prototype scene and the **Water Wars** menu in the editor. |

The game builds itself in code when you press Play, so the scene looks
almost empty in the editor. That's on purpose: nothing can get unhooked by
accident, and you can read the whole game in these files.

### Try a change

1. Open `Assets/WaterWars/Scripts/Tuning.cs` (double-click it in Unity's
   Project window).
2. Change `RunSpeed = 5.5f` to `RunSpeed = 8f`.
3. Save, go back to Unity, wait for the little spinner to finish, press Play.
   You're faster now.

## 7. Saving your work to GitHub

After opening the project, Unity creates a `.meta` file next to every file
and folder (it uses them to keep track of things). Commit those: they belong
in git. Unity's big `Library` and `Temp` folders are ignored on purpose.

## 8. What's next

1. Online play: the same game hosted over Steam for up to 10 friends
   (Netcode for GameObjects + Steam).
2. Your own heroes replacing the placeholders.
3. Art: your Blender models replace the grey boxes; the golden-hour cartoon
   look and the sticker-style UI from the design doc.

Design doc: "Water Wars: Game Design Doc" (Claude Docs).
