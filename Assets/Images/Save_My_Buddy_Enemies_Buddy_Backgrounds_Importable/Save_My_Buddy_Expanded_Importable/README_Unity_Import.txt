SAVE MY BUDDY — EXPANDED UNITY IMPORT PACK

This pack contains extracted, transparent character frames and environment
pieces from the newly generated fantasy pixel-art sheet.

AI BUDDY
- Knight buddy: 20 individual PNG frames.
- Atlas: 128x128 cells, 4 columns x 5 rows.

ENEMIES
- Enemy Blob: 16 frames; 128x128 cells; 4 columns x 4 rows.\n- Enemy Brute: 6 frames; 128x128 cells; 2 columns x 3 rows.\n- Enemy Mage: 9 frames; 128x128 cells; 3 columns x 3 rows.\n- Enemy Bat: 4 frames; 128x128 cells; 2 columns x 2 rows.\n- Enemy Shadow Rogue: 10 frames; 128x128 cells; 5 columns x 2 rows.\n- Enemy Spider: 8 frames; 128x128 cells; 4 columns x 2 rows.\n- Enemy Skeleton: 7 frames; 128x128 cells; 4 columns x 2 rows.\n- Enemy Ghost: 8 frames; 128x128 cells; 3 columns x 3 rows.\n- Enemy Golem: 2 frames; 128x128 cells; 2 columns x 1 rows.\n
BACKGROUND / ENVIRONMENT
- 133 individual transparent environment pieces.
- Environment atlas uses 192x192 cells, 8 columns x 17 rows.
- Contains modular stonework, floor/wall sections, arches, pillars, doors,
  banners, chests, plants, trees, rocks, statues, and decorations.
- Background pieces are not all repeatable floor tiles; inspect before
  creating Tile assets. Use large pieces as scene props/background elements.

UNITY IMPORT — CHARACTER ATLASES
1. Copy the folders into Assets/Art/Characters.
2. Select each *_Atlas_128px.png.
3. Texture Type: Sprite (2D and UI); Sprite Mode: Multiple.
4. Filter Mode: Point (no filter); Compression: None; Mip Maps: Off.
5. Pixels Per Unit: 128 (for 128x128 frame cells).
6. Sprite Editor > Slice > Grid By Cell Size > 128 x 128 > Slice > Apply.
7. The matching Frames folder also contains each frame as an individual
   transparent 128x128 PNG if you prefer not to slice.

UNITY IMPORT — ENVIRONMENT ATLAS
1. Select Environment_Atlas_192px_8cols.png.
2. Sprite Mode: Multiple; Filter Mode: Point; Compression: None.
3. Pixels Per Unit: choose a consistent scale (e.g. 32 or 64).
4. Slice by cell size 192 x 192, then Apply.
5. For mixed-size individual pieces, import from Individual/ instead.

ANIMATION NOTE
The extracted images are arranged in the same visual scan order as the source.
They are pose/frame collections, not finished Animator Controllers or timed
clips. Create clips in Unity and check frame order and pivots before use.
Some enemy sets have a limited number of distinct poses; duplicate frames
or use sprite swaps only where appropriate.

STYLE / QUALITY NOTE
These are detailed generated pixel-art assets. Please inspect the extracted
frames in Unity: the source sheet is a concept atlas rather than a rigorously
authored animation production sheet, so some poses may not form perfectly
continuous loops. The extraction keeps the individual artwork transparent
and places each frame on a consistent cell for import.
