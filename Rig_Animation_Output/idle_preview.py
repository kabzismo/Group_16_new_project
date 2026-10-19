from PIL import Image
from pathlib import Path
p=Path(r'C:\Users\alwan\Documents\unity\Group_16_new_project\Rig_Animation_Output')
f=[Image.open(p/('New_Idle_%03d.png'%i)).convert('RGB') for i in range(1,145,12)]
f[0].save(p/'New_Idle.gif',save_all=True,append_images=f[1:],duration=500,loop=0)
