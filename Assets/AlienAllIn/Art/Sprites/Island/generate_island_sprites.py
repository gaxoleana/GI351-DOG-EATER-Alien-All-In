import math
import random
from PIL import Image, ImageDraw

SIZE = 1024  # Draw at 4x for smooth antialiasing
FINAL_SIZE = 256
OUT_DIR = r"D:\WorkForgit\GI351-DOG-EATER-Alien-All-In\Assets\AlienAllIn\Art\Sprites\Island"

def save_downscaled(img, filename):
    down = img.resize((FINAL_SIZE, FINAL_SIZE), Image.Resampling.LANCZOS)
    path = f"{OUT_DIR}\\{filename}"
    down.save(path, "PNG")
    print(f"Saved: {path}")

def draw_doodle_flower(draw, cx, cy, radius, pet_color, center_color=(255, 255, 255, 255), outline=(40, 55, 30, 255)):
    # 5 petals
    pet_r = radius * 0.7
    for i in range(5):
        angle = i * (2 * math.pi / 5)
        px = cx + math.cos(angle) * radius * 0.85
        py = cy + math.sin(angle) * radius * 0.85
        draw.ellipse([px - pet_r, py - pet_r, px + pet_r, py + pet_r], fill=pet_color, outline=outline, width=6)
    draw.ellipse([cx - radius * 0.6, cy - radius * 0.6, cx + radius * 0.6, cy + radius * 0.6], fill=center_color, outline=outline, width=6)

def draw_grass_fleck(draw, x, y, size=30, color=(45, 75, 35, 255)):
    # Cute little blade of grass v-shape
    draw.line([(x, y + size), (x - size * 0.4, y)], fill=color, width=8)
    draw.line([(x, y + size), (x + size * 0.5, y - size * 0.2)], fill=color, width=8)

# 1. GrassTile_A
img_a = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw_a = ImageDraw.Draw(img_a)

# Rounded rectangle grass tile
margin = 24
draw_a.rounded_rectangle([margin, margin, SIZE - margin, SIZE - margin], radius=50, fill=(116, 193, 82, 255), outline=(45, 70, 32, 255), width=12)
# Inner lighter grass highlight
draw_a.rounded_rectangle([margin + 40, margin + 40, SIZE - margin - 40, SIZE - margin - 40], radius=40, fill=(130, 204, 94, 255))
# Flowers & flecks
draw_doodle_flower(draw_a, 260, 280, 40, pet_color=(255, 175, 200, 255), center_color=(255, 245, 180, 255))
draw_doodle_flower(draw_a, 780, 720, 35, pet_color=(255, 230, 110, 255), center_color=(255, 255, 255, 255))
draw_grass_fleck(draw_a, 720, 300, 35)
draw_grass_fleck(draw_a, 320, 750, 30)
save_downscaled(img_a, "GrassTile_A.png")

# 2. GrassTile_B (Alternating shade with different flowers)
img_b = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw_b = ImageDraw.Draw(img_b)

draw_b.rounded_rectangle([margin, margin, SIZE - margin, SIZE - margin], radius=50, fill=(132, 203, 98, 255), outline=(45, 70, 32, 255), width=12)
draw_b.rounded_rectangle([margin + 40, margin + 40, SIZE - margin - 40, SIZE - margin - 40], radius=40, fill=(145, 215, 110, 255))
draw_doodle_flower(draw_b, 750, 260, 42, pet_color=(140, 215, 255, 255), center_color=(255, 255, 255, 255))
draw_doodle_flower(draw_b, 240, 740, 36, pet_color=(255, 190, 120, 255), center_color=(255, 255, 255, 255))
draw_grass_fleck(draw_b, 280, 320, 32)
draw_grass_fleck(draw_b, 740, 780, 35)
save_downscaled(img_b, "GrassTile_B.png")

# 3. Island_Cliff_Front (The 2.5D strata cliff drop-off below the tiles)
img_cliff = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw_c = ImageDraw.Draw(img_cliff)

# Background cliff fill (mauve earth strata)
# Top is y=120, bottom is y=880 (giving good height for the shallow drop-off)
# Wavy strata polygon
cliff_poly = [
    (0, 120), (SIZE, 120),
    (SIZE, 860),
    (SIZE * 0.75, 890), (SIZE * 0.5, 870), (SIZE * 0.25, 900), (0, 860)
]
draw_c.polygon(cliff_poly, fill=(132, 86, 104, 255))

# Strata layers (wavy horizontal bands)
strata_colors = [
    ((158, 111, 131, 255), 260, 40),
    ((176, 131, 151, 255), 440, 35),
    ((111, 66, 85, 255), 620, 45),
    ((84, 48, 65, 255), 780, 50),
]
for col, y_base, amp in strata_colors:
    pts = []
    for x in range(0, SIZE + 50, 50):
        y = y_base + math.sin(x * 0.008) * amp + math.cos(x * 0.015) * (amp * 0.4)
        pts.append((x, y))
    pts.extend([(SIZE, SIZE), (0, SIZE)])
    draw_c.polygon(pts, fill=col)

# Wavy strata divider pen lines
for y_base, amp in [(260, 40), (440, 35), (620, 45), (780, 50)]:
    pts = [(x, y_base + math.sin(x * 0.008) * amp + math.cos(x * 0.015) * (amp * 0.4)) for x in range(0, SIZE + 30, 30)]
    draw_c.line(pts, fill=(50, 30, 40, 200), width=10)

# Overhanging Grass Rim at the top (scalloped grass tufts)
# Grass hangs from y=0 to y=180
grass_pts = [(0, 0), (SIZE, 0)]
for x in range(SIZE, -30, -30):
    y = 120 + math.sin(x * 0.02) * 35 + (20 if (x // 60) % 2 == 0 else -15)
    grass_pts.append((x, y))
draw_c.polygon(grass_pts, fill=(116, 193, 82, 255), outline=(45, 70, 32, 255))
# Grass outline on the bottom edge
tuft_curve = [(x, 120 + math.sin(x * 0.02) * 35 + (20 if (x // 60) % 2 == 0 else -15)) for x in range(0, SIZE + 20, 20)]
draw_c.line(tuft_curve, fill=(40, 65, 28, 255), width=14)

# Bottom cliff boundary line
bot_curve = [(0, 860), (SIZE * 0.25, 900), (SIZE * 0.5, 870), (SIZE * 0.75, 890), (SIZE, 860)]
draw_c.line(bot_curve, fill=(40, 25, 32, 255), width=16)

save_downscaled(img_cliff, "Island_Cliff_Front.png")

# 4. Island_Cliff_Corner_Left (Rounded left corner cliff)
img_cl = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw_cl = ImageDraw.Draw(img_cl)

# Arc/curve on the left
cliff_left_poly = [
    (180, 120), (SIZE, 120),
    (SIZE, 860),
    (SIZE * 0.6, 880), (320, 840), (180, 680), (140, 400), (180, 120)
]
draw_cl.polygon(cliff_left_poly, fill=(132, 86, 104, 255))
for col, y_base, amp in strata_colors:
    pts = [(160, y_base)]
    for x in range(180, SIZE + 50, 50):
        y = y_base + math.sin(x * 0.008) * amp
        pts.append((x, y))
    pts.extend([(SIZE, SIZE), (160, SIZE)])
    draw_cl.polygon(pts, fill=col)

# Outer left contour line
left_edge = [(180, 120), (140, 400), (180, 680), (320, 840), (SIZE * 0.6, 880), (SIZE, 860)]
draw_cl.line(left_edge, fill=(40, 25, 32, 255), width=16)

# Grass overhang rounded corner
cgrass_pts = [(120, 0), (SIZE, 0), (SIZE, 130)]
for x in range(SIZE, 120, -30):
    y = 120 + math.sin(x * 0.02) * 30
    cgrass_pts.append((x, y))
cgrass_pts.extend([(80, 80), (120, 0)])
draw_cl.polygon(cgrass_pts, fill=(116, 193, 82, 255), outline=(40, 65, 28, 255))
save_downscaled(img_cl, "Island_Cliff_Corner_Left.png")

# 5. Island_Cliff_Corner_Right (Mirrored)
img_cr = img_cl.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
save_downscaled(img_cr, "Island_Cliff_Corner_Right.png")

# 6. Floating_Island_Mini (Small satellite island with rocket firecracker)
img_mini = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw_m = ImageDraw.Draw(img_mini)

# Mini cliff bottom
draw_m.ellipse([SIZE * 0.2, SIZE * 0.35, SIZE * 0.8, SIZE * 0.85], fill=(132, 86, 104, 255), outline=(40, 25, 32, 255), width=14)
# Mini strata
draw_m.ellipse([SIZE * 0.22, SIZE * 0.45, SIZE * 0.78, SIZE * 0.82], fill=(111, 66, 85, 255))
# Mini grass top
draw_m.ellipse([SIZE * 0.15, SIZE * 0.25, SIZE * 0.85, SIZE * 0.55], fill=(116, 193, 82, 255), outline=(40, 65, 28, 255), width=14)
draw_m.ellipse([SIZE * 0.22, SIZE * 0.3, SIZE * 0.78, SIZE * 0.5], fill=(132, 203, 98, 255))

# Tiny flowers
draw_doodle_flower(draw_m, SIZE * 0.4, SIZE * 0.4, 25, pet_color=(255, 175, 200, 255), center_color=(255, 255, 255, 255))
draw_doodle_flower(draw_m, SIZE * 0.65, SIZE * 0.42, 22, pet_color=(255, 230, 110, 255), center_color=(255, 255, 255, 255))

# Red and white striped cartoon rocket firecracker
# Stick
draw_m.line([(SIZE * 0.7, SIZE * 0.45), (SIZE * 0.7, SIZE * 0.15)], fill=(120, 80, 50, 255), width=10)
# Rocket body (cylinder tilted slightly)
draw_m.rounded_rectangle([SIZE * 0.65, SIZE * 0.18, SIZE * 0.75, SIZE * 0.38], radius=12, fill=(245, 245, 245, 255), outline=(30, 30, 30, 255), width=8)
# Red stripes
draw_m.rectangle([SIZE * 0.65, SIZE * 0.22, SIZE * 0.75, SIZE * 0.27], fill=(225, 60, 60, 255))
draw_m.rectangle([SIZE * 0.65, SIZE * 0.31, SIZE * 0.75, SIZE * 0.36], fill=(225, 60, 60, 255))
# Rocket nose cone (triangle)
draw_m.polygon([(SIZE * 0.63, SIZE * 0.18), (SIZE * 0.77, SIZE * 0.18), (SIZE * 0.70, SIZE * 0.08)], fill=(225, 60, 60, 255), outline=(30, 30, 30, 255))

save_downscaled(img_mini, "Floating_Island_Mini.png")

print("All island sprites generated successfully!")
