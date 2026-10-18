from PIL import Image
from pathlib import Path
p=Path(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output')
f=[Image.open(p/('Fixed_Skin_%03d.png'%i)).convert('RGB') for i in range(1,49,4)]
f[0].save(p/'Fixed_Skin_Walk.gif',save_all=True,append_images=f[1:],duration=167,loop=0)
