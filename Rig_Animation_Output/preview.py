from PIL import Image,ImageDraw
from pathlib import Path
p=Path(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output')
for name,duration in [('Walk_Loop',167),('Idle_Loop',333)]:
 frames=[Image.open(f).convert('RGB') for f in sorted(p.glob(name+'_*.png'))]
 frames[0].save(p/(name+'.gif'),save_all=True,append_images=frames[1:],duration=duration,loop=0)
