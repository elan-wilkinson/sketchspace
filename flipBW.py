import os
from PIL import Image, ImageOps

def invert_bw_images_in_directory(root_dir):
    for subdir, _, files in os.walk(root_dir):
        for file in files:
            if file.lower().endswith('.png'):
                file_path = os.path.join(subdir, file)
                try:
                    with Image.open(file_path) as img:
                        # Convert to 'L' mode (grayscale) if not already
                        if img.mode != 'L':
                            img = img.convert('L')

                        # Invert image
                        inverted_img = ImageOps.invert(img)

                        # Overwrite original image
                        inverted_img.save(file_path)
                        print(f"Inverted: {file_path}")
                except Exception as e:
                    print(f"Error processing {file_path}: {e}")

# Replace this with your target directory
target_directory = "C:/Users/elanw/Files/SketchSense/SketchSense/Assets/Resources/"
invert_bw_images_in_directory(target_directory)
