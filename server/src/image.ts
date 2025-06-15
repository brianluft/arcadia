import * as fs from 'fs';
import * as path from 'path';
import { Jimp } from 'jimp';
import OpenAI from 'openai';
import { generateTimestampedFilename } from './storage.js';

/**
 * Result of reading/analyzing an image
 */
export interface ImageAnalysisResult {
  /** Analysis result from GPT-4o */
  analysis: string;
  /** Path to the processed image file if it was modified */
  processedImagePath?: string;
}

/**
 * Process an image with jimp according to the specifications:
 * - Resize to 1080p max height while maintaining aspect ratio
 * - Keep PNG as PNG, keep JPG as JPG, convert others to JPG with high quality
 * - Use storage directory for converted images
 */
async function processImage(imagePath: string, storageDirectory: string): Promise<string> {
  const image = await Jimp.read(imagePath);
  const originalFormat = image.mime;

  // Check if we need to resize (height > 1080)
  const needsResize = image.height > 1080;

  // Determine output format
  const isPng = originalFormat === 'image/png';
  const isJpg = originalFormat === 'image/jpeg';
  const keepOriginalFormat = (isPng || isJpg) && !needsResize;

  if (keepOriginalFormat) {
    // No processing needed - use original file
    return imagePath;
  }

  // Process the image
  if (needsResize) {
    // Resize maintaining aspect ratio (width auto, height 1080)
    image.resize({ h: 1080 });
  }

  // Generate output filename with appropriate extension
  const extension = isPng ? 'png' : 'jpg';
  const outputPath = generateTimestampedFilename(storageDirectory, extension);

  // Write the processed image (jimp will determine format based on file extension)
  await image.write(outputPath as `${string}.${string}`);

  return outputPath;
}

/**
 * Validate that the image path is acceptable and the file exists
 */
function validateImagePath(imagePath: string): string {
  // Convert various Windows path formats to normalized Windows path
  let normalizedPath = imagePath;

  // Handle MSYS-style paths like /c/foo/bar or /C:/foo/bar
  if (imagePath.match(/^\/[a-zA-Z](\:|\/)/)) {
    const driveLetter = imagePath.charAt(1).toUpperCase();
    if (imagePath.charAt(2) === ':') {
      // Format: /c:/foo/bar -> C:/foo/bar
      normalizedPath = `${driveLetter}:${imagePath.substring(3)}`;
    } else {
      // Format: /c/foo/bar -> C:/foo/bar
      normalizedPath = `${driveLetter}:${imagePath.substring(2)}`;
    }
  }

  // Convert forward slashes to backslashes for Windows
  normalizedPath = normalizedPath.replace(/\//g, '\\');

  // Check if file exists
  if (!fs.existsSync(normalizedPath)) {
    throw new Error(`Image file does not exist: ${normalizedPath}`);
  }

  // Check if it's a supported image format
  const ext = path.extname(normalizedPath).toLowerCase();
  const supportedFormats = ['.bmp', '.gif', '.jpeg', '.jpg', '.png', '.tiff', '.tif'];
  if (!supportedFormats.includes(ext)) {
    throw new Error(`Unsupported image format: ${ext}. Supported formats: ${supportedFormats.join(', ')}`);
  }

  return normalizedPath;
}

/**
 * Read and analyze an image using OpenAI GPT-4o
 * @param imagePath - Absolute path to the image file
 * @param prompt - Optional prompt for the analysis (defaults to describing for blind user)
 * @param openaiClient - OpenAI client instance
 * @param storageDirectory - Storage directory for processed images
 * @returns Promise<ImageAnalysisResult>
 */
export async function readImage(
  imagePath: string,
  prompt: string | undefined,
  openaiClient: OpenAI,
  storageDirectory: string
): Promise<ImageAnalysisResult> {
  // Validate and normalize the image path
  const normalizedImagePath = validateImagePath(imagePath);

  // Process the image if needed
  const processedImagePath = await processImage(normalizedImagePath, storageDirectory);

  // Read the processed image as base64
  const imageBuffer = await fs.promises.readFile(processedImagePath);
  const base64Image = imageBuffer.toString('base64');

  // Determine MIME type based on file extension
  const ext = path.extname(processedImagePath).toLowerCase();
  let mimeType = 'image/jpeg'; // default
  if (ext === '.png') {
    mimeType = 'image/png';
  }

  // Use default prompt if none provided
  const analysisPrompt = prompt || 'Describe this image to a blind user. Transcribe any text.';

  // Call OpenAI GPT-4o for image analysis
  const completion = await openaiClient.chat.completions.create({
    model: 'gpt-4o',
    max_tokens: 1000,
    messages: [
      {
        role: 'user',
        content: [
          {
            type: 'image_url',
            image_url: {
              url: `data:${mimeType};base64,${base64Image}`,
              detail: 'high',
            },
          },
          {
            type: 'text',
            text: analysisPrompt,
          },
        ],
      },
    ],
  });

  const analysis = completion.choices[0].message.content || 'No analysis returned from GPT-4o';

  return {
    analysis,
    processedImagePath: processedImagePath !== normalizedImagePath ? processedImagePath : undefined,
  };
}
