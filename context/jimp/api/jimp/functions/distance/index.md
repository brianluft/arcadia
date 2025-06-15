# distance

> **distance**\<`I`\>(`img1`, `img2`): `number`

Calculates the hamming distance of two images based on their perceptual
hash

## Type Parameters

• **I** *extends* `JimpClass`

## Parameters

• **img1**: `I`

A Jimp image to compare

• **img2**: `I`

A Jimp image to compare

## Returns

`number`

A number ranging from 0 to 1, 0 means they are believed to be identical

## Example

```
import { Jimp, distance } from "jimp";
const image1 = await Jimp.read("test/image.png");const image2 = await Jimp.read("test/image.png");
distance(image1, image2); // 0.5
```

## Defined in

plugins/plugin-hash/dist/esm/index.d.ts:60
