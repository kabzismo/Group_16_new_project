import bpy
bpy.ops.wm.open_mainfile(filepath=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\Rig2_Fixed_Animated.blend')
r=bpy.data.objects['Armature.008']
for d in r.animation_data.drivers:
 print('DRIVER',d.data_path,d.driver.expression,[(v.name,[(t.id.name if t.id else None,t.data_path) for t in v.targets]) for v in d.driver.variables])
for n in ['FK/IKSwitch','UpperArm.L','FK.UpperArm.L']:
 p=r.pose.bones[n];print('BONE',n,dict(p.items()),[(c.name,c.influence) for c in p.constraints])
for m in bpy.data.materials:
 print('MAT',m.name)
 if m.node_tree:
  for n in m.node_tree.nodes:print('NODE',n.name,n.type,getattr(getattr(n,'image',None),'name',None))
  for l in m.node_tree.links:print('LINK',l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name)
for im in bpy.data.images:print('IMAGE',im.name,im.filepath,bool(im.packed_file),list(im.size))
print('SLOTS',[m.name for m in bpy.data.objects['LowPoly'].data.materials])
