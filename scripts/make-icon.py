from PIL import Image, ImageDraw
from pathlib import Path

# Original geometric comparison mark, supersampled for crisp small Windows icons.
root = Path(__file__).resolve().parent.parent
im = Image.new('RGBA', (1024, 1024))
d = ImageDraw.Draw(im)
d.rounded_rectangle((32, 32, 992, 992), radius=208, fill='#172436')
d.rounded_rectangle((128, 192, 896, 832), radius=72, fill='#7593ad')
d.rectangle((512, 192, 824, 832), fill='#38aada')
d.rounded_rectangle((752, 192, 896, 832), radius=72, fill='#38aada')
d.ellipse((662, 280, 774, 392), fill='#ffda7c')
d.polygon([(128, 686), (316, 452), (512, 650), (512, 832), (200, 832), (128, 760)], fill='#cad5df')
d.polygon([(512, 650), (696, 478), (896, 682), (896, 760), (824, 832), (512, 832)], fill='#d5f3ed')
d.rounded_rectangle((490, 138, 534, 886), radius=20, fill='white')
d.ellipse((410, 410, 614, 614), fill='white')
d.line([(481, 473), (444, 512), (481, 551)], fill='#172436', width=19, joint='curve')
d.line([(543, 473), (580, 512), (543, 551)], fill='#172436', width=19, joint='curve')
im.save(root / 'ImageCompare.ico', sizes=[(16,16),(20,20),(24,24),(32,32),(40,40),(48,48),(64,64),(128,128),(256,256)])
