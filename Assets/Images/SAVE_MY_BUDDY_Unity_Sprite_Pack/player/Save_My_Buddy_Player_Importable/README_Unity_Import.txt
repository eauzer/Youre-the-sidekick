SAVE MY BUDDY — PLAYER SPRITES (UNITY-IMPORTABLE)

Included files
- Player_Atlas_128px_8cols.png: extracted character frames in a uniform 128x128 grid.
- Player_Frame_01.png ... Player_Frame_75.png: individual transparent frames (no slicing required).
- Player_Original_Transparent_Sheet.png: original generated artwork sheet with its transparency channel.
- Player_Frame_Preview.jpg: visual overview.

ATLAS DETAILS
- Cell size: 128 x 128 pixels.
- Columns: 8.
- Rows: 10.
- Frame count: 75.
- Transparent RGBA background.
- Frames are centered horizontally and bottom-aligned to keep a consistent pivot.

UNITY IMPORT — ATLAS
1. Copy PNGs into Assets/Art/Player/.
2. Select Player_Atlas_128px_8cols.png in Unity.
3. Texture Type: Sprite (2D and UI). Sprite Mode: Multiple.
4. Filter Mode: Point (no filter). Compression: None. Mip Maps: Off.
5. Open Sprite Editor > Slice > Grid By Cell Size. Set 128 x 128. Slice, then Apply.
6. Select frames for an animation in the Project window and drag them into the Animation window.

UNITY IMPORT — INDIVIDUAL FRAMES
You can also drag any Player_Frame_XX.png directly into a scene or Animation window. Each is already an individual Sprite; no slicing is needed.

IMPORTANT
The extracted frames follow the order in the generated source sheet, left-to-right and top-to-bottom. The source artwork contains different poses/actions but does not provide a reliable authored animation naming map. Preview the frames and group the desired poses into Idle/Walk/Run/Attack clips yourself. Some poses may repeat.
