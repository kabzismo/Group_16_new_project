exec(open(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\inspect.py').read().split("r={'objects'")[0])
bpy.ops.object.mode_set(mode='OBJECT')
rig=bpy.data.objects['Armature.008']
for p in rig.pose.bones:
 for c in p.constraints:
  print(p.name,c.type,c.influence, 'chain',getattr(c,'chain_count',''),'pole',getattr(c,'pole_subtarget',''),'angle',getattr(c,'pole_angle',''),'headtail',getattr(c,'head_tail',''))
print('DRIVERS',[(d.data_path,d.driver.expression) for d in rig.animation_data.drivers] if rig.animation_data else None)
print('PROPS',[(p.name,dict(p.items())) for p in rig.pose.bones if p.keys()])
for o in bpy.data.objects: print('VIS',o.name,o.hide_get(),o.hide_render)
scene=bpy.context.scene
for o in bpy.data.objects:
 if o.type!='MESH' or o.name!='LowPoly': o.hide_render=True
bpy.ops.object.camera_add(location=(14,-25,12))
cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=13;scene.camera=cam
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.filepath=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\before.png'
bpy.ops.render.render(write_still=True)
