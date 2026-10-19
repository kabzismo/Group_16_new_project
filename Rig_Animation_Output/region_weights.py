import bpy,math,os,json
from mathutils import Vector
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_Textured_Animated.blend'))
r=bpy.data.objects['Armature.008'];m=bpy.data.objects['LowPoly'];s=bpy.context.scene
m.vertex_groups.clear()
for b in r.data.bones:
 if b.use_deform:m.vertex_groups.new(name=b.name)
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def blend(a,b,t):
 w={k:v*(1-t) for k,v in a.items()}
 for k,v in b.items():w[k]=w.get(k,0)+v*t
 return w
for v in m.data.vertices:
 p=r.matrix_world.inverted()@m.matrix_world@v.co;x=abs(p.x);z=p.z;side='L' if p.x>0 else 'R'
 # Preserve the sculpted face; blend only at the neck.
 torso=blend({'Pelvis.DEF':1},{'Spine3':1},smooth(1.03,1.43,z))
 torso=blend(torso,{'Neck':1},smooth(1.59,1.70,z))
 torso=blend(torso,{'Head':1},smooth(1.70,1.78,z))
 if z<1.08:
  leg=blend({'Foot.'+side:1},{'Shin.'+side:1},smooth(.10,.21,z))
  leg=blend(leg,{'Thigh.'+side:1},smooth(.44,.59,z))
  torso=blend(leg,torso,smooth(.91,1.065,z))
 # Arms outside the jacket silhouette. Smooth shoulder transition.
 arm=blend({'Hand.'+side:1},{'LowerArm.'+side:1},smooth(1.075,1.17,z))
 arm=blend(arm,{'UpperArm.'+side:1},smooth(1.24,1.34,z))
 threshold=.27 if z<1.40 else .215
 armfactor=smooth(threshold,threshold+.10,x)*(1-smooth(1.54,1.64,z))*smooth(.82,.94,z)
 w=blend(torso,arm,armfactor)
 for name,weight in w.items():
  if weight>1e-7:m.vertex_groups[name].add([v.index],weight,'REPLACE')
for mod in m.modifiers:
 if mod.type=='ARMATURE':mod.use_deform_preserve_volume=True
s.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_Deformation_Fixed.blend'))
for f in [1,13,25]:
 s.frame_set(f);s.render.filepath=os.path.join(out,'Skin_Test_%03d.png'%f);bpy.ops.render.render(write_still=True)
