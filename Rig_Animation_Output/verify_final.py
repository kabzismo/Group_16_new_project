import bpy,os
p=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\Rig2_Textured_Animated.blend'
bpy.ops.wm.open_mainfile(filepath=p)
m=bpy.data.objects['LowPoly'];r=bpy.data.objects['Armature.008']
assert all(sum(g.weight for g in v.groups)>.999 for v in m.data.vertices)
assert all(d.is_valid for d in r.animation_data.drivers)
assert all(n.image.packed_file for n in m.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
print('PASS: weights, drivers, packed material textures and saved actions', [a.name for a in bpy.data.actions])
