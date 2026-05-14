from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

generated_dir = Path(__file__).resolve().parent.parent / "generated"
pages_dir = generated_dir / "rendered_rescate_word" / "pages"
out_dir = generated_dir / "rendered_rescate_word" / "review_sheets"
out_dir.mkdir(parents=True, exist_ok=True)

pages = sorted(pages_dir.glob("page-*.png"), key=lambda p: int(p.stem.split("-")[1]))
thumb_w = 330
gap = 34
label_h = 28
cols = 2
rows = 2

font_path = Path("C:/Windows/Fonts/arial.ttf")
font = ImageFont.truetype(str(font_path), 18) if font_path.exists() else ImageFont.load_default()

for batch_idx in range(0, len(pages), cols * rows):
    batch = pages[batch_idx: batch_idx + cols * rows]
    thumbs = []
    for page in batch:
        img = Image.open(page).convert("RGB")
        ratio = thumb_w / img.width
        thumb_h = int(img.height * ratio)
        thumbs.append((page, img.resize((thumb_w, thumb_h), Image.Resampling.LANCZOS)))
    max_h = max(t.height for _, t in thumbs)
    sheet = Image.new("RGB", (cols * thumb_w + (cols + 1) * gap, rows * (max_h + label_h) + (rows + 1) * gap), "#edf0f3")
    draw = ImageDraw.Draw(sheet)
    for i, (page, thumb) in enumerate(thumbs):
        c = i % cols
        r = i // cols
        x = gap + c * (thumb_w + gap)
        y = gap + r * (max_h + label_h + gap)
        draw.text((x, y), page.stem, font=font, fill="#30343B")
        sheet.paste(thumb, (x, y + label_h))
    sheet.save(out_dir / f"review-{batch_idx // (cols * rows) + 1}.png")

print(len(pages))
