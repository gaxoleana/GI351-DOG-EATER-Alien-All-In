import re

SCENE_PATH = r"D:\WorkForgit\GI351-DOG-EATER-Alien-All-In\Assets\Scenes\Main.unity"

with open(SCENE_PATH, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update Camera background color
content = re.sub(
    r"m_BackGroundColor: \{r: 0\.8509804, g: 0\.8509804, b: 0\.8509804, a: 1\}",
    "m_BackGroundColor: {r: 0.9882353, g: 0.9019608, b: 0.9254902, a: 1}",
    content
)
content = re.sub(
    r"backgroundColor: \{r: 0\.8509804, g: 0\.8509804, b: 0\.8509804, a: 1\}",
    "backgroundColor: {r: 0.9882353, g: 0.9019608, b: 0.9254902, a: 1}",
    content
)

# 2. Add BoardIslandVisual component to Board GameObject (446845522)
board_go_target = """--- !u!1 &446845522
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 446845524}
  - component: {fileID: 446845523}"""

board_go_replacement = """--- !u!1 &446845522
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 446845524}
  - component: {fileID: 446845523}
  - component: {fileID: 446845525}"""

if board_go_target in content:
    content = content.replace(board_go_target, board_go_replacement, 1)
    print("Added BoardIslandVisual component reference to Board GameObject")
else:
    print("Warning: Board GameObject target not found directly, checking...")

board_visual_component = """--- !u!114 &446845525
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
  grassTileA: {fileID: 1001, guid: a1000000000000000000000000000001, type: 3}
  grassTileB: {fileID: 1002, guid: a1000000000000000000000000000002, type: 3}
  cliffFront: {fileID: 1003, guid: a1000000000000000000000000000003, type: 3}
  cliffCornerLeft: {fileID: 1004, guid: a1000000000000000000000000000004, type: 3}
  cliffCornerRight: {fileID: 1005, guid: a1000000000000000000000000000005, type: 3}
  miniFloatingIsland: {fileID: 1006, guid: a1000000000000000000000000000006, type: 3}
  pastelSkyColor: {r: 0.9882353, g: 0.9019608, b: 0.9254902, a: 1}
  cliffYOffset: -0.55
  cliffSortingOrder: 2
  backgroundSortingOrder: -20
"""

board_grid_marker = "keepLockedCellsVisible: 0\n"
if board_grid_marker in content and "--- !u!114 &446845525" not in content:
    idx = content.find(board_grid_marker)
    insert_pos = idx + len(board_grid_marker)
    content = content[:insert_pos] + board_visual_component + content[insert_pos:]
    print("Inserted BoardIslandVisual component definition")

# 3. Update the 100 cells to use GrassTile_A and GrassTile_B
# Cell structure in YAML:
# Transform ...
# SpriteRenderer ...
#   m_Sprite: {fileID: 7482667652216324306, guid: 7a8bb29e59ac3d74698d577c7f8aff02, type: 3}
#   m_Color: ...
# BoardCell ...
#   coordinate: {x: X, y: Y}
#   baseColor: {r: 0.98, g: 0.98, b: 0.98, a: 1}

# Let's parse cells using regex
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
    sprite_id = 1001 if is_even else 1002
    guid = "a1000000000000000000000000000001" if is_even else "a1000000000000000000000000000002"
    sprite_str = f"{{fileID: {sprite_id}, guid: {guid}, type: 3}}"
    
    color_str = "{r: 1, g: 1, b: 1, a: 1}" if unlocked == 1 else "{r: 0.72, g: 0.72, b: 0.72, a: 1}"
    base_color_str = "{r: 1, g: 1, b: 1, a: 1}"
    
    return f"{prefix_sr}{sprite_str}{mid_sr}{color_str}{mid_cell}{base_color_str}{mid_unlocked}"

new_content, count = pattern.subn(replace_cell, content)
print(f"Replaced {count} cells with GrassTile sprites and baseColor white!")

with open(SCENE_PATH, "w", encoding="utf-8", newline="\n") as f:
    f.write(new_content)

print("Main.unity updated successfully!")
