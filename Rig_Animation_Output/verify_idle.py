import bpy,os
from mathutils import Vector
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_With_Idle.blend'))
r=bpy.data.objects['Armature.008'];s=bpy.context.scene
assert r.animation_data.action.name=='Idle_Loop'
assert (s.frame_start,s.frame_end)==(1,144)
m=bpy.data.objects['LowPoly'];poses=[]
for f in [1,145]:
 s.frame_set(f);dg=bpy.context.evaluated_depsgraph_get();obj=m.evaluated_get(dg);me=obj.to_mesh();poses.append([v.co.copy() for v in me.vertices]);obj.to_mesh_clear()
err=max((a-b).length for a,b in zip(*poses));assert err<1e-5
print('PASS: saved idle selected, 144-frame playback, loop error',err,'walk retained',bool(bpy.data.actions.get('Walk_Loop')))
