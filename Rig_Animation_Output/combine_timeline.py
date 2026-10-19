import bpy,os
out=r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output'
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'Rig2_With_Idle.blend'))
r=bpy.data.objects['Armature.008'];s=bpy.context.scene;ad=r.animation_data
idle=bpy.data.actions['Idle_Loop'];walk=bpy.data.actions['Walk_Loop']
ad.action=None
for tr in list(ad.nla_tracks):ad.nla_tracks.remove(tr)
for name,action,start,end in [('01 - IDLE (1-144)',idle,1,145),('02 - WALK (145-240)',walk,145,241)]:
 tr=ad.nla_tracks.new();tr.name=name
 st=tr.strips.new(action.name,start,action)
 if len(action.slots):st.action_slot=action.slots[0]
 st.action_frame_start=1;st.action_frame_end=145 if action==idle else 49
 st.repeat=1 if action==idle else 2
 st.extrapolation='NOTHING';st.blend_type='REPLACE';st.use_auto_blend=False;st.blend_in=0;st.blend_out=0
 assert abs(st.frame_end-end)<.01
s.timeline_markers.clear();s.timeline_markers.new('IDLE starts',frame=1);s.timeline_markers.new('WALK starts',frame=145)
s.frame_start=1;s.frame_end=240;s.frame_set(1)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='OUTLINER':area.spaces.active.filter_text=''
bpy.data.texts['README - Animation'].write('\nBoth animations are now placed on NLA tracks: idle frames 1-144, walk frames 145-240 (two cycles). Press Space to play both. Jump to frame 145 for the walk. Outliner search cleared.\n')
path=os.path.join(out,'Rig2_Idle_and_Walk.blend');bpy.ops.wm.save_as_mainfile(filepath=path)
bpy.ops.wm.open_mainfile(filepath=path)
r=bpy.data.objects['Armature.008'];s=bpy.context.scene
assert len(r.animation_data.nla_tracks)==2
for f in [1,73,145,169]:
 s.frame_set(f);bpy.context.view_layer.update();print('FRAME',f,'HAND',tuple(r.pose.bones['Hand.L'].head))
print('VERIFIED',[(tr.name,[(st.action.name,st.frame_start,st.frame_end) for st in tr.strips]) for tr in r.animation_data.nla_tracks])
