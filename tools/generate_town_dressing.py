#!/usr/bin/env python3
"""Create native-resolution, alpha PNG town-dressing sheets (16 px = 1 Unity unit)."""
from pathlib import Path
from PIL import Image, ImageDraw
import random

OUT = Path("Assets/Art/Environment/TownDressing")
OUT.mkdir(parents=True, exist_ok=True)
rng = random.Random(20260918)

PAL = {
    "grass": "#6b9c45", "grass_l": "#91bc58", "grass_d": "#4d763a",
    "dirt": "#b9854e", "dirt_l": "#d6a96c", "dirt_d": "#855c3c",
    "wood": "#85562f", "wood_l": "#b97940", "wood_d": "#4f3426",
    "stone": "#7e8374", "stone_l": "#b1b39a", "stone_d": "#52584f",
    "leaf": "#3e743b", "leaf_l": "#6da84a", "leaf_d": "#285233",
    "flower": "#e9d467", "pink": "#db7777", "water": "#5a9dc4",
    "shadow": "#31512d",
}

def sheet(cols, rows): return Image.new("RGBA", (cols * 16, rows * 16), (0, 0, 0, 0))
def rect(d, x, y, w, h, c): d.rectangle((x, y, x+w-1, y+h-1), fill=c)
def px(d, x, y, c): d.point((x, y), fill=c)
def grass(d, ox, oy):
    rect(d, ox, oy, 16, 16, PAL["grass"])
    for _ in range(15):
        x, y = ox+rng.randrange(16), oy+rng.randrange(16)
        px(d, x, y, PAL["grass_l"] if rng.random() < .55 else PAL["grass_d"])
def dirt(d, ox, oy):
    rect(d, ox, oy, 16, 16, PAL["dirt"])
    for _ in range(13):
        x, y = ox+rng.randrange(16), oy+rng.randrange(16)
        px(d, x, y, PAL["dirt_l"] if rng.random() < .5 else PAL["dirt_d"])
def tile(img, col, row, kind="grass"):
    d=ImageDraw.Draw(img); x,y=col*16,row*16
    (grass if kind=="grass" else dirt)(d,x,y); return d,x,y
def outline(d, box, c=PAL["wood_d"]): d.rectangle(box, outline=c)

def paths():
    im=sheet(8,4)
    # centers
    for c in range(4): tile(im,c,0,"dirt")
    # cardinal grass edges: dirt occupies tile side described
    for c, side in enumerate("NESW"):
        d,x,y=tile(im,c,1,"grass")
        if side=="N": rect(d,x,y,16,8,PAL["dirt"])
        if side=="S": rect(d,x,y+8,16,8,PAL["dirt"])
        if side=="E": rect(d,x+8,y,8,16,PAL["dirt"])
        if side=="W": rect(d,x,y,8,16,PAL["dirt"])
        for i in range(16): px(d,x+i,y+7 if side=="N" else y+8 if side=="S" else y+i, PAL["dirt_d"] if side in "NS" else PAL["grass_d"])
    # outer corners, dirt quadrant is corner
    for c,(xx,yy) in enumerate(((0,0),(8,0),(0,8),(8,8))):
        d,x,y=tile(im,c,2,"grass"); rect(d,x+xx,y+yy,8,8,PAL["dirt"])
    # inner corners: dirt except grass quadrant
    for c,(xx,yy) in enumerate(((0,0),(8,0),(0,8),(8,8))):
        d,x,y=tile(im,c,3,"dirt"); rect(d,x+xx,y+yy,8,8,PAL["grass"])
    # narrow paths, breaks, growing-in variants
    for c in range(4,8):
        d,x,y=tile(im,c,0,"grass"); rect(d,x+5,y,6,16,PAL["dirt"])
    for c in range(4,8):
        d,x,y=tile(im,c,1,"grass"); rect(d,x,y+5,16,6,PAL["dirt"])
    for c in range(4,8):
        d,x,y=tile(im,c,2,"dirt")
        for _ in range(4):
            xx,yy=x+rng.randrange(16),y+rng.randrange(16); rect(d,xx,yy,2,2,PAL["grass"])
    for c in range(4,8):
        d,x,y=tile(im,c,3,"grass"); rect(d,x+3,y+5,10,6,PAL["dirt"])
        for xx in (x+5,x+10): rect(d,xx,y+5,2,4,PAL["grass_l"])
    im.save(OUT/"town_paths.png")

def ground():
    im=sheet(8,3); d=ImageDraw.Draw(im)
    # weeds 0-3
    for c in range(4):
        x,y=c*16,0
        for i in range(3+c%2):
            bx=x+3+i*3; d.line((bx,y+14,bx+(-1 if i%2 else 1),y+7-rng.randrange(4)),fill=PAL["leaf_d"],width=1)
            d.line((bx+1,y+14,bx+2,y+9),fill=PAL["leaf_l"],width=1)
    # flowers 4-6, rock 7
    for c,col in zip(range(4,7),(PAL["flower"],PAL["pink"],"#f0d5b2")):
        x,y=c*16,0
        for cx,cy in ((6,10),(10,12),(12,8)):
            rect(d,x+cx-1,y+cy-1,3,3,col); px(d,x+cx,y+cy,PAL["dirt_d"])
    for c in range(8):
        x,y=(c%8)*16,16
        if c<3: # rocks
            rect(d,x+3,y+10,7,4,PAL["stone"]); rect(d,x+5,y+8,4,3,PAL["stone_l"]); px(d,x+4,y+13,PAL["stone_d"])
        elif c<5: # bare earth
            rect(d,x+2,y+8,12,5,PAL["dirt"]); px(d,x+6,y+9,PAL["dirt_l"]); px(d,x+11,y+11,PAL["dirt_d"])
        elif c==5: # leaves
            for xx,yy in ((4,11),(8,9),(11,12),(6,13)): rect(d,x+xx,y+yy,3,2,"#a76a36")
        elif c==6: # mushrooms
            for xx,yy in ((5,11),(10,12)): rect(d,x+xx,y+yy,2,3,"#ead9b5"); rect(d,x+xx-1,y+yy-2,4,2,"#ca7165")
        else: # stump
            rect(d,x+4,y+7,8,7,PAL["wood"]); rect(d,x+5,y+6,6,3,PAL["wood_l"]); outline(d,(x+4,y+7,x+11,y+14))
    # puddle + extra quiet variations
    for c in range(8):
        x,y=c*16,32
        if c==0: rect(d,x+2,y+9,12,5,PAL["water"]); px(d,x+5,y+9,"#9dd3e5")
        else:
            for i in range(2): d.line((x+4+i*4,y+14,x+5+i*4,y+9),fill=PAL["leaf_d"])
    im.save(OUT/"town_ground_details.png")

def vegetation():
    im=Image.new("RGBA",(192,80),(0,0,0,0)); d=ImageDraw.Draw(im)
    # trees 64x80
    for t in range(3):
        ox=t*64; rect(d,ox+28,48,8,28,PAL["wood_d"]); rect(d,ox+30,47,5,29,PAL["wood"])
        # angular canopy clusters
        for cx,cy,r in ((32,22,17),(19,35,13),(45,35,13),(31,38,19),(24,18,11),(42,18,11)):
            col=PAL["leaf_l"] if (cx+cy+t)%3==0 else PAL["leaf"]
            d.polygon([(ox+cx,cy-r),(ox+cx+r,cy),(ox+cx+r-5,cy+r),(ox+cx-r+4,cy+r),(ox+cx-r,cy)],fill=col)
        for i in range(14): px(d,ox+rng.randrange(12,53),rng.randrange(12,49),PAL["leaf_d"])
    im.save(OUT/"town_trees.png")
    im=Image.new("RGBA",(96,32),(0,0,0,0)); d=ImageDraw.Draw(im)
    for b in range(3):
        ox=b*32; d.ellipse((ox+2,9,ox+30,29),fill=PAL["leaf_d"]); d.ellipse((ox+4,5,ox+25,27),fill=PAL["leaf"]); d.ellipse((ox+11,4,ox+29,27),fill=PAL["leaf_l"])
        if b==1:
            for xx,yy in ((9,16),(18,11),(23,19)): rect(d,ox+xx,yy,2,2,PAL["flower"])
    im.save(OUT/"town_bushes.png")

def props():
    im=Image.new("RGBA",(192,64),(0,0,0,0));d=ImageDraw.Draw(im)
    # Each 32x32 cell: crate, barrel, sacks, wood, bucket, sign, bench, table, well, cart, laundry, trough
    for i in range(12):
        x=(i%6)*32;y=(i//6)*32
        if i==0:
            rect(d,x+8,y+12,16,16,PAL["wood"]);outline(d,(x+8,y+12,x+23,y+27));d.line((x+9,y+26,x+22,y+13),fill=PAL["wood_l"]);d.line((x+9,y+13,x+22,y+26),fill=PAL["wood_l"])
        elif i==1:
            rect(d,x+9,y+7,14,20,PAL["wood"]);rect(d,x+8,y+9,16,3,PAL["stone_d"]);rect(d,x+8,y+20,16,3,PAL["stone_d"]);outline(d,(x+9,y+7,x+22,y+27))
        elif i==2:
            for xx,yy in ((8,14),(15,11),(20,16)): d.ellipse((x+xx,y+yy,x+xx+9,y+yy+12),fill="#c6a176")
        elif i==3:
            for xx,yy in ((5,18),(11,14),(18,16)): d.ellipse((x+xx,y+yy,x+xx+13,y+yy+7),fill=PAL["wood_l"]);px(d,x+xx+5,y+yy+3,PAL["wood_d"])
        elif i==4:
            rect(d,x+10,y+15,12,10,PAL["wood_l"]);d.arc((x+9,y+10,x+23,y+23),180,350,fill=PAL["stone_d"]);rect(d,x+11,y+15,10,2,PAL["water"])
        elif i==5:
            rect(d,x+15,y+7,3,21,PAL["wood"]);d.polygon([(x+17,y+10),(x+27,y+13),(x+17,y+16)],fill=PAL["wood_l"]);outline(d,(x+17,y+10,x+27,y+16))
        elif i==6:
            rect(d,x+2,y+17,28,6,PAL["wood"]);rect(d,x+5,y+22,3,6,PAL["wood_d"]);rect(d,x+24,y+22,3,6,PAL["wood_d"]);rect(d,x+3,y+15,26,2,PAL["wood_l"])
        elif i==7:
            rect(d,x+5,y+16,22,8,PAL["wood"]);rect(d,x+7,y+23,3,5,PAL["wood_d"]);rect(d,x+22,y+23,3,5,PAL["wood_d"]);outline(d,(x+5,y+16,x+26,y+23))
        elif i==8:
            d.ellipse((x+5,y+12,x+27,y+29),fill=PAL["stone_d"]);d.ellipse((x+7,y+9,x+25,y+26),fill=PAL["stone"]);d.ellipse((x+10,y+12,x+22,y+19),fill=PAL["water"]);rect(d,x+6,y+20,20,3,PAL["stone_l"])
        elif i==9:
            rect(d,x+5,y+16,21,8,PAL["wood_l"]);rect(d,x+8,y+23,3,4,PAL["wood_d"]);d.ellipse((x+4,y+21,x+12,y+29),fill=PAL["stone_d"]);d.ellipse((x+21,y+21,x+29,y+29),fill=PAL["stone_d"])
        elif i==10:
            rect(d,x+3,y+8,2,20,PAL["wood"]);rect(d,x+27,y+8,2,20,PAL["wood"]);d.line((x+4,y+10,x+28,y+10),fill=PAL["wood_d"]);rect(d,x+8,y+11,7,11,"#e9e0ca");rect(d,x+17,y+11,6,11,"#8ab1bd")
        else:
            rect(d,x+2,y+16,28,9,PAL["wood"]);rect(d,x+4,y+17,24,4,PAL["water"]);rect(d,x+4,y+24,3,4,PAL["wood_d"]);rect(d,x+25,y+24,3,4,PAL["wood_d"])
    im.save(OUT/"town_props.png")

def fence():
    im=Image.new("RGBA",(224,32),(0,0,0,0));d=ImageDraw.Draw(im)
    # seven 32x32 cells: horiz, vertical, 4 corners, gate
    for i in range(7):
        x=i*32
        if i==0:
            for xx in (x+3,x+23):rect(d,xx,8,4,20,PAL["wood_l"])
            rect(d,x+4,13,22,4,PAL["wood"]);rect(d,x+4,21,22,4,PAL["wood"])
        elif i==1:
            rect(d,x+12,3,4,27,PAL["wood_l"]);rect(d,x+3,8,22,4,PAL["wood"]);rect(d,x+3,21,22,4,PAL["wood"])
        elif i<6:
            rect(d,x+4,5,4,23,PAL["wood_l"]);rect(d,x+4,21,20,4,PAL["wood"]);rect(d,x+20,12,4,13,PAL["wood_l"]);rect(d,x+7,12,15,4,PAL["wood"])
        else:
            rect(d,x+4,7,4,21,PAL["wood_l"]);rect(d,x+22,7,4,21,PAL["wood_l"]);rect(d,x+7,14,16,10,PAL["wood"]);d.line((x+8,23,x+22,15),fill=PAL["wood_l"])
    im.save(OUT/"town_fence_set_v2.png")

paths(); ground(); vegetation(); props(); fence()
