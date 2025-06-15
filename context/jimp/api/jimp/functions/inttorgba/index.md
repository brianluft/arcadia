# intToRGBA

> **intToRGBA**(`i`):
> [`RGBAColor`](../../interfaces/rgbacolor/index.md)

A helper method that converts RGBA values to a single integer value

## Parameters

• **i**: `number`

A single integer value representing an RGBA colour (e.g. 0xFF0000FF for
red)

## Returns

[`RGBAColor`](../../interfaces/rgbacolor/index.md)

An object with the properties r, g, b and a representing RGBA values

## Example

```
import { intToRGBA } from "@jimp/utils";
intToRGBA(0xFF0000FF); // { r: 255, g: 0, b: 0, a:255 }
```

## Defined in

packages/utils/dist/esm/index.d.ts:24
