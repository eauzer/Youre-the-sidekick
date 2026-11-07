# SAVE MY BUDDY — Unity 2D Sprite Pack

All character and projectile art is exported as transparent PNG sprite sheets.
Each animation frame is exactly **48 × 48 pixels**. Sheets use a 4-column grid;
animation names are rows, and frames play left-to-right.

## Files
- `player_sidekick.png` — idle; walk_down/up/left/right; sprint_down/up/left/right; hit
- `ai_buddy_knight.png` — same animation rows as the player
- `enemy_small_blob.png` — idle, move, hit
- `enemy_medium_thrower.png` — idle, move, attack, hit
- `enemy_large_brute.png` — idle, move, attack, hit
- `player_projectile_blue.png` — flight, impact
- `enemy_projectile_purple.png` — flight, impact
- `pickup_icons.png` — heart_full, heart_empty, heal_cross, key, coin, gem, potion

## Unity import settings
For each PNG in Unity:
1. Select the texture in the Project window.
2. Set **Texture Type** = Sprite (2D and UI).
3. Set **Sprite Mode** = Multiple.
4. Set **Pixels Per Unit** = 48.
5. Set **Filter Mode** = Point (no filter).
6. Set **Compression** = None; enable **Alpha Is Transparency**.
7. Open Sprite Editor → Slice → Grid By Cell Size → 48 × 48 → Slice → Apply.

For `pickup_icons.png`, use the same 48 × 48 grid slice. Each icon is one cell.

## Animation setup
In the Sprite Editor, each row is one animation. Select the four cells in that row
from left to right, drag them into the Scene/Animation window, and save the clip
with the row name. Suggested starting rates:
- Idle: 5 FPS
- Walk: 8 FPS
- Sprint: 12 FPS
- Hit: 10 FPS, disable Loop Time
- Enemy attack: 8 FPS; use Animation Events for projectile spawn / melee hit timing
- Projectile flight: 12 FPS; impact: 12 FPS, disable Loop Time

## Notes
- Transparent background; no baked-in labels or UI.
- The sheets use a consistent 48 × 48 cell grid and a 4-frame row layout.
- Character art is a compact prototype style. You can replace individual frames
  later without changing the animation row layout.
