import math
from PIL import Image, ImageDraw, ImageFilter

OUT_DIR = r"D:\WorkForgit\GI351-DOG-EATER-Alien-All-In\Assets\AlienAllIn\Art\Sprites\Island"

# ==============================================================================
# 1. UNIFIED 2.5D 3D-BLOCK PLATFORM (Bomb Club Deluxe style 3D Extruded Block)
# ==============================================================================
# Canvas: 2048x2048 (drawn at 2048x2048, downscaled to 1024x1024 with PPU=168)
# Let's map exact coordinates so the 5x5 grid sits centered on the top face!
# In 1024x1024 final image:
# Center of image = (512, 512)
# Let the 5x5 grid occupy:
#   X: 162 to 862 (width = 700 px -> 140 px per cell)
#   Y: 90 to 790 (height = 700 px -> 140 px per cell, centered at y = 440)
# Wait: If we put the pivot of the sprite at (0.5, 440/1024) = (0.5, 1 - 440/1024) = (0.5, 0.5703125),
# then placing the platform at the exact center of the active grid aligns the grid cells
# with [162..862, 90..790] with 100% mathematical precision!

W = 2048
H = 2048
img_block = Image.new("RGBA", (W, H), (0, 0, 0, 0))
draw = ImageDraw.Draw(img_block)

# Grid region in 2048x2048 space:
# X: 324 to 1724 (width = 1400 px, center = 1024)
# Y: 180 to 1580 (height = 1400 px, center = 880)

def rounded_trapezoid(x_top_l, x_top_r, y_top, x_bot_l, x_bot_r, y_bot, r, steps=16):
    pts = []
    # Top-left corner
    for i in range(steps + 1):
        a = math.pi + (math.pi * 0.5) * (i / steps)
        pts.append((x_top_l + r + math.cos(a) * r, y_top + r + math.sin(a) * r))
    # Top-right corner
    for i in range(steps + 1):
        a = -math.pi * 0.5 + (math.pi * 0.5) * (i / steps)
        pts.append((x_top_r - r + math.cos(a) * r, y_top + r + math.sin(a) * r))
    # Bottom-right corner
    for i in range(steps + 1):
        a = 0 + (math.pi * 0.5) * (i / steps)
        pts.append((x_bot_r - r + math.cos(a) * r, y_bot - r + math.sin(a) * r))
    # Bottom-left corner
    for i in range(steps + 1):
        a = math.pi * 0.5 + (math.pi * 0.5) * (i / steps)
        pts.append((x_bot_l + r + math.cos(a) * r, y_bot - r + math.sin(a) * r))
    return pts

# A. Soft Ambient Drop Shadow under the entire 3D Block
shadow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
s_draw = ImageDraw.Draw(shadow_layer)
shadow_poly = rounded_trapezoid(220, 1828, 300, 110, 1938, 2000, 120)
s_draw.polygon(shadow_poly, fill=(35, 12, 22, 95))
shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(radius=28))
img_block.alpha_composite(shadow_layer)
draw = ImageDraw.Draw(img_block)

# B. Scalloped Base Skirt (wrapping around the bottom and slanted sides of the 3D block, just like Bomb Club Deluxe!)
skirt_outer = rounded_trapezoid(235, 1813, 320, 130, 1918, 1965, 105)
draw.polygon(skirt_outer, fill=(92, 18, 38, 255))

# Add organic scalloped bumps along the outer edge of the base skirt
num_bumps_bot = 18
for i in range(num_bumps_bot + 1):
    t = i / num_bumps_bot
    bx = 190 + t * (1858 - 190)
    by = 1945 + math.sin(i * 1.7) * 14
    br = 54
    draw.ellipse([bx - br, by - br, bx + br, by + br], fill=(92, 18, 38, 255))

for i in range(12):
    t = i / 11
    # Left side scallops
    lx = 235 * (1 - t) + 140 * t - 10
    ly = 380 + t * (1900 - 380)
    br = 42
    draw.ellipse([lx - br, ly - br, lx + br, ly + br], fill=(92, 18, 38, 255))
    # Right side scallops
    rx = 1813 * (1 - t) + 1908 * t + 10
    ry = 380 + t * (1900 - 380)
    draw.ellipse([rx - br, ry - br, rx + br, ry + br], fill=(92, 18, 38, 255))

# Inner lighter layer of base skirt (like the two-tone grass skirt in Bomb Club Deluxe)
skirt_inner = rounded_trapezoid(250, 1798, 340, 155, 1893, 1935, 95)
draw.polygon(skirt_inner, fill=(122, 26, 50, 255))

# Tiny decorative gold/cream casino diamonds/dots on the base skirt
for bx, by in [(340, 1950), (620, 1960), (1120, 1955), (1520, 1962), (1720, 1948), (155, 1680), (1895, 1620), (180, 1200), (1875, 1150)]:
    draw.ellipse([bx - 12, by - 12, bx + 12, by + 12], fill=(245, 200, 105, 255))
    draw.ellipse([bx - 5, by - 5, bx + 5, by + 5], fill=(255, 245, 210, 255))

# C. 3D EXTRUDED BLOCK BODY (Side Walls + Front Cliff Wall)
# Top of block is at y_top=115..1645, bottom of cliff wall is at y=1905
# Notice how the 3D block connects the top face (265..1783 at top, 195..1853 at front rim y=1645)
# straight down to the cliff base (195..1853 at y=1905)!
cliff_body = [
    (265, 160), (1783, 160),
    (1853, 1620), (1853, 1865),
    (1790, 1910), (258, 1910),
    (195, 1865), (195, 1620)
]
draw.polygon(cliff_body, fill=(138, 116, 114, 255)) # Warm taupe-stone 3D cliff base (like Bomb Club Deluxe!)

# Rounded bottom of the 3D cliff body
cliff_full = rounded_trapezoid(265, 1783, 150, 195, 1853, 1910, 85)
draw.polygon(cliff_full, fill=(140, 118, 116, 255))

# Wavy Horizontal Strata Band 1 (darker middle strata band across the front & sides)
strata1_pts = [(195, 1715)]
for x in range(195, 1860, 25):
    wy = 1735 + math.sin(x * 0.012) * 16 + math.cos(x * 0.027) * 8
    strata1_pts.append((x, wy))
strata1_pts.extend([(1853, 1810)])
for x in range(1853, 190, -25):
    wy = 1795 + math.sin(x * 0.012 + 0.4) * 16 + math.cos(x * 0.025) * 8
    strata1_pts.append((x, wy))
draw.polygon(strata1_pts, fill=(118, 96, 95, 255))

# Wavy Horizontal Strata Band 2 (lower shadow strata at base of cliff)
strata2_pts = []
for x in range(195, 1860, 25):
    wy = 1855 + math.sin(x * 0.010 + 1.2) * 14
    strata2_pts.append((x, wy))
strata2_pts.extend([(1853, 1910), (195, 1910)])
draw.polygon(strata2_pts, fill=(112, 90, 89, 255))

# Side Wall Strata (going up the left and right 3D slanted side walls!)
left_wall_band = [(265, 220), (295, 220), (225, 1680), (195, 1680)]
draw.polygon(left_wall_band, fill=(118, 96, 95, 255))
right_wall_band = [(1753, 220), (1783, 220), (1853, 1680), (1823, 1680)]
draw.polygon(right_wall_band, fill=(118, 96, 95, 255))

# Subtle pebble spots on the 3D cliff face (exact Bomb Club Deluxe style)
pebbles = [
    (360, 1690, 18, 10), (520, 1830, 22, 12), (780, 1705, 15, 9),
    (1050, 1825, 24, 11), (1340, 1695, 19, 10), (1580, 1835, 20, 11),
    (1710, 1710, 16, 9), (920, 1685, 14, 8), (1440, 1815, 17, 10)
]
for px, py, rx, ry in pebbles:
    draw.ellipse([px - rx, py - ry, px + rx, py + ry], fill=(110, 88, 87, 255))

# D. 3D TOP PLATFORM BEVEL LIP (The darker 3D rim where the top deck overhangs the cliff)
top_bevel_outer = rounded_trapezoid(262, 1786, 110, 192, 1856, 1665, 85)
draw.polygon(top_bevel_outer, fill=(72, 12, 28, 255)) # Dark 3D overhang lip

top_bevel_mid = rounded_trapezoid(265, 1783, 110, 196, 1852, 1642, 82)
draw.polygon(top_bevel_mid, fill=(98, 18, 38, 255)) # Mid bevel shade

# E. 3D TOP PLATFORM MAIN DECK (The solid top surface of the 3D cuboid!)
top_deck = rounded_trapezoid(270, 1778, 112, 204, 1844, 1618, 78)
draw.polygon(top_deck, fill=(128, 24, 48, 255)) # Rich Casino Velvet Top Deck

# Inner Gold Frame on the Top Deck surrounding the 5x5 grid area
grid_frame = rounded_trapezoid(305, 1743, 155, 275, 1773, 1590, 48)
draw.polygon(grid_frame, fill=(90, 15, 34, 255), outline=(232, 182, 92, 220), width=8)

down_block = img_block.resize((1024, 1024), Image.Resampling.LANCZOS)
down_block.save(f"{OUT_DIR}\\Casino_3D_Block.png", "PNG")
print("Saved Casino_3D_Block.png")

# ==============================================================================
# 2. 2.5D RECESSED CASINO CARPET TILES (Bomb Club Deluxe Cushion Pad Style)
# ==============================================================================
# Notice in Bomb Club Deluxe (`mid_island.png`):
# - Each tile sits INSIDE its grid cell with a visible margin so the solid 3D deck shows between tiles!
# - Each tile has soft organic rounded corners, a 3D bottom edge bevel, and clean low-contrast motifs!

TSIZE = 1024

def create_25d_casino_tile(filename, variant=0):
    t_img = Image.new("RGBA", (TSIZE, TSIZE), (0, 0, 0, 0))
    td = ImageDraw.Draw(t_img)

    # Margin around the tile so the solid 3D Block Deck shows between cells as recessed grooves!
    pad = 52

    # 1. 3D Tile Bottom Thickness Bevel / Shadow on the deck
    td.rounded_rectangle(
        [pad, pad + 18, TSIZE - pad, TSIZE - pad + 12],
        radius=95,
        fill=(64, 10, 24, 235)
    )

    # 2. Main 2.5D Cushion Tile Surface
    base_col = (156, 34, 58, 255) if variant == 0 else (144, 28, 52, 255)
    td.rounded_rectangle(
        [pad, pad, TSIZE - pad, TSIZE - pad - 14],
        radius=95,
        fill=base_col
    )

    # Create mask for clipping the inner casino carpet pattern cleanly inside the rounded pad
    mask = Image.new("L", (TSIZE, TSIZE), 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle(
        [pad + 14, pad + 14, TSIZE - pad - 14, TSIZE - pad - 28],
        radius=82,
        fill=255
    )

    pattern = Image.new("RGBA", (TSIZE, TSIZE), (0, 0, 0, 0))
    pd = ImageDraw.Draw(pattern)

    # Soft, chunky, Bomb Club Deluxe style vector casino carpet waves (NO noisy thin lines!)
    # Smooth marbled wave bands
    phase = 0.0 if variant == 0 else 2.2
    wave1 = [(0, TSIZE)]
    for x in range(0, TSIZE + 32, 32):
        y = 360 + math.sin(x * 0.006 + phase) * 140 + math.cos(x * 0.011) * 60
        wave1.append((x, y))
    wave1.append((TSIZE, TSIZE))
    pd.polygon(wave1, fill=(176, 46, 70, 255))

    wave2 = [(0, TSIZE)]
    for x in range(0, TSIZE + 32, 32):
        y = 620 + math.cos(x * 0.007 + phase) * 130 + math.sin(x * 0.012) * 50
        wave2.append((x, y))
    wave2.append((TSIZE, TSIZE))
    pd.polygon(wave2, fill=(132, 24, 46, 255))

    # Smooth gold ribbon wave (soft, warm casino carpet accent)
    gold_pts = []
    for x in range(0, TSIZE + 24, 24):
        y = 490 + math.sin(x * 0.0065 + phase) * 135
        gold_pts.append((x, y))
    pd.line(gold_pts, fill=(230, 175, 88, 175), width=36)
    pd.line([(x, y - 220) for x, y in gold_pts], fill=(212, 145, 78, 130), width=26)

    # Cute subtle geometric triangles/diamonds (matching the exact low-contrast triangle texture in Bomb Club Deluxe!)
    tri_coords = [
        (280, 280, 32), (720, 320, 28), (340, 700, 30), (680, 660, 34)
    ] if variant == 0 else [
        (320, 360, 30), (660, 260, 32), (290, 660, 28), (710, 720, 30)
    ]
    for tx, ty, ts in tri_coords:
        pd.polygon([(tx, ty - ts), (tx + ts, ty + ts * 0.7), (tx - ts, ty + ts * 0.7)], fill=(192, 62, 84, 150))

    # Tiny dainty casino star/flower accent (like the tiny flowers on the tiles in Bomb Club Deluxe)
    fx, fy = (300, 300) if variant == 0 else (700, 680)
    pd.ellipse([fx - 18, fy - 18, fx + 18, fy + 18], fill=(248, 208, 120, 220))
    pd.ellipse([fx - 8, fy - 8, fx + 8, fy + 8], fill=(255, 248, 220, 255))

    t_img.paste(pattern, (0, 0), mask)

    # Subtle top-edge 3D highlight rim on the pad
    td = ImageDraw.Draw(t_img)
    td.rounded_rectangle(
        [pad + 6, pad + 6, TSIZE - pad - 6, TSIZE - pad - 20],
        radius=88,
        outline=(215, 85, 105, 110),
        width=10
    )

    down_tile = t_img.resize((256, 256), Image.Resampling.LANCZOS)
    down_tile.save(f"{OUT_DIR}\\{filename}", "PNG")
    print(f"Saved {filename}")

create_25d_casino_tile("Casino_Tile_A.png", variant=0)
create_25d_casino_tile("Casino_Tile_B.png", variant=1)
