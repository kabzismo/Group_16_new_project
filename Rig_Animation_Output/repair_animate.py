import bpy,math,json,os
from mathutils import Vector,Quaternion
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=r'C:\Users\alwan\Documents\blender stuff\Alwande_Shangase_Group16_A2_NPC\working blender file\Rig2.blend')
if bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
rig=bpy.data.objects['Armature.008']; mesh=bpy.data.objects['LowPoly']; scene=bpy.context.scene
# Consolidate duplicate control weights into the intended deformation bones.
merged=0
for g in list(mesh.vertex_groups):
 if g.name.startswith('FK.'):
  target=mesh.vertex_groups.get(g.name[3:])
  if target:
   for v in mesh.data.vertices:
    weights={w.group:w.weight for w in v.groups}
    if g.index in weights:
     target.add([v.index],min(1,weights.get(target.index,0)+weights[g.index]),'REPLACE');merged+=1
   mesh.vertex_groups.remove(g)
for b in rig.data.bones:
 if b.name.startswith(('FK.','IK.')) or b.name.startswith('IKHand'): b.use_deform=False
for v in mesh.data.vertices:
 ws=[(w.group,w.weight) for w in v.groups if rig.data.bones.get(mesh.vertex_groups[w.group].name) and rig.data.bones[mesh.vertex_groups[w.group].name].use_deform]
 total=sum(w for _,w in ws)
 if total:
  for idx,w in ws: mesh.vertex_groups[idx].add([v.index],w/total,'REPLACE')
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb=rig.data.edit_bones
eb['Spine1'].parent=eb['bone.Pelvis']
for s in ['L','R']: eb['FK.Thigh.'+s].parent=eb['Pelvis.DEF']
bpy.ops.object.mode_set(mode='OBJECT')
for p in rig.pose.bones:
 p.location=(0,0,0);p.rotation_mode='QUATERNION';p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
 for c in p.constraints:
  if c.type=='IK': c.use_stretch=False
for o in bpy.data.objects:
 if o.name.startswith('CNTRL_') or o.type=='EMPTY' or o.name=='HIGHPOLY': o.hide_render=True;o.hide_set(True)
switch=rig.pose.bones['FK/IKSwitch']
for key in switch.keys(): switch[key]=1.0 if 'Hand' in key else 0.0
rig.show_in_front=True
# Key local translations from armature-space offsets and rotations from world axes.
def loc(name,xyz):
 p=rig.pose.bones[name];p.location=p.bone.matrix_local.to_3x3().inverted()@Vector(xyz)
def rot(name,axis,angle):
 p=rig.pose.bones[name];axislocal=p.bone.matrix_local.to_3x3().inverted()@Vector(axis);p.rotation_quaternion=Quaternion(axislocal,angle)
controls=['bone.Pelvis','Spine2','Spine3','Neck','Head']+[n+s for s in ['L','R'] for n in ['IK.Control.Foot.','FK.UpperArm.','FK.LowerArm.']]
rig.animation_data_create()
for name,period in [('Idle_Loop',96),('Walk_Loop',48)]:
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
 for f in range(1,period+2):
  t=2*math.pi*(f-1)/period
  for p in rig.pose.bones: p.location=(0,0,0);p.rotation_quaternion=(1,0,0,0)
  if name=='Idle_Loop':
   loc('bone.Pelvis',(.006*math.sin(t),0,-.035+.004*math.cos(t)))
   rot('Spine3',(1,0,0),.012*math.sin(t));rot('Head',(0,0,1),.025*math.sin(t));rot('Neck',(1,0,0),-.008*math.sin(t))
  else:
   loc('bone.Pelvis',(.012*math.sin(t),0,-.045+.012*math.cos(2*t)))
   rot('bone.Pelvis',(0,0,1),.035*math.sin(t));rot('Spine3',(0,0,1),-.05*math.sin(t));rot('Spine2',(1,0,0),.025)
  for s,sign in [('L',1),('R',-1)]:
   phase=t+(0 if s=='L' else math.pi)
   if name=='Walk_Loop':
    u=((f-1)/period+(0 if s=='L' else .5))%1
    # Linear planted stance, smooth raised recovery, in-place locomotion.
    y=-.17+.68*u if u<.5 else .17-.34*((u-.5)*2)
    z=0 if u<.5 else .075*math.sin(math.pi*(u-.5)*2)**2
    loc('IK.Control.Foot.'+s,(0,y,z))
   rot('FK.UpperArm.'+s,(0,1,0),sign*.28)
   p=rig.pose.bones['FK.UpperArm.'+s]
   ax=p.bone.matrix_local.to_3x3().inverted()@Vector((1,0,0))
   p.rotation_quaternion=Quaternion(ax,(.16*math.cos(phase) if name=='Walk_Loop' else .018*math.sin(phase)))*p.rotation_quaternion
   rot('FK.LowerArm.'+s,(1,0,0),-.10)
  for n in controls:
   p=rig.pose.bones[n];p.keyframe_insert('location',frame=f,group=n);p.keyframe_insert('rotation_quaternion',frame=f,group=n)
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:
     for k in fc.keyframe_points:k.interpolation='LINEAR'
     fc.modifiers.new('CYCLES')
rig.animation_data.action=bpy.data.actions['Walk_Loop']
scene.render.fps=24;scene.frame_start=1;scene.frame_end=48;scene.frame_set(1)
# Dedicated clean preview camera.
camdata=bpy.data.cameras.new('Animation Preview');cam=bpy.data.objects.new('Animation Preview',camdata);scene.collection.objects.link(cam)
cam.location=(13,-26,11);cam.rotation_euler=(Vector((0,0,5))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=12.7;scene.camera=cam
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=600;scene.render.resolution_y=600;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D': area.spaces.active.region_3d.view_distance=16;area.spaces.active.region_3d.view_location=(0,0,5)
readme=bpy.data.texts.new('README - Animation')
readme.write('Repaired rig with Idle_Loop (frames 1-96) and Walk_Loop (frames 1-48), 24 fps. Select the armature and choose the action in Dope Sheet > Action Editor. Walk is in place; animate Root or the object for travel. Duplicate final keys at 97 and 49 close the loops. Arms use FK; legs use IK. Original source is unchanged. FK duplicate skin weights merged into deformation bones; FK bones no longer deform. Spine and both FK thighs now follow the pelvis control. IK stretch disabled. High-poly reference and custom-shape source objects hidden from render.\n')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_Fixed_Animated.blend'))
report={'merged_weight_assignments':merged,'actions':{},'invalid_drivers':[]}
for name,period in [('Idle_Loop',96),('Walk_Loop',48)]:
 rig.animation_data.action=bpy.data.actions[name];scene.frame_set(1);bpy.context.view_layer.update()
 first={p.name:p.matrix.copy() for p in rig.pose.bones};scene.frame_set(period+1);bpy.context.view_layer.update()
 report['actions'][name]={'loop_matrix_error':max(abs(p.matrix[i][j]-first[p.name][i][j]) for p in rig.pose.bones for i in range(4) for j in range(4))}
 for f in range(1,period+1,4 if period==48 else 8):
  scene.frame_set(f);scene.render.filepath=os.path.join(out,name+'_%03d.png'%f);bpy.ops.render.render(write_still=True)
report['invalid_drivers']=[d.data_path for d in rig.animation_data.drivers if not d.is_valid]
open(os.path.join(out,'validation.json'),'w').write(json.dumps(report,indent=2))
