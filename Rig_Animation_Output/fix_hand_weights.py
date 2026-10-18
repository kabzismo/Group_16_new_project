import bpy,os,json
from mathutils import Vector
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_Textured_Animated.blend'))
m=bpy.data.objects['LowPoly'];r=bpy.data.objects['Armature.008'];s=bpy.context.scene
coords=[r.matrix_world.inverted()@m.matrix_world@v.co for v in m.data.vertices]
fixedhands=0;filled=0
for v,p in zip(m.data.vertices,coords):
 if abs(p.x)>.46 and .85<p.z<1.16:
  for g in m.vertex_groups:g.remove([v.index])
  m.vertex_groups['Hand.'+('L' if p.x>0 else 'R')].add([v.index],1,'REPLACE');fixedhands+=1
valid={v.index:[(w.group,w.weight) for w in v.groups if w.weight>1e-5] for v in m.data.vertices}
valid={i:w for i,w in valid.items() if w}
for v,p in zip(m.data.vertices,coords):
 if v.index not in valid:
  near=min(valid,key=lambda i:(coords[i]-p).length_squared)
  for g in m.vertex_groups:g.remove([v.index])
  total=sum(w for _,w in valid[near])
  for idx,w in valid[near]:m.vertex_groups[idx].add([v.index],w/total,'REPLACE')
  filled+=1
print('WEIGHTS_REPAIRED',fixedhands,filled)
s.frame_set(1);s.render.resolution_x=600;s.render.resolution_y=600
bpy.data.texts['README - Animation'].write('\nHand weights repaired; vertices with no effective weights bound from nearest weighted surface vertex.\n')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_Textured_Animated.blend'))
for f in range(1,49,4):
 s.frame_set(f);s.render.filepath=os.path.join(out,'Textured_Walk_%03d.png'%f);bpy.ops.render.render(write_still=True)
