# loadFont

> **loadFont**(`file`): `Promise`\<`object`\>

Loads a Bitmap Font from a file.

## Parameters

• **file**: `string`

A path or URL to a font file

## Returns

`Promise`\<`object`\>

A collection of Jimp images that can be used to print text

### chars

> **chars**: `Record`\<`string`, `BmCharacter`\>

### common

> **common**: `BmCommonProps`

### info

> **info**: `Record`\<`string`, `any`\>

### kernings

> **kernings**: `Record`\<`string`, `BmKerning`\>

### pages

> **pages**: `object` & `JimpInstanceMethods`\[\]

## Example

```
import { Jimp, loadFont } from "jimp";import { SANS_10_BLACK } from "jimp/fonts";
const font = await loadFont(SANS_10_BLACK);const image = new Jimp({ width: 200, height: 100, color: 0xffffffff });
image.print(font, 10, 10, "Hello world!");
```

## Defined in

plugins/plugin-print/dist/esm/load-font.d.ts:16
