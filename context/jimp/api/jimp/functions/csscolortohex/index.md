# cssColorToHex

> **cssColorToHex**(`cssColor`): `number`

Converts a css color (Hex, 8-digit (RGBA) Hex, RGB, RGBA, HSL, HSLA,
HSV, HSVA, Named) to a hex number

## Parameters

• **cssColor**: `string` \| `number`

## Returns

`number`

A hex number representing a color

## Example

```
import { cssColorToHex } from "@jimp/utils";
cssColorToHex("rgba(255, 0, 0, 0.5)"); // "ff000080"
```

## Defined in

packages/utils/dist/esm/index.d.ts:84
