import argparse
from PIL import Image
import sys

def process_image(input_path, output_path, scale):
    try:
        # Open the source image
        with Image.open(input_path) as img:
            # Convert to RGB if it's in a different mode (like RGBA) to ensure compatibility
            img = img.convert("RGB")

            # Calculate the reduced dimensions
            # We use max(1, ...) to ensure the size never hits 0
            small_size = (max(1, img.width // scale), max(1, img.height // scale))

            # Step 1: Shrink (using BOX resample for better color averaging)
            img_small = img.resize(small_size, resample=Image.BOX)

            # Step 2: Scale up using NEAREST to keep those edges sharp
            pixel_art = img_small.resize(img.size, resample=Image.NEAREST)

            # Save the result
            pixel_art.save(output_path)
            print(f"Successfully processed! Created: {output_path}")

    except FileNotFoundError:
        print(f"Error: The file '{input_path}' was not found.")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convert any image into sharp pixel art.")

    # Required positional arguments
    parser.add_argument("input", help="Path to the input image file")
    parser.add_argument("output", help="Path where the output image will be saved")

    # Optional argument for pixel size
    parser.add_argument("-s", "--scale", type=int, default=8,
                        help="The size of the pixels (default: 8)")

    args = parser.parse_args()

    process_image(args.input, args.output, args.scale)