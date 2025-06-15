# colorDiff

> **colorDiff**(`rgba1`, `rgba2`): `number`

Compute color difference 0 means no difference, 1 means maximum
difference. Both parameters must be an color object
`{ r:val, g:val, b:val, a:val }` Where `a` is optional and `val` is an
integer between 0 and 255.

## Parameters

• **rgba1**: [`RGBAColor`](../../interfaces/rgbacolor/index.md) \|
[`RGBColor`](../../interfaces/rgbcolor/index.md)

first color to compare.

• **rgba2**: [`RGBAColor`](../../interfaces/rgbacolor/index.md) \|
[`RGBColor`](../../interfaces/rgbcolor/index.md)

second color to compare.

## Returns

`number`

float between 0 and 1.

## Example

```
import { colorDiff } from "@jimp/utils";
colorDiff( { r: 255, g: 0, b: 0, a: 0 }, { r: 0, g: 255, b: 0, a: 0 },); // 0.5
colorDiff( { r: 0, g: 0, b: 0, }, { r: 255, g: 255, b: 255, }); // 0.7
```

## Defined in

packages/utils/dist/esm/index.d.ts:62
