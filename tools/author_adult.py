import bpy,math,json
from mathutils import Vector,Matrix
from pathlib import Path
out=Path(__file__).resolve().parents[1]/"Assets/CharacterSources";out.mkdir(exist_ok=True)
body=bpy.data.objects['GEO-body_male_realistic'];original=body.matrix_world.copy()
asset=next((c.asset_data for c in bpy.data.collections if c.name=='Body Male - Realistic' and c.asset_data),body.asset_data)
meta={k:getattr(asset,k,'') for k in ['author','description','license','copyright']} if asset else {}
print('ASSET_METADATA',meta,flush=True)
eyes=[o for o in bpy.data.objects if o.name.startswith('GEO-body_male_realistic.eye.')]
for eye in eyes:eye.matrix_world=original.inverted()@eye.matrix_world
for obj in list(bpy.data.objects):
 if obj!=body and obj not in eyes:bpy.data.objects.remove(obj,do_unlink=True)
body.matrix_world=Matrix.Identity(4);body.name='Body';body.hide_set(False);body.hide_viewport=False;body.hide_render=False
for m in list(body.modifiers):body.modifiers.remove(m)
for poly in body.data.polygons:poly.use_smooth=True
body.data.update()
rigdata=bpy.data.armatures.new('AdultSkeleton');rig=bpy.data.objects.new('AdultRig',rigdata);bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,a,b,parent=None):
 q=rigdata.edit_bones.new(name);q.head=a;q.tail=b
 if parent:q.parent=rigdata.edit_bones[parent]
 return q
bone('Hips',(0,0,.94),(0,0,1.05));bone('Spine',(0,0,1.05),(0,0,1.21),'Hips');bone('Chest',(0,0,1.21),(0,0,1.44),'Spine');bone('Neck',(0,0,1.44),(0,0,1.59),'Chest');bone('Head',(0,0,1.59),(0,0,1.78),'Neck')
for side,sign in [('L',1),('R',-1)]:
 def P(x,y,z):return (sign*x,y,z)
 bone('Shoulder.'+side,P(.045,0,1.43),P(.17,0,1.43),'Chest')
 bone('UpperArm.'+side,P(.17,0,1.43),P(.32,-.015,1.21),'Shoulder.'+side)
 bone('LowerArm.'+side,P(.32,-.015,1.21),P(.405,-.025,1.005),'UpperArm.'+side)
 bone('Hand.'+side,P(.405,-.025,1.005),P(.444,-.025,.925),'LowerArm.'+side)
 bone('Index1.'+side,P(.444,-.025,.925),P(.458,-.026,.88),'Hand.'+side)
 bone('Thumb1.'+side,P(.408,-.045,.98),P(.415,-.085,.945),'Hand.'+side)
 bone('UpperLeg.'+side,P(.095,0,.96),P(.105,-.015,.53),'Hips')
 bone('LowerLeg.'+side,P(.105,-.015,.53),P(.105,0,.09),'UpperLeg.'+side)
 bone('Foot.'+side,P(.105,0,.09),P(.105,-.13,.055),'LowerLeg.'+side)
 bone('Toes.'+side,P(.105,-.13,.055),P(.105,-.19,.05),'Foot.'+side)
bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO');print('WEIGHTS',len(body.vertex_groups),flush=True)
def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.85;return m
skin=material('AdultSkin',(.64,.46,.36));body.data.materials.clear();body.data.materials.append(skin)
parts=[]
def garment(name,predicate,color,inflate):
 faces=[p for p in body.data.polygons if predicate(p.center)];indices=sorted(set(v for p in faces for v in p.vertices));remap={v:i for i,v in enumerate(indices)}
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([body.data.vertices[i].co+body.data.vertices[i].normal*inflate for i in indices],[],[[remap[v] for v in p.vertices] for p in faces]);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
 for g in body.vertex_groups:obj.vertex_groups.new(name=g.name)
 for target,source in enumerate(indices):
  for g in body.data.vertices[source].groups:obj.vertex_groups[g.group].add([target],g.weight,'REPLACE')
 obj.parent=rig;mod=obj.modifiers.new('Skinning','ARMATURE');mod.object=rig;mesh.materials.append(material(name+'Cloth',color))
 for p in mesh.polygons:p.use_smooth=True
 parts.append((obj,indices));return obj
# Calculate polygon centers before the masks are sampled.
body.data.update()
garment('Shorts',lambda p:.67<p.z<1.025 and abs(p.x)<.245,(.13,.17,.20),.009)
garment('Shirt',lambda p:1.025<p.z<1.46 and abs(p.x)<(.31 if p.z>1.27 else .23),(.27,.32,.26),.013)
garment('Trousers',lambda p:.13<p.z<1.035 and abs(p.x)<.26,(.20,.24,.28),.016)
garment('Shoes',lambda p:p.z<.16,(.15,.14,.12),.018)
garment('Hair',lambda p:p.z>1.755 or (p.z>1.70 and p.y>-.015),(.055,.045,.035),.003)
for eye in eyes:
 eye.name='Eye'+('L' if eye.name.endswith('.L') else 'R');eye.parent=rig;eye.hide_set(False);eye.hide_viewport=False;eye.hide_render=False
 for m in list(eye.modifiers):eye.modifiers.remove(m)
 g=eye.vertex_groups.new(name='Head');g.add(list(range(len(eye.data.vertices))),1,'REPLACE');m=eye.modifiers.new('Skinning','ARMATURE');m.object=rig;eye.data.materials.clear();eye.data.materials.append(material(eye.name+'White',(.58,.59,.53)))
 # A small dark iris, placed on the forward surface; both share the head bone.
 center=eye.matrix_world.translation.copy();bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.006,location=center+Vector((0,-.014,0)));iris=bpy.context.object;iris.name='Iris'+eye.name;iris.scale=(1,.3,1);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);iris.parent=rig;g=iris.vertex_groups.new(name='Head');g.add(list(range(len(iris.data.vertices))),1,'REPLACE');m=iris.modifiers.new('Skinning','ARMATURE');m.object=rig;iris.data.materials.append(material(iris.name+'Dark',(.065,.07,.06)))
# Shape variants share the rig and clothing bindings.
for obj,indices in [(body,list(range(len(body.data.vertices))))]+parts[:4]:
 obj.shape_key_add(name='Basis')
 for key in ['Heavy','Gaunt']:
  shape=obj.shape_key_add(name=key)
  for i,point in enumerate(shape.data):
   p=point.co
   torso=math.exp(-((p.z-1.14)/.24)**4)*max(0,1-(abs(p.x)/.32)**6)
   if key=='Heavy':p.x*=1+.25*torso;p.y*=1+.3*torso
   else:p.x*=1-.10*torso;p.y*=1-.09*torso
# All geometry is exported; visibility is controlled by equipment in Unity.
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
 if o.type in {'MESH','ARMATURE'}:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out/'Adult.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False)
(out/'source.json').write_text(json.dumps({'source':'Blender Human Base Meshes v1.0.0','object':'GEO-body_male_realistic','metadata':meta,'body_vertices':len(body.data.vertices),'parts':[(o.name,len(o.data.vertices)) for o in bpy.data.objects if o.type=='MESH'],'source_url':'https://download.blender.org/demo/asset-bundles/human-base-meshes/human-base-meshes-bundle-v1.0.0.zip','license_url':'https://creativecommons.org/publicdomain/zero/1.0/','bundle_sha256':'46a912c0524072ac3b78c35d5d2471df7b8df102394a050ca8cd7184e3393648'},indent=2))
print('EXPORTED',str(out),flush=True)
