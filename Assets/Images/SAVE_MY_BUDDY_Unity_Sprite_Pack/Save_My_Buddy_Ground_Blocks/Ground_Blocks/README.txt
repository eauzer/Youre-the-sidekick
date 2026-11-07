SAVE MY BUDDY — GROUND BLOCKS

Ground_Blocks_64px_9x6.png is a 9-column x 6-row atlas.
Each cell is exactly 64x64 pixels. The pack also includes each tile as an
individual transparent PNG.

Unity import:
1. Put the PNGs in Assets/Art/Tiles/Ground.
2. Select the atlas and set Texture Type = Sprite (2D and UI).
3. Sprite Mode = Multiple; Filter Mode = Point (no filter);
   Compression = None; Mip Maps = Off.
4. Open Sprite Editor > Slice > Grid By Cell Size; set 64 x 64; Slice > Apply.
5. Create Tile assets from the sliced sprites and paint with
   Window > 2D > Tile Palette.

Note: The bottom row contains some edge pieces and decorative ground details,
not all are plain repeatable floor tiles. Inspect before using as a seamless
tilemap. The top rows are dungeon floor/platform block variants.
