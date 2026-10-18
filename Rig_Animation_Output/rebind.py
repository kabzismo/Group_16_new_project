import bpy,os,json
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_Textured_Animated.blend'))
r=bpy.data.objects['Armature.008'];m=bpy.data.objects['LowPoly'];s=bpy.context.scene
if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
r.data.pose_position='REST'
m.vertex_groups.clear()
bpy.ops.object.select_all(action='DESELECT');m.select_set(True);r.select_set(True);bpy.context.view_layer.objects.active=r
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for mod in m.modifiers:
 if mod.type=='ARMATURE':mod.use_deform_preserve_volume=True
r.data.pose_position='POSE'
print('UNWEIGHTED',sum(1 for v in m.data.vertices if sum(w.weight for w in v.groups)<.001))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_Deformation_Fixed.blend'))
for f in [1,13,25]:
 s.frame_set(f);s.render.filepath=os.path.join(out,'Skin_Test_%03d.png'%f);bpy.ops.render.render(write_still=True)
