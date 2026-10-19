import bpy,json
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=r'C:\Users\alwan\Documents\blender stuff\Alwande_Shangase_Group16_A2_NPC\working blender file\Rig2.blend')
r={'objects':[],'actions':[a.name for a in bpy.data.actions]}
for o in bpy.data.objects:
 d={'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'loc':list(o.location),'rot':list(o.rotation_euler),'scale':list(o.scale),'dims':list(o.dimensions),'hide':o.hide_viewport}
 if o.type=='ARMATURE':
  d['bones']=[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local),'deform':b.use_deform,'constraints':[{'name':c.name,'type':c.type,'target':getattr(getattr(c,'target',None),'name',None),'subtarget':getattr(c,'subtarget',None)} for c in o.pose.bones[b.name].constraints]} for b in o.data.bones]
 if o.type=='MESH':
  d['vertices']=len(o.data.vertices);d['groups']=[g.name for g in o.vertex_groups];d['mods']=[{'type':m.type,'object':getattr(getattr(m,'object',None),'name',None)} for m in o.modifiers]
 r['objects'].append(d)
open(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output\inspection.json','w').write(json.dumps(r,indent=2))
