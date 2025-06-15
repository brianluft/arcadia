# Jimp

A `Jimp` class enables you to:class

- Read an image into a “bit map” (a collection of pixels)
- Modify the bit map through methods that change the pixels
- Write the bit map back to an image buffer

## Example

#### Basic

You can use the Jimp class to make empty images. This is useful for when
you want to create an image that composed of other images on top of a
background.

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 256, height: 256, color: 0xffffffff });const image2 = new Jimp({ width: 100, height: 100, color: 0xff0000ff });
image.composite(image2, 50, 50);
```

#### Node

You can use jimp in Node.js. For example you can read an image from a
file and resize it and then write it back to a file.

```
import { Jimp } from "jimp";import { promises as fs } from "fs";
const image = await Jimp.read("test/image.png");
image.resize(256, 100);image.greyscale();
await image.write('test/output.png');
```

#### Browser

You can use jimp in the browser by reading files from URLs

```
import { Jimp } from "jimp";
const image = await Jimp.read("https://upload.wikimedia.org/wikipedia/commons/0/01/Bot-Test.jpg");
image.resize(256, 100);image.greyscale();
const output = await image.getBuffer("test/image.png");
const canvas = document.createElement("canvas");
canvas.width = image.bitmap.width;canvas.height = image.bitmap.height;
const ctx = canvas.getContext("2d");ctx.putImageData(image.bitmap, 0, 0);
document.body.appendChild(canvas);
```

## Constructors

### new Jimp()

> **new Jimp**(`JimpConstructorOptions`): [`Jimp`](index.md)

#### Parameters

• **JimpConstructorOptions**:
[`Bitmap`](../../interfaces/bitmap/index.md) \|
[`JimpSimpleConstructorOptions`](../../interfaces/jimpsimpleconstructoroptions/index.md)

#### Returns

[`Jimp`](index.md)

#### Defined in

packages/core/dist/esm/index.d.ts:54

## Methods

### fromBitmap()

> `static` **fromBitmap**(`bitmap`): `any`

Create a Jimp instance from a bitmap. The difference between this and
just using the constructor is that this will convert raw image data into
the bitmap format that Jimp uses.

#### Parameters

• **bitmap**: [`RawImageData`](../../interfaces/rawimagedata/index.md)

#### Returns

`any`

#### Example

```
import { Jimp } from "jimp";
const image = Jimp.fromBitmap({  data: Buffer.from([    0xffffffff, 0xffffffff, 0xffffffff,    0xffffffff, 0xffffffff, 0xffffffff,    0xffffffff, 0xffffffff, 0xffffffff,  ]),  width: 3,  height: 3,});
```

#### Defined in

packages/core/dist/esm/index.d.ts:839

------------------------------------------------------------------------

### fromBuffer()

> `static` **fromBuffer**(`buffer`, `options`?): `Promise`\<`Jimp`\>

Parse a bitmap with the loaded image types.

#### Parameters

• **buffer**: `Buffer` \| `ArrayBuffer`

Raw image data

• **options?**: `Record`\<`"image/tiff"`, `undefined` \|
`Record`\<`string`, `any`\>\> \| `Record`\<`"image/gif"`, `undefined` \|
`Record`\<`string`, `any`\>\> \| `Record`\<`"image/bmp"`,
`DecodeBmpOptions`\> \| `Record`\<`"image/x-ms-bmp"`,
`DecodeBmpOptions`\> \| `Record`\<`"image/jpeg"`, `DecodeJpegOptions`\>
\| `Record`\<`"image/png"`, `DecodePngOptions`\>

#### Returns

`Promise`\<`Jimp`\>

#### Example

```
import { Jimp } from "jimp";
const buffer = await fs.readFile("test/image.png");const image = await Jimp.fromBuffer(buffer);
```

#### Defined in

packages/core/dist/esm/index.d.ts:1102

------------------------------------------------------------------------

### read()

> `static` **read**(`url`, `options`?): `Promise`\<`Jimp`\>

Create a Jimp instance from a URL, a file path, or a Buffer

#### Parameters

• **url**: `string` \| `Buffer` \| `ArrayBuffer`

• **options?**: `Record`\<`"image/tiff"`, `undefined` \|
`Record`\<`string`, `any`\>\> \| `Record`\<`"image/gif"`, `undefined` \|
`Record`\<`string`, `any`\>\> \| `Record`\<`"image/bmp"`,
`DecodeBmpOptions`\> \| `Record`\<`"image/x-ms-bmp"`,
`DecodeBmpOptions`\> \| `Record`\<`"image/jpeg"`, `DecodeJpegOptions`\>
\| `Record`\<`"image/png"`, `DecodePngOptions`\>

#### Returns

`Promise`\<`Jimp`\>

#### Example

```
import { Jimp } from "jimp";
// Read from a file pathconst image = await Jimp.read("test/image.png");
// Read from a URLconst image = await Jimp.read("https://upload.wikimedia.org/wikipedia/commons/0/01/Bot-Test.jpg");
```

#### Defined in

packages/core/dist/esm/index.d.ts:318

------------------------------------------------------------------------

### autocrop()

> **autocrop**(`options`?): `Jimp`

Autocrop same color borders from this image. This function will attempt
to crop out transparent pixels from the image.

#### Parameters

• **options?**:
[`AutocropOptions`](../../type-aliases/autocropoptions/index.md)

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");const cropped = image.autocrop();
```

#### Defined in

plugins/plugin-crop/dist/esm/index.d.ts:99

------------------------------------------------------------------------

### blit()

> **blit**(`options`): `Jimp`

Short for “bit-block transfer”. It involves the transfer of a block of
pixel data from one area of a computer’s memory to another area,
typically for the purpose of rendering images on the screen or
manipulating them in various ways. It’s a fundamental operation in
computer graphics utilized in various applications, from operating
systems to video games.

#### Parameters

• **options**: `object` \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");const parrot = await Jimp.read("test/party-parrot.png");
image.blit({ src: parrot, x: 10, y: 10 });
```

#### Defined in

plugins/plugin-blit/dist/esm/index.d.ts:115

------------------------------------------------------------------------

### blur()

> **blur**(`r`): `Jimp`

A fast blur algorithm that produces similar effect to a Gaussian blur -
but MUCH quicker

#### Parameters

• **r**: `number`

the pixel radius of the blur

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.blur(5);
```

#### Defined in

plugins/plugin-blur/dist/esm/index.d.ts:15

------------------------------------------------------------------------

### brightness()

> **brightness**(`val`): `Jimp`

Adjusts the brightness of the image

#### Parameters

• **val**: `number`

the amount to adjust the brightness.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.brightness(0.5);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:542

------------------------------------------------------------------------

### circle()

> **circle**(`options`?): `Jimp`

Creates a circle out of an image.

#### Parameters

• **options?**

• **options.radius?**: `number`

the radius of the circle

• **options.x?**: `number`

the x position to draw the circle

• **options.y?**: `number`

the y position to draw the circle

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.circle();// orimage.circle({ radius: 50, x: 25, y: 25 });
```

#### Defined in

plugins/plugin-circle/dist/esm/index.d.ts:34

------------------------------------------------------------------------

### clone()

> **clone**\<`S`\>(`this`): `S`

Clone the image into a new Jimp instance.

#### Type Parameters

• **S** *extends* `unknown`

#### Parameters

• **this**: `S`

#### Returns

`S`

A new Jimp instance

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
const clone = image.clone();
```

#### Defined in

packages/core/dist/esm/index.d.ts:156

------------------------------------------------------------------------

### color()

> **color**(`actions`): `Jimp`

Apply multiple color modification rules

#### Parameters

• **actions**: (`object` \| `object` \| `object` \| `object` \| `object`
\| `object` \| `object` \| `object` \| `object` \| `object` \| `object`
\| `object` \| `object` \| `object` \| `object`)\[\]

list of color modification rules, in following format: { apply: '',
params: \[ \] }

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.color([  { apply: "hue", params: [-90] },  { apply: "lighten", params: [50] },  { apply: "xor", params: ["#06D"] },]);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:703

------------------------------------------------------------------------

### composite()

> **composite**\<`I`\>(`src`, `x`?, `y`?, `options`?): `any`

Composites a source image over to this image respecting alpha channels

#### Type Parameters

• **I** *extends* `unknown`

#### Parameters

• **src**: `I`

the source Jimp instance

• **x?**: `number`

the x position to blit the image

• **y?**: `number`

the y position to blit the image

• **options?**

determine what mode to use

• **options.mode?**:
[`BlendMode`](../../enumerations/blendmode/index.md)

• **options.opacityDest?**: `number`

• **options.opacitySource?**: `number`

#### Returns

`any`

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 10, height: 10, color: 0xffffffff });const image2 = new Jimp({ width: 3, height: 3, color: 0xff0000ff });
image.composite(image2, 3, 3);
```

#### Defined in

packages/core/dist/esm/index.d.ts:236

------------------------------------------------------------------------

### contain()

> **contain**(`options`): `Jimp`

Scale the image to the given width and height keeping the aspect ratio.
Some parts of the image may be letter boxed.

#### Parameters

• **options**

• **options.align?**: `number`

A bitmask for horizontal and vertical alignment

• **options.h**: `number`

the height to resize the image to

• **options.mode?**:
[`ResizeStrategy`](../../enumerations/resizestrategy/index.md)

a scaling method (e.g. Jimp.RESIZE_BEZIER)

• **options.w**: `number`

the width to resize the image to

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.contain({ w: 150, h: 100 });
```

#### Defined in

plugins/plugin-contain/dist/esm/index.d.ts:41

------------------------------------------------------------------------

### contrast()

> **contrast**(`val`): `Jimp`

Adjusts the contrast of the image

#### Parameters

• **val**: `number`

the amount to adjust the contrast, a number between -1 and +1

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.contrast(0.75);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:555

------------------------------------------------------------------------

### convolute()

> **convolute**(`options`): `Jimp`

Applies a convolution kernel to the image or a region

#### Parameters

• **options**: `number`\[\]\[\] \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
// apply a convolution kernel to the whole imageimage.convolution([  [-1, -1, 0],  [-1, 1, 1],  [0, 1, 1],]);
// apply a convolution kernel to a regionimage.convolution([  [-1, -1, 0],  [-1, 1, 1],  [0, 1, 1],], 10, 10, 10, 20);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:686

------------------------------------------------------------------------

### convolution()

> **convolution**(`options`): `Jimp`

Adds each element of the image to its local neighbors, weighted by the
kernel

#### Parameters

• **options**: `number`\[\]\[\] \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.convolute([  [-1, -1, 0],  [-1, 1, 1],  [0, 1, 1],]);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:634

------------------------------------------------------------------------

### cover()

> **cover**(`options`): `Jimp`

Scale the image so the given width and height keeping the aspect ratio.
Some parts of the image may be clipped.

#### Parameters

• **options**

• **options.align?**: `number`

A bitmask for horizontal and vertical alignment

• **options.h**: `number`

the height to resize the image to

• **options.mode?**:
[`ResizeStrategy`](../../enumerations/resizestrategy/index.md)

a scaling method (e.g. ResizeStrategy.BEZIER)

• **options.w**: `number`

the width to resize the image to

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.cover(150, 100);
```

#### Defined in

plugins/plugin-cover/dist/esm/index.d.ts:37

------------------------------------------------------------------------

### crop()

> **crop**(`options`): `Jimp`

Crops the image at a given point to a give size.

#### Parameters

• **options**

• **options.h**: `number`

the height to crop form

• **options.w**: `number`

the width to crop form

• **options.x**: `number`

the x position to crop form

• **options.y**: `number`

the y position to crop form

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");const cropped = image.crop(150, 100);
```

#### Defined in

plugins/plugin-crop/dist/esm/index.d.ts:86

------------------------------------------------------------------------

### displace()

> **displace**(`options`): `Jimp`

Displaces the image based on the provided displacement map

#### Parameters

• **options**

• **options.map**

the source Jimp instance

• **options.map.bitmap**

• **options.map.bitmap.data**: `Buffer` \| `Uint8Array`

• **options.map.bitmap.height**: `number`

• **options.map.bitmap.width**: `number`

• **options.offset**: `number`

the maximum displacement value

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");const map = await Jimp.read("test/map.png");
image.displace(map, 10);
```

#### Defined in

plugins/plugin-displace/dist/esm/index.d.ts:69

------------------------------------------------------------------------

### distanceFromHash()

> **distanceFromHash**(`compareHash`): `number`

Calculates the hamming distance of the current image and a hash based on
their perceptual hash

#### Parameters

• **compareHash**: `string`

hash to compare to

#### Returns

`number`

a number ranging from 0 to 1, 0 means they are believed to be identical

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.distanceFromHash(image.pHash());
```

#### Defined in

plugins/plugin-hash/dist/esm/index.d.ts:43

------------------------------------------------------------------------

### dither()

> **dither**(): `Jimp`

Apply a ordered dithering effect.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.dither();
```

#### Defined in

plugins/plugin-dither/dist/esm/index.d.ts:14

------------------------------------------------------------------------

### fade()

> **fade**(`f`): `Jimp`

Fades each pixel by a factor between 0 and 1

#### Parameters

• **f**: `number`

A number from 0 to 1. 0 will haven no effect. 1 will turn the image
completely transparent.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.fade(0.7);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:618

------------------------------------------------------------------------

### fisheye()

> **fisheye**(`options`?): `Jimp`

Adds a fisheye effect to the image.

#### Parameters

• **options?**

• **options.radius?**: `number`

the radius of the circle

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.fisheye();
```

#### Defined in

plugins/plugin-fisheye/dist/esm/index.d.ts:24

------------------------------------------------------------------------

### flip()

> **flip**(`options`): `Jimp`

Flip the image.

#### Parameters

• **options**

• **options.horizontal?**: `boolean`

if true the image will be flipped horizontally

• **options.vertical?**: `boolean`

if true the image will be flipped vertically

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.flip(true, false);
```

#### Defined in

plugins/plugin-flip/dist/esm/index.d.ts:30

------------------------------------------------------------------------

### gaussian()

> **gaussian**(`r`): `Jimp`

Applies a true Gaussian blur to the image (warning: this is VERY slow)

#### Parameters

• **r**: `number`

the pixel radius of the blur

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.gaussian(15);
```

#### Defined in

plugins/plugin-blur/dist/esm/index.d.ts:28

------------------------------------------------------------------------

### getBase64()

> **getBase64**\<`ProvidedMimeType`, `Options`\>(`mime`, `options`?):
> `Promise`\<`string`\>

Converts the image to a base 64 string

#### Type Parameters

• **ProvidedMimeType** *extends* `"image/x-ms-bmp"` \| `"image/bmp"` \|
`"image/gif"` \| `"image/jpeg"` \| `"image/png"` \| `"image/tiff"`

• **Options** *extends* `undefined` \| `Record`\<`string`, `any`\> \|
`Pretty`\<`Partial`\<`Pick`\<`BmpImage`, `"colors"` \| `"palette"` \|
`"hr"` \| `"importantColors"` \| `"vr"` \| `"reserved1"` \|
`"reserved2"`\>\>\> \|
[`JPEGOptions`](../../interfaces/jpegoptions/index.md) \|
`Omit`\<`PNGOptions`, `"filterType"` \| `"colorType"` \|
`"inputColorType"`\> & `object`

#### Parameters

• **mime**: `ProvidedMimeType`

The mime type to export to

• **options?**: `Options`

The options to use when exporting

#### Returns

`Promise`\<`string`\>

#### Example

```
import { Jimp } from "jimp";
const image = Jimp.fromBuffer(Buffer.from([  0xff, 0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00,  0xff, 0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00,  0xff, 0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00,]));
const base64 = image.getBase64("image/jpeg", {  quality: 50,});
```

#### Defined in

packages/core/dist/esm/index.d.ts:124

------------------------------------------------------------------------

### getBuffer()

> **getBuffer**\<`ProvidedMimeType`, `Options`\>(`mime`, `options`?):
> `Promise`\<`Buffer`\>

Converts the Jimp instance to an image buffer

#### Type Parameters

• **ProvidedMimeType** *extends* `"image/x-ms-bmp"` \| `"image/bmp"` \|
`"image/gif"` \| `"image/jpeg"` \| `"image/png"` \| `"image/tiff"`

• **Options** *extends* `undefined` \| `Record`\<`string`, `any`\> \|
`Pretty`\<`Partial`\<`Pick`\<`BmpImage`, `"colors"` \| `"palette"` \|
`"hr"` \| `"importantColors"` \| `"vr"` \| `"reserved1"` \|
`"reserved2"`\>\>\> \|
[`JPEGOptions`](../../interfaces/jpegoptions/index.md) \|
`Omit`\<`PNGOptions`, `"filterType"` \| `"colorType"` \|
`"inputColorType"`\> & `object`

#### Parameters

• **mime**: `ProvidedMimeType`

The mime type to export to

• **options?**: `Options`

The options to use when exporting

#### Returns

`Promise`\<`Buffer`\>

#### Example

```
import { Jimp } from "jimp";import { promises as fs } from "fs";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
await image.getBuffer("image/jpeg", {  quality: 50,});
```

#### Defined in

packages/core/dist/esm/index.d.ts:103

------------------------------------------------------------------------

### getPixelColor()

> **getPixelColor**(`x`, `y`): `number`

Returns the hex color value of a pixel

#### Parameters

• **x**: `number`

the x coordinate

• **y**: `number`

the y coordinate

#### Returns

`number`

the color of the pixel

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
image.getPixelColor(1, 1); // 0xffffffff
```

#### Defined in

packages/core/dist/esm/index.d.ts:187

------------------------------------------------------------------------

### getPixelIndex()

> **getPixelIndex**(`x`, `y`, `edgeHandling`?): `number`

Returns the offset of a pixel in the bitmap buffer

#### Parameters

• **x**: `number`

the x coordinate

• **y**: `number`

the y coordinate

• **edgeHandling?**: [`Edge`](../../enumerations/edge/index.md)

(optional) define how to sum pixels from outside the border

#### Returns

`number`

the index of the pixel or -1 if not found

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
image.getPixelIndex(1, 1); // 2
```

#### Defined in

packages/core/dist/esm/index.d.ts:172

------------------------------------------------------------------------

### greyscale()

> **greyscale**(): `Jimp`

Removes colour from the image using ITU Rec 709 luminance values

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.greyscale();
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:580

------------------------------------------------------------------------

### hasAlpha()

> **hasAlpha**(): `boolean`

Determine if the image contains opaque pixels.

#### Returns

`boolean`

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffaa });const image2 = new Jimp({ width: 3, height: 3, color: 0xff0000ff });
image.hasAlpha(); // falseimage2.hasAlpha(); // true
```

#### Defined in

packages/core/dist/esm/index.d.ts:219

------------------------------------------------------------------------

### hash()

> **hash**(`base`?): `string`

Generates a perceptual hash of the image
<https://en.wikipedia.org/wiki/Perceptual_hashing>. And pads the string.
Can configure base.

#### Parameters

• **base?**: `number`

A number between 2 and 64 representing the base for the hash (e.g. 2 is
binary, 10 is decimal, 16 is hex, 64 is base 64). Defaults to 64.

#### Returns

`string`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.hash(2); // binaryimage.hash(64); // base 64
```

#### Defined in

plugins/plugin-hash/dist/esm/index.d.ts:29

------------------------------------------------------------------------

### inspect()

> **inspect**(): `string`

Nicely format Jimp object when sent to the console e.g.
console.log(image)

#### Returns

`string`

Pretty printed jimp object

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
console.log(image);
```

#### Defined in

packages/core/dist/esm/index.d.ts:77

------------------------------------------------------------------------

### invert()

> **invert**(): `Jimp`

Inverts the colors in the image.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.invert();
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:529

------------------------------------------------------------------------

### mask()

> **mask**(`options`): `Jimp`

Masks a source image on to this image using average pixel colour. A
completely black pixel on the mask will turn a pixel in the image
completely transparent.

#### Parameters

• **options**: `object` \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");const mask = await Jimp.read("test/mask.png");
image.mask(mask);
```

#### Defined in

plugins/plugin-mask/dist/esm/index.d.ts:99

------------------------------------------------------------------------

### normalize()

> **normalize**(): `Jimp`

Normalizes the image.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.normalize();
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:517

------------------------------------------------------------------------

### opacity()

> **opacity**(`f`): `Jimp`

Multiplies the opacity of each pixel by a factor between 0 and 1

#### Parameters

• **f**: `number`

A number, the factor by which to multiply the opacity of each pixel

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.opacity(0.5);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:593

------------------------------------------------------------------------

### opaque()

> **opaque**(): `Jimp`

Set the alpha channel on every pixel to fully opaque.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.opaque();
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:646

------------------------------------------------------------------------

### pHash()

> **pHash**(): `string`

Calculates the perceptual hash

#### Returns

`string`

the perceptual hash

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.hash();
```

#### Defined in

plugins/plugin-hash/dist/esm/index.d.ts:15

------------------------------------------------------------------------

### pixelate()

> **pixelate**(`options`): `Jimp`

Pixelates the image or a region

#### Parameters

• **options**: `number` \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
// pixelate the whole imageimage.pixelate(10);
// pixelate a regionimage.pixelate(10, 10, 10, 20, 20);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:662

------------------------------------------------------------------------

### posterize()

> **posterize**(`n`): `Jimp`

Apply a posterize effect

#### Parameters

• **n**: `number`

the amount to adjust the contrast, minimum threshold is two

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.posterize(5);
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:568

------------------------------------------------------------------------

### print()

> **print**(`__namedParameters`): `Jimp`

Draws a text on a image on a given boundary

#### Parameters

• **\_\_namedParameters**: `object` & `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");const font = await Jimp.loadFont(Jimp.FONT_SANS_32_BLACK);
image.print({ font, x: 10, y: 10, text: "Hello world!" });
```

#### Defined in

plugins/plugin-print/dist/esm/index.d.ts:88

------------------------------------------------------------------------

### quantize()

> **quantize**(`options`): `Jimp`

Image color number reduction.

#### Parameters

• **options**

• **options.colorDistanceFormula?**: `"euclidean-bt709"` \|
`"cie94-graphic-arts"` \| `"cie94-textiles"` \| `"ciede2000"` \|
`"color-metric"` \| `"euclidean"` \| `"euclidean-bt709-noalpha"` \|
`"manhattan"` \| `"manhattan-bt709"` \| `"manhattan-nommyde"` \|
`"pngquant"`

• **options.colors?**: `number`

• **options.imageQuantization?**: `"floyd-steinberg"` \| `"nearest"` \|
`"riemersma"` \| `"false-floyd-steinberg"` \| `"stucki"` \| `"atkinson"`
\| `"jarvis"` \| `"burkes"` \| `"sierra"` \| `"two-sierra"` \|
`"sierra-lite"`

• **options.paletteQuantization?**: `"wuquant"` \| `"neuquant"` \|
`"rgbquant"` \| `"neuquant-float"`

#### Returns

`Jimp`

#### Defined in

plugins/plugin-quantize/dist/esm/index.d.ts:24

------------------------------------------------------------------------

### resize()

> **resize**(`options`): `Jimp`

Resizes the image to a set width and height using a 2-pass bilinear
algorithm

#### Parameters

• **options**: `object` \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.resize({ w: 150 });
```

#### Defined in

plugins/plugin-resize/dist/esm/index.d.ts:80

------------------------------------------------------------------------

### rotate()

> **rotate**(`options`): `Jimp`

Rotates the image counter-clockwise by a number of degrees. By default
the width and height of the image will be resized appropriately.

#### Parameters

• **options**: `number` \| `object`

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.rotate(90);
```

#### Defined in

plugins/plugin-rotate/dist/esm/index.d.ts:29

------------------------------------------------------------------------

### scale()

> **scale**(`options`): `Jimp`

Uniformly scales the image by a factor.

#### Parameters

• **options**:
[`ScaleOptions`](../../type-aliases/scaleoptions/index.md)

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.scale(0.5);
```

#### Defined in

plugins/plugin-resize/dist/esm/index.d.ts:94

------------------------------------------------------------------------

### scaleToFit()

> **scaleToFit**(`options`): `Jimp`

Scale the image to the largest size that fits inside the rectangle that
has the given width and height.

#### Parameters

• **options**

• **options.h**: `number`

the height to resize the image to

• **options.mode?**:
[`ResizeStrategy`](../../enumerations/resizestrategy/index.md)

a scaling method (e.g. Jimp.RESIZE_BEZIER)

• **options.w**: `number`

the width to resize the image to

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.scaleToFit(100, 100);
```

#### Defined in

plugins/plugin-resize/dist/esm/index.d.ts:109

------------------------------------------------------------------------

### scan()

#### scan(f)

> **scan**(`f`): `any`

Scan through the image and call the callback for each pixel

##### Parameters

• **f**

##### Returns

`any`

##### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
image.scan((x, y, idx) => {  // do something with the pixel});
// Or scan through just a regionimage.scan(0, 0, 2, 2, (x, y, idx) => {  // do something with the pixel});
```

##### Defined in

packages/core/dist/esm/index.d.ts:260

#### scan(x, y, w, h, cb)

> **scan**(`x`, `y`, `w`, `h`, `cb`): `any`

Scan through the image and call the callback for each pixel

##### Parameters

• **x**: `number`

• **y**: `number`

• **w**: `number`

• **h**: `number`

• **cb**

##### Returns

`any`

##### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
image.scan((x, y, idx) => {  // do something with the pixel});
// Or scan through just a regionimage.scan(0, 0, 2, 2, (x, y, idx) => {  // do something with the pixel});
```

##### Defined in

packages/core/dist/esm/index.d.ts:280

------------------------------------------------------------------------

### scanIterator()

> **scanIterator**(`x`?, `y`?, `w`?, `h`?): `Generator`\<`object`,
> `void`, `unknown`\>

Iterate scan through a region of the bitmap

#### Parameters

• **x?**: `number`

the x coordinate to begin the scan at

• **y?**: `number`

the y coordinate to begin the scan at

• **w?**: `number`

the width of the scan region

• **h?**: `number`

the height of the scan region

#### Returns

`Generator`\<`object`, `void`, `unknown`\>

##### idx

> **idx**: `number`

##### image

> **image**: `any`

##### x

> **x**: `number`

##### y

> **y**: `number`

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
for (const { x, y, idx, image } of j.scanIterator()) {  // do something with the pixel}
```

#### Defined in

packages/core/dist/esm/index.d.ts:298

------------------------------------------------------------------------

### sepia()

> **sepia**(): `Jimp`

Applies a sepia tone to the image.

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.sepia();
```

#### Defined in

plugins/plugin-color/dist/esm/index.d.ts:605

------------------------------------------------------------------------

### setPixelColor()

> **setPixelColor**(`hex`, `x`, `y`): `any`

Sets the hex colour value of a pixel

#### Parameters

• **hex**: `number`

color to set

• **x**: `number`

the x coordinate

• **y**: `number`

the y coordinate

#### Returns

`any`

#### Example

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 3, height: 3, color: 0xffffffff });
image.setPixelColor(0xff0000ff, 0, 0);
```

#### Defined in

packages/core/dist/esm/index.d.ts:204

------------------------------------------------------------------------

### threshold()

> **threshold**(`options`): `Jimp`

Applies a minimum color threshold to a grayscale image. Converts image
to grayscale by default.

#### Parameters

• **options**

• **options.autoGreyscale?**: `boolean`

A boolean whether to apply greyscale beforehand (default true)

• **options.max**: `number`

A number auto limited between 0 - 255

• **options.replace?**: `number`

A number auto limited between 0 - 255 (default 255)

#### Returns

`Jimp`

#### Example

```
import { Jimp } from "jimp";
const image = await Jimp.read("test/image.png");
image.threshold({ max: 150 });
```

#### Defined in

plugins/plugin-threshold/dist/esm/index.d.ts:33

------------------------------------------------------------------------

### toString()

> **toString**(): `string`

Nicely format Jimp object when converted to a string

#### Returns

`string`

pretty printed

#### Defined in

packages/core/dist/esm/index.d.ts:82

------------------------------------------------------------------------

### write()

> **write**\<`Extension`, `Mime`, `Options`\>(`path`, `options`?):
> `Promise`\<`void`\>

Write the image to a file

#### Type Parameters

• **Extension** *extends* `string`

• **Mime** *extends* `"image/x-ms-bmp"` \| `"image/bmp"` \|
`"image/gif"` \| `"image/jpeg"` \| `"image/png"` \| `"image/tiff"`

• **Options** *extends* `undefined` \| `Record`\<`string`, `any`\> \|
`Pretty`\<`Partial`\<`Pick`\<`BmpImage`, `"colors"` \| `"palette"` \|
`"hr"` \| `"importantColors"` \| `"vr"` \| `"reserved1"` \|
`"reserved2"`\>\>\> \|
[`JPEGOptions`](../../interfaces/jpegoptions/index.md) \|
`Omit`\<`PNGOptions`, `"filterType"` \| `"colorType"` \|
`"inputColorType"`\> & `object`

#### Parameters

• **path**: \`\${string}.\${Extension}\`

the path to write the image to

• **options?**: `Options`

the options to use when writing the image

#### Returns

`Promise`\<`void`\>

#### Example

```
import { Jimp } from "jimp";
const image = Jimp.fromBuffer(Buffer.from([  0xff, 0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00,  0xff, 0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00,  0xff, 0x00, 0x00, 0x00, 0xff, 0x00, 0x00, 0x00,]));
await image.write("test/output.png");
```

#### Defined in

packages/core/dist/esm/index.d.ts:142

## Properties

### background

> **background**: `number`

Default color to use for new pixels

#### Defined in

packages/core/dist/esm/index.d.ts:60

------------------------------------------------------------------------

### bitmap

> **bitmap**: [`Bitmap`](../../interfaces/bitmap/index.md)

The bitmap data of the image

#### Defined in

packages/core/dist/esm/index.d.ts:58

------------------------------------------------------------------------

### formats

> **formats**: `Format`\<`any`, `undefined`, `undefined`\>\[\]

Formats that can be used with Jimp

#### Defined in

packages/core/dist/esm/index.d.ts:62

------------------------------------------------------------------------

### height

> `readonly` **height**: `number`

Get the height of the image

#### Defined in

packages/core/dist/esm/index.d.ts:86

------------------------------------------------------------------------

### mime?

> `optional` **mime**: `string`

The original MIME type of the image

#### Defined in

packages/core/dist/esm/index.d.ts:64

------------------------------------------------------------------------

### width

> `readonly` **width**: `number`

Get the width of the image

#### Defined in

packages/core/dist/esm/index.d.ts:84
