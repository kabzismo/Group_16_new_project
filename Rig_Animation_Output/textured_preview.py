from PIL import Image
from pathlib import Path
p=Path(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output')
frames=[Image.open(p/('Textured_Walk_%03d.png'%f)).convert('RGB') for f in range(1,49,4)]
frames[0].save(p/'Textured_Walk.gif',save_all=True,append_images=frames[1:],duration=167,loop=0)
