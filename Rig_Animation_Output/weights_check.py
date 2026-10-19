import bpy,collections
bpy.ops.wm.open_mainfile(filepath=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\Rig2_Textured_Animated.blend')
m=bpy.data.objects['LowPoly'];r=bpy.data.objects['Armature.008']
print('GROUPS',[(g.index,g.name) for g in m.vertex_groups])
for v in m.data.vertices:
 p=r.matrix_world.inverted()@m.matrix_world@v.co
 if p.x>.48 and p.z<1.2:
  print('V',v.index,tuple(round(x,3) for x in p),[(m.vertex_groups[w.group].name,round(w.weight,3)) for w in v.groups])
