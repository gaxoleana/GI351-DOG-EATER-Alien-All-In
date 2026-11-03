import re

SCENE_PATH = r"D:\WorkForgit\GI351-DOG-EATER-Alien-All-In\Assets\Scenes\Main.unity"

with open(SCENE_PATH, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update Camera background color
content = re.sub(
    r"m_BackGroundColor: \{r: [^,]+, g: [^,]+, b: [^,]+, a: 1\}",
    "m_BackGroundColor: {r: 0.97, g: 0.94, b: 0.93, a: 1}",
    content
)
content = re.sub(
    r"backgroundColor: \{r: [^,]+, g: [^,]+, b: [^,]+, a: 1\}",
    "backgroundColor: {r: 0.97, g: 0.94, b: 0.93, a: 1}",
    content
)

# 2. Update BoardIslandVisual component
new_board_visual = """--- !u!114 &446845525
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 446845522}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: c2000000000000000000000000000001, type: 3}
  m_Name: 
  m_EditorClassIdentifier: AlienAllIn.Runtime::AlienAllIn.BoardIslandVisual
  casinoTileA: {fileID: 2001, guid: c1000000000000000000000000000001, type: 3}
  casinoTileB: {fileID: 2002, guid: c1000000000000000000000000000002, type: 3}
  cliffFront: {fileID: 2003, guid: c1000000000000000000000000000003, type: 3}
  cliffCornerLeft: {fileID: 2004, guid: c1000000000000000000000000000004, type: 3}
  cliffCornerRight: {fileID: 2005, guid: c1000000000000000000000000000005, type: 3}
  cliffSideLeft: {fileID: 2006, guid: c1000000000000000000000000000006, type: 3}
  cliffSideRight: {fileID: 2007, guid: c1000000000000000000000000000007, type: 3}
  rimTop: {fileID: 2008, guid: c1000000000000000000000000000008, type: 3}
  islandShadow: {fileID: 2009, guid: c1000000000000000000000000000009, type: 3}
  characterShadow: {fileID: 2010, guid: c100000000000000000000000000000a, type: 3}
  floatingCasinoPad: {fileID: 2011, guid: c100000000000000000000000000000b, type: 3}
  casinoAmbientColor: {r: 0.97, g: 0.94, b: 0.93, a: 1}
  cliffYOffset: -0.6
  cliffSortingOrder: 2
  rimSortingOrder: 1
  charShadowSortingOrder: 4
  islandShadowSortingOrder: -15
  backgroundSortingOrder: -25
"""

# Replace existing 446845525 block
content = re.sub(
    r"--- !u!114 &446845525\nMonoBehaviour:.*?(?=--- !u!|\Z)",
    new_board_visual,
    content,
    flags=re.DOTALL
)

# 3. Update all 100 cells to use Casino_Tile_A and Casino_Tile_B
pattern = re.compile(
    r"(SpriteRenderer:.*?m_Sprite: )\{fileID: [-\d]+, guid: [a-f0-9]+, type: 3\}(.*?m_Color: )\{r: [^,]+, g: [^,]+, b: [^,]+, a: 1\}(.*?coordinate: \{x: (\d+), y: (\d+)\}.*?baseColor: )\{r: [^,]+, g: [^,]+, b: [^,]+, a: 1\}(.*?unlocked: (\d))",
    re.DOTALL
)

def replace_cell(match):
    prefix_sr = match.group(1)
    mid_sr = match.group(2)
    mid_cell = match.group(3)
    x = int(match.group(4))
    y = int(match.group(5))
    mid_unlocked = match.group(6)
    unlocked = int(match.group(7))
    
    is_even = (x + y) % 2 == 0
    sprite_id = 2001 if is_even else 2002
    guid = "c1000000000000000000000000000001" if is_even else "c1000000000000000000000000000002"
    sprite_str = f"{{fileID: {sprite_id}, guid: {guid}, type: 3}}"
    
    color_str = "{r: 1, g: 1, b: 1, a: 1}" if unlocked == 1 else "{r: 0.72, g: 0.72, b: 0.72, a: 1}"
    base_color_str = "{r: 1, g: 1, b: 1, a: 1}"
    
    return f"{prefix_sr}{sprite_str}{mid_sr}{color_str}{mid_cell}{base_color_str}{mid_unlocked}"

new_content, count = pattern.subn(replace_cell, content)
print(f"Replaced {count} cells with Casino carpet tiles!")

with open(SCENE_PATH, "w", encoding="utf-8", newline="\n") as f:
    f.write(new_content)

print("Main.unity updated with Casino 2.5D Diorama successfully!")
