# diff

> **diff**\<`I`\>(`img1`, `img2`, `threshold`?): `object`

Diffs two images and returns

## Type Parameters

• **I** *extends* `JimpClass`

## Parameters

• **img1**: `I`

A Jimp image to compare

• **img2**: `I`

A Jimp image to compare

• **threshold?**: `number`

A number, 0 to 1, the smaller the value the more sensitive the
comparison (default: 0.1)

## Returns

`object`

An object with the following properties:

- percent: The proportion of different pixels (0-1), where 0 means the
  two images are pixel identical
- image: A Jimp image showing differences

### image

> **image**: `any`

### percent

> **percent**: `number`

## Example

```
import { Jimp, diff } from "jimp";
const image1 = await Jimp.read("test/image.png");const image2 = await Jimp.read("test/image.png");
const diff = diff(image1, image2);
diff.percent; // 0.5diff.image; // a Jimp image showing differences
```

## Defined in

packages/diff/dist/esm/index.d.ts:23
