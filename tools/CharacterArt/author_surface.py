"""Blender 4.5: refine the licensed CC0 base and bake authored surface colour.
Run: blender -b --python tools/CharacterArt/author_surface.py
Input AdultBase.fbx is preserved so rerunning never accumulates sculpt edits.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
source=root/'Assets/CharacterSources/AdultBase.fbx'
output=root/'Assets/CharacterSources/Adult.fbx'
textures=root/'Assets/Resources/Survival/Human/Surfaces';textures.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
body=bpy.data.objects['Body']
grip=body.shape_key_add(name='GripL')
direction=Vector((.053,0,-.125)).normalized();start=Vector((.426,-.025,.958));normal=Vector((0,-1,0))
for point in grip.data:
 p=point.co
 if p.x>.40 and p.z<.99:
  distance=max(0,(p-start).dot(direction));angle=min(1,distance/.10)*2.75;radius=.10/2.75
  point.co+=direction*(radius*math.sin(angle)-distance)+normal*(radius*(1-math.cos(angle)))
def gaussian(v,c,w):return math.exp(-((v-c)/w)**2)
def sculpt(p):
 # Keep all joints and the original skin bindings; define cheek, jaw and nose planes.
 x,y,z=p
 face=gaussian(z,1.66,.085)*max(0,min(1,(-y-.055)/.06))
 cheek=gaussian(abs(x),.052,.022)*gaussian(z,1.674,.025)*face
 p.y-=cheek*.003
 p.x*=1-.018*gaussian(z,1.595,.035)
 p.y-=gaussian(x,0,.012)*gaussian(z,1.663,.022)*face*.0025
 return p
for point in body.data.vertices:point.co=sculpt(point.co)
if body.data.shape_keys:
 for key in body.data.shape_keys.key_blocks:
  for point in key.data:point.co=sculpt(point.co)
 # Gaunt infected have recessed cheeks and a slightly parted jaw.
 key=body.data.shape_keys.key_blocks.get('Gaunt')
 if key:
  for point in key.data:
   p=point.co;face=max(0,min(1,(-p.y-.06)/.05))
   p.y+=.008*gaussian(abs(p.x),.05,.022)*gaussian(p.z,1.635,.035)*face
   p.z-=.008*gaussian(p.x,0,.045)*gaussian(p.z,1.586,.022)*face
for name in ['Shorts','Shirt','Trousers','Shoes','Hair']:
 obj=bpy.data.objects[name]
 # Smooth the garment boundary, independently of the anatomical source mesh.
 counts={};neighbours={}
 for face in obj.data.polygons:
  for edge in face.edge_keys:counts[edge]=counts.get(edge,0)+1
 for (a,b),count in counts.items():
  if count==1:neighbours.setdefault(a,[]).append(b);neighbours.setdefault(b,[]).append(a)
 before=[v.co.copy() for v in obj.data.vertices]
 for _ in range(8):
  updates={i:obj.data.vertices[i].co.lerp(sum((obj.data.vertices[j].co for j in around),obj.data.vertices[i].co*0)/len(around),.4) for i,around in neighbours.items()}
  for i,p in updates.items():obj.data.vertices[i].co=p
 if obj.data.shape_keys:
  for key in obj.data.shape_keys.key_blocks:
   if key.name=='Basis':continue
   for i in neighbours:key.data[i].co+=obj.data.vertices[i].co-before[i]
 def cloth(p):
  x,y,z=p
  if name in ['Shorts','Trousers']:
   # Loosen front fabric and give hems/thighs shallow folds rather than painted skin.
   front=max(0,min(1,-y/.08));p.y-=front*.012*gaussian(z,.93,.14)
   p.y+=.0025*math.sin(z*105+abs(x)*38)*gaussian(z,.73 if name=='Shorts' else .52,.15)
  elif name=='Shirt':p.y+=.0035*math.sin(z*85+x*24)*gaussian(z,1.1,.12)
  elif name=='Hair':p.z+=.0025*math.sin(x*160)*math.cos(y*110)
  return p
 for point in obj.data.vertices:point.co=cloth(point.co)
 if obj.data.shape_keys:
  for key in obj.data.shape_keys.key_blocks:
   for point in key.data:point.co=cloth(point.co)
obj=body
colour=obj.data.color_attributes.new(name='AuthoredSkin',type='FLOAT_COLOR',domain='POINT')
for v in obj.data.vertices:
 x,y,z=v.co;front=max(0,min(1,(-y-.025)/.055))
 mott=.012*math.sin(x*241+y*183+z*165)+.008*math.sin(x*91-z*103)
 c=[.57+mott,.365+mott*.7,.255+mott*.5]
 # Warm joints, lips and subtle beard/eyebrow shapes are placed on actual anatomy.
 warm=gaussian(z,.55,.055)*.06+gaussian(z,1.19,.035)*gaussian(abs(x),.32,.06)*.07
 c[0]+=warm;c[1]-=warm*.22
 lip=gaussian(x,0,.028)*gaussian(z,1.609,.007)*front
 brow=gaussian(abs(x),.031,.018)*gaussian(z,1.716,.005)*front
 beard=gaussian(z,1.585,.034)*gaussian(x,0,.055)*front*.18
 eye=gaussian(abs(x),.032,.018)*gaussian(z,1.688,.011)*front*.11
 for i in range(3):c[i]=c[i]*(1-brow*.72-beard-eye-lip*.27)+([.20,.047,.035][i])*lip*.27
 colour.data[v.index].color=(*c,1)
mat=bpy.data.materials.new('AuthoredSkinBake');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();links=mat.node_tree.links
attr=n.new('ShaderNodeVertexColor');attr.layer_name=colour.name
ao=n.new('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.10;ao.samples=16
mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.45
links.new(attr.outputs['Color'],mix.inputs[1]);links.new(ao.outputs['Color'],mix.inputs[2])
emit=n.new('ShaderNodeEmission');links.new(mix.outputs[0],emit.inputs['Color']);out=n.new('ShaderNodeOutputMaterial');links.new(emit.outputs[0],out.inputs['Surface'])
image=bpy.data.images.new('Adult authored skin',width=1024,height=1024,alpha=False)
tex=n.new('ShaderNodeTexImage');tex.image=image;n.active=tex
obj.data.materials.clear();obj.data.materials.append(mat)
for other in bpy.data.objects:
 other.select_set(False)
 if other.type=='MESH':other.hide_render=other!=body
obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.018);bpy.ops.object.mode_set(mode='OBJECT')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.bake.margin=8
bpy.ops.object.bake(type='EMIT');image.filepath_raw=str(textures/'SkinAlbedo.png');image.file_format='PNG';image.save()
for name,base in [('Shorts',(.10,.135,.16)),('Shirt',(.18,.22,.16)),('Trousers',(.12,.16,.20)),('Shoes',(.10,.085,.065)),('Hair',(.035,.025,.018))]:
 obj=bpy.data.objects[name]
 for other in bpy.data.objects:
  other.select_set(False)
  if other.type=='MESH':other.hide_render=other!=obj
 obj.select_set(True);bpy.context.view_layer.objects.active=obj
 if not obj.data.uv_layers:obj.data.uv_layers.new(name='UVMap')
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
 col=obj.data.color_attributes.new(name='AuthoredCloth',type='FLOAT_COLOR',domain='POINT')
 for v in obj.data.vertices:
  x,y,z=v.co;grain=.92+.06*math.sin(z*370+x*270)*math.sin(y*240+x*420)+.05*math.sin(z*33+x*25)
  hem=.16*gaussian(z,.68 if name=='Shorts' else 1.03 if name=='Shirt' else .14,.008)
  col.data[v.index].color=(*(max(.003,c*(grain-hem)) for c in base),1)
 cloth=mat.copy();cloth.name='Authored '+name;obj.data.materials.clear();obj.data.materials.append(cloth)
 nodes=cloth.node_tree.nodes
 next(n for n in nodes if n.type=='VERTEX_COLOR').layer_name=col.name
 pic=bpy.data.images.new(name+' albedo',width=512,height=512,alpha=False);dest=next(n for n in nodes if n.type=='TEX_IMAGE');dest.image=pic;nodes.active=dest
 bpy.ops.object.bake(type='EMIT');pic.filepath_raw=str(textures/(name+'Albedo.png'));pic.file_format='PNG';pic.save()
for other in bpy.data.objects:
 if other.type in {'MESH','ARMATURE'}:other.hide_render=False;other.select_set(True)
bpy.context.view_layer.objects.active=next(o for o in bpy.data.objects if o.type=='ARMATURE')
bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).resolve().parent/'AdultStudio.blend'))
print('AUTHORED',output,textures/'SkinAlbedo.png',flush=True)
