import os
from PIL import Image, ImageEnhance

def is_grayscale(img):
    """
    Check if an image is grayscale.
    Assumes mode 'L' or that R == G == B for all pixels in RGB.
    """
    if img.mode == 'L':
        return True
    if img.mode == 'RGB':
        rgb = img.getdata()
        return all(r == g == b for r, g, b in rgb)
    return False

def double_contrast_grayscale_images(root_dir):
    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.lower().endswith('.png'):
                file_path = os.path.join(dirpath, filename)

                try:
                    with Image.open(file_path) as img:
                        if is_grayscale(img):
                            print(f"Doubling contrast for: {file_path}")
                            enhancer = ImageEnhance.Contrast(img)
                            enhanced_img = enhancer.enhance(2.0)  # Double the contrast
                            enhanced_img.save(file_path)
                        else:
                            print(f"Skipping (not grayscale): {file_path}")
                except Exception as e:
                    print(f"Error processing {file_path}: {e}")

# Replace this path with your actual root folder
root_folder = "C:/Users/elanw/Files/SketchSense/SketchSense/Assets/Resources/"
double_contrast_grayscale_images(root_folder)
