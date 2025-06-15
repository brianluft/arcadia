# limit255

> **limit255**(`n`): `number`

Limits a number to between 0 or 255

## Parameters

• **n**: `number`

## Returns

`number`

## Example

```
import { limit255 } from "@jimp/utils";
limit255(256); // 255limit255(-1); // 0
```

## Defined in

packages/utils/dist/esm/index.d.ts:73
