import bpy,math,os,json
from mathutils import Vector,Quaternion
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_Fixed_Animated.blend'))
r=bpy.data.objects['Armature.008'];s=bpy.context.scene
for name,period in [('Idle_Loop',96),('Walk_Loop',48)]:
 r.animation_data.action=bpy.data.actions[name]
 for f in range(1,period+2):
  s.frame_set(f);t=2*math.pi*(f-1)/period
  for side,sign in [('L',1),('R',-1)]:
   phase=t+(0 if side=='L' else math.pi)
   for part in ['UpperArm','LowerArm','Hand']:
    p=r.pose.bones['FK.'+part+'.'+side];basis=p.bone.matrix_local.to_quaternion()
    if part=='UpperArm':
     swing=.38*math.cos(phase) if name=='Walk_Loop' else .025*math.sin(phase)
     world=Quaternion((1,0,0),swing)@Quaternion((0,1,0),sign*.48)
    elif part=='LowerArm':world=Quaternion((1,0,0),-(.18+.09*math.sin(phase-.5)) if name=='Walk_Loop' else -.15)
    else:world=Quaternion((1,0,0),.035*math.sin(phase-.6) if name=='Walk_Loop' else 0)
    p.rotation_quaternion=basis.inverted()@world@basis
    p.keyframe_insert('rotation_quaternion',frame=f,group=p.name)
  for key in r.pose.bones['FK/IKSwitch'].keys():
   r.pose.bones['FK/IKSwitch'].keyframe_insert(data_path='["'+key+'"]',frame=f,group='IK FK Settings')
 for layer in r.animation_data.action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:
     for k in fc.keyframe_points:k.interpolation='LINEAR'
     if not any(m.type=='CYCLES' for m in fc.modifiers):fc.modifiers.new('CYCLES')
s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True
s.world.use_nodes=True;s.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.14,.17,.22,1);s.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.5
for name,pos,power,size in [('Key',(5,-10,15),1800,8),('Fill',(-8,-5,9),1000,7),('Rim',(3,5,13),2200,6)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
 obj=bpy.data.objects.new(name,data);s.collection.objects.link(obj);obj.location=pos;obj.rotation_euler=(Vector((0,0,5))-obj.location).to_track_quat('-Z','Y').to_euler()
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
s.view_settings.view_transform='AgX'
r.animation_data.action=bpy.data.actions['Walk_Loop'];s.frame_set(1)
bpy.data.texts['README - Animation'].write('\nUpdated: natural opposite arm swing, relaxed shoulders and elbow follow-through. Original packed materials preserved. Viewports use Material Preview; renders use Cycles.\n')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_Textured_Animated.blend'))
s.render.resolution_x=600;s.render.resolution_y=600
for f in [1,25]:
 s.frame_set(f);s.render.filepath=os.path.join(out,'Textured_Walk_%03d.png'%f);bpy.ops.render.render(write_still=True)
report={}
for name,period in [('Idle_Loop',96),('Walk_Loop',48)]:
 r.animation_data.action=bpy.data.actions[name];s.frame_set(1);bpy.context.view_layer.update();first={p.name:p.matrix.copy() for p in r.pose.bones};hands={side:list(r.pose.bones['Hand.'+side].head) for side in ['L','R']}
 s.frame_set(period+1);bpy.context.view_layer.update();error=max(abs(p.matrix[i][j]-first[p.name][i][j]) for p in r.pose.bones for i in range(4) for j in range(4))
 s.frame_set(1+period//2);bpy.context.view_layer.update()
 report[name]={'loop_error':error,'hand_travel':{side:(r.pose.bones['Hand.'+side].head-Vector(hands[side])).length for side in ['L','R']}}
open(os.path.join(out,'textured_validation.json'),'w').write(json.dumps(report,indent=2))
