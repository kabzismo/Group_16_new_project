import bpy,json
bpy.ops.wm.open_mainfile(filepath=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\Rig2_Textured_Animated.blend')
m=bpy.data.objects['LowPoly'];r=bpy.data.objects['Armature.008'];coords=[r.matrix_world.inverted()@m.matrix_world@v.co for v in m.data.vertices]
adj=[set() for v in coords]
for e in m.data.edges:a,b=e.vertices;adj[a].add(b);adj[b].add(a)
remaining=set(range(len(coords)));comps=[]
while remaining:
 start=remaining.pop();seen={start};stack=[start]
 while stack:
  for b in adj[stack.pop()]:
   if b in remaining:remaining.remove(b);seen.add(b);stack.append(b)
 comps.append(sorted(seen))
for i,c in enumerate(comps): print(i,len(c),'bounds',[[round(min(coords[v][k] for v in c),3),round(max(coords[v][k] for v in c),3)] for k in range(3)])
json.dump({'coords':[list(v) for v in coords],'components':comps,'edges':[list(e.vertices) for e in m.data.edges]},open(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\topology.json','w'))
r.data.pose_position='REST';bpy.context.scene.render.filepath=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\Rest_Original.png';bpy.ops.render.render(write_still=True)
