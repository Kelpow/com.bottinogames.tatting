import bpy
import math
import mathutils
import argparse
from pathlib import Path
 
 
print ('test #0')
#command line code from: https://caretdashcaret.com/2015/05/19/how-to-run-blender-headless-from-the-command-line-without-the-gui/
def get_args():
    parser = argparse.ArgumentParser()

    # get all script args
    _, all_arguments = parser.parse_known_args()
    print(all_arguments)
    double_dash_index = all_arguments.index('--')
    print ('test #0.2')
    script_args = all_arguments[double_dash_index + 1: ]
    print ('test #1')
    # add parser rules
    parser.add_argument('-o', '--output', help="filepath to save output fbx")
    parser.add_argument('-f', '--font', help="filepath for the desired font")
    parser.add_argument('-c', '--char', help="character set to generate")
    parser.add_argument('-e', '--extrude', help="blender units to extrude the characters")
    parser.add_argument('-b', '--bevel', help="size of the bevel in blender units")
    parser.add_argument('-r', '--resolution', help="the resoltion of the font triangulation")
    parser.add_argument('-v', '--bevelres', help="the number of edges in the bevel")
    parser.add_argument('-pr', '--preview', help="whether to generate the set as individual characters or one single text object ('True' or 'False')")
    
    print('test #2')

    parsed_script_args, _ = parser.parse_known_args(script_args)
    return parsed_script_args

args = get_args()
print('test #3')

export_filepath = args.output
font_filepath = args.font
charPath = Path(args.char)
charSet = charPath.read_text()
extrude = float(args.extrude)
resolution = int(args.resolution)
bevel = float(args.bevel)
bevel_resolution = int(args.bevelres)
preview = args.preview

if font_filepath != None:
    vectorFont = bpy.data.fonts.load(font_filepath, check_existing = True)

for objectToDelete in bpy.data.objects:
    bpy.data.objects.remove(objectToDelete, do_unlink=True)
for meshToDelete in bpy.data.meshes:
    bpy.data.meshes.remove(meshToDelete, do_unlink=True)

x = 0
y = 0

if preview == 'True':
    bpy.ops.object.add(type='FONT',location=[0,0,0])
    
    obj = bpy.data.objects['Text']
    obj.name = charSet
    obj.data.body = charSet
    if font_filepath != None:
        obj.data.font = vectorFont
    
    obj.data.extrude = extrude
    obj.data.resolution_u = resolution
    obj.data.bevel_depth = bevel
    obj.data.bevel_resolution = bevel_resolution
    
    bpy.ops.object.convert(target='MESH')
    obj.data.name = charSet
else:

    #get the blender-unit width of a space by comparting the vert positions of  "c" and " c". This is the best I've got, sorry.
    tchar = charSet[0]

    bpy.ops.object.add(type='FONT',location=[0,0,0])

    tobj1 = bpy.data.objects['Text']
    tobj1.name = tchar
    tobj1.data.body = tchar
    if font_filepath != None:
        tobj1.data.font = vectorFont

    bpy.ops.object.convert(target='MESH')
    tobj1.data.name = tchar

    bpy.ops.object.add(type='FONT',location=[0,0,0])

    tobj2 = bpy.data.objects['Text']
    tobj2.name = ' ' + tchar
    tobj2.data.body = ' ' +tchar
    if font_filepath != None:
        tobj2.data.font = vectorFont

    bpy.ops.object.convert(target='MESH')
    tobj2.data.name = ' ' + tchar

    x1 = tobj1.data.vertices[0].co[0]
    x2 = tobj2.data.vertices[0].co[0]
    xd = x2 - x1

    charPath.write_text(str(xd))
    
    

    #delete temp meshes made by this process
    for objectToDelete in bpy.data.objects:
        bpy.data.objects.remove(objectToDelete, do_unlink=True)
    for meshToDelete in bpy.data.meshes:
        bpy.data.meshes.remove(meshToDelete, do_unlink=True)

    


    for char in charSet :
        bpy.ops.object.add(type='FONT',location=[x,y,0])
        x-=1
        if x < -9 :
            x=0
            y-=1
        
        obj = bpy.data.objects['Text']
        obj.name = char
        obj.data.body = char
        if font_filepath != None:
            obj.data.font = vectorFont
        
        obj.data.extrude = extrude
        obj.data.resolution_u = resolution
        obj.data.bevel_depth = bevel
        obj.data.bevel_resolution = bevel_resolution
        
        bpy.ops.object.convert(target='MESH')
        obj.data.name = char

    
mins = [100,100,100]
maxs = [-100,-100,-100]

for mesh in bpy.data.meshes:
    for vert in mesh.vertices:
        vert.co = [-vert.co[0],vert.co[1],-vert.co[2]]
        
        mins[0] = min(vert.co[0],mins[0])
        mins[1] = min(vert.co[1],mins[1])
        mins[2] = min(vert.co[2],mins[2])

        maxs[0] = max(vert.co[0],maxs[0])
        maxs[1] = max(vert.co[1],maxs[1])
        maxs[2] = max(vert.co[2],maxs[2])

diffs = [maxs[0] - mins[0], maxs[1] - mins[1], maxs[2] - mins[2]]

for mesh in bpy.data.meshes:
    localxmin = 100
    localxmax = -100
    for vert in mesh.vertices: 
        localxmin = min(vert.co[0],localxmin)
        localxmax = max(vert.co[0],localxmax)
    if not mesh.vertex_colors:
        mesh.vertex_colors.new()
    colorlayer = mesh.vertex_colors[0]
    for poly in mesh.polygons:
        for idx in poly.loop_indices:
            loopVertPos = mesh.vertices[mesh.loops[idx].vertex_index].co
            r = (loopVertPos[0] - localxmin) / (localxmax - localxmin)
            g = (loopVertPos[1] - mins[1]) / diffs[1]
            b = (loopVertPos[2] - mins[2]) / diffs[2]
            colorlayer.data[idx].color = [1.0 - r,g,b,1.0]





bpy.ops.export_scene.fbx(filepath=export_filepath,check_existing=False)