# Extension Icons

This directory should contain the following icon files:

- `icon16.png` - 16x16 pixels (toolbar icon)
- `icon48.png` - 48x48 pixels (extension management page)
- `icon128.png` - 128x128 pixels (Chrome Web Store)

## Creating Icons

You can create icons using any image editor. The icons should:

1. Have a transparent background
2. Use the SuperQA brand colors (purple gradient: #667eea to #764ba2)
3. Include a recognizable symbol (e.g., test tube, checklist, or "SQ" letters)

## Temporary Placeholder

For development purposes, you can use simple colored squares:

### Using ImageMagick (if available):

```bash
convert -size 16x16 xc:#667eea icon16.png
convert -size 48x48 xc:#667eea icon48.png
convert -size 128x128 xc:#667eea icon128.png
```

### Using SVG to PNG conversion:

Create an SVG with the "SQ" letters or SuperQA logo, then convert to the required sizes.

### Using Online Tools:

- https://www.canva.com
- https://www.favicon-generator.org
- https://realfavicongenerator.net

## Note

The extension will work without proper icons, but they should be added before publishing to the Chrome Web Store.
