import bpy,os,json,math
from mathutils import Vector
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_Deformation_Fixed.blend'))
r=bpy.data.objects['Armature.008'];m=bpy.data.objects['LowPoly'];s=bpy.context.scene
report={'unweighted_vertices':sum(sum(w.weight for w in v.groups)<.999 for v in m.data.vertices),'actions':{}}
rest=[(m.data.vertices[e.vertices[0]].co-m.data.vertices[e.vertices[1]].co).length for e in m.data.edges]
for name,period in [('Idle_Loop',96),('Walk_Loop',48)]:
 r.animation_data.action=bpy.data.actions[name];first=None;ratios=[]
 for f in range(1,period+2):
  s.frame_set(f);dg=bpy.context.evaluated_depsgraph_get();em=m.evaluated_get(dg).to_mesh();vs=[v.co.copy() for v in em.vertices]
  if f==1:first=vs
  if f==period+1:err=max((a-b).length for a,b in zip(first,vs))
  ratios.extend((vs[e.vertices[0]]-vs[e.vertices[1]]).length/l for e,l in zip(m.data.edges,rest) if l>.02)
  m.evaluated_get(dg).to_mesh_clear()
 report['actions'][name]={'loop_vertex_error':err,'edge_stretch_p99':sorted(ratios)[int(len(ratios)*.99)],'max_edge_stretch':max(ratios)}
r.animation_data.action=bpy.data.actions['Walk_Loop'];s.frame_set(1)
t=bpy.data.texts['README - Animation'];t.clear();t.write('Deformation repair: rebuilt normalized anatomical weights, enabled Preserve Volume, retained original mesh topology, UVs and packed textures. Idle_Loop: play 1-96; Walk_Loop: play 1-48 at 24 fps. Endpoints 97/49 close the loops. Arms FK, legs IK. Choose action in Action Editor. Original source untouched.\n')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'Rig2_Deformation_Fixed.blend'))
for f in range(1,49,4):
 s.frame_set(f);s.render.filepath=os.path.join(out,'Fixed_Skin_%03d.png'%f);bpy.ops.render.render(write_still=True)
s.camera.location=(13,26,11);s.camera.rotation_euler=(Vector((0,0,5))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.frame_set(13);s.render.filepath=os.path.join(out,'Fixed_Back.png');bpy.ops.render.render(write_still=True)
json.dump(report,open(os.path.join(out,'deformation_validation.json'),'w'),indent=2)
