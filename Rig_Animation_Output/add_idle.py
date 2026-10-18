import bpy,math,os,json
from mathutils import Quaternion,Vector
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_Deformation_Fixed.blend'))
r=bpy.data.objects['Armature.008'];s=bpy.context.scene
old=bpy.data.actions.get('Idle_Loop')
if old:old.name='Idle_Previous';old.use_fake_user=True
act=bpy.data.actions.new('Idle_Loop');act.use_fake_user=True;r.animation_data.action=act
controls=['bone.Pelvis','Spine2','Spine3','Neck','Head']+[prefix+side for side in ['L','R'] for prefix in ['IK.Control.Foot.','FK.UpperArm.','FK.LowerArm.','FK.Hand.']]
def rot(n,axis,angle):
 p=r.pose.bones[n];q=p.bone.matrix_local.to_quaternion();p.rotation_quaternion=q.inverted()@Quaternion(axis,angle)@q
for f in range(1,145):
 t=2*math.pi*(f-1)/144
 for p in r.pose.bones:p.location=(0,0,0);p.rotation_mode='QUATERNION';p.rotation_quaternion=(1,0,0,0);p.scale=(1,1,1)
 p=r.pose.bones['bone.Pelvis'];p.location=p.bone.matrix_local.to_3x3().inverted()@Vector((.008*math.sin(t),0,-.024+.003*math.cos(2*t)))
 rot('bone.Pelvis',(0,1,0),.008*math.sin(t));rot('Spine2',(1,0,0),.008*math.sin(2*t));rot('Spine3',(0,1,0),-.009*math.sin(t))
 rot('Neck',(1,0,0),.009*math.sin(2*t-.25));rot('Head',(0,0,1),.035*math.sin(t))
 for side,sign in [('L',1),('R',-1)]:
  p=r.pose.bones['FK.UpperArm.'+side];q=p.bone.matrix_local.to_quaternion()
  p.rotation_quaternion=q.inverted()@(Quaternion((1,0,0),.018*math.sin(2*t+sign*.4))@Quaternion((0,1,0),sign*.48))@q
  rot('FK.LowerArm.'+side,(1,0,0),-.14+.015*math.sin(2*t-.4))
  rot('FK.Hand.'+side,(1,0,0),.018*math.sin(2*t-.8))
 for n in controls:
  p=r.pose.bones[n];p.keyframe_insert('location',frame=f,group=n);p.keyframe_insert('rotation_quaternion',frame=f,group=n)
 for key in r.pose.bones['FK/IKSwitch'].keys():
  r.pose.bones['FK/IKSwitch'][key]=1.0 if 'Hand' in key else 0.0
  r.pose.bones['FK/IKSwitch'].keyframe_insert(data_path='["'+key+'"]',frame=f,group='IK FK Settings')
# Exact duplicate endpoint, including all controller channels.
for layer in act.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for fc in bag.fcurves:
    fc.keyframe_points.insert(145,fc.keyframe_points[0].co.y)
    for k in fc.keyframe_points:k.interpolation='LINEAR'
    fc.modifiers.new('CYCLES')
s.render.fps=24;s.frame_start=1;s.frame_end=144;s.frame_set(1)
bpy.data.texts['README - Animation'].write('\nNew six-second Idle_Loop: frames 1-144 at 24 fps (145 closes loop). Gentle breathing, weight shift, head turn, relaxed arm follow-through. Idle selected by default. Walk_Loop retained.\n')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_With_Idle.blend'))
positions=[]
for f in [1,145]:
 s.frame_set(f);dg=bpy.context.evaluated_depsgraph_get();o=bpy.data.objects['LowPoly'].evaluated_get(dg);me=o.to_mesh();positions.append([v.co.copy() for v in me.vertices]);o.to_mesh_clear()
print('IDLE_LOOP_VERTEX_ERROR',max((a-b).length for a,b in zip(*positions)))
for f in range(1,145,12):
 s.frame_set(f);s.render.filepath=os.path.join(out,'New_Idle_%03d.png'%f);bpy.ops.render.render(write_still=True)
