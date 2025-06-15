# Writing Plugins

There are two types of plugins you can write for Jimp:

1.  Image format plugins
2.  Image manipulation plugins

You can combine plugins to create a [Custom
Jimp](https://jimp-dev.github.io/guides/custom-jimp).

## Image format plugins

Image format plugins are responsible for loading and saving images in a
specific format.

For example, the
[bmp](https://github.com/jimp-dev/jimp/tree/main/packages/js-bmp)
package is responsible for loading and saving
[BMP](https://en.wikipedia.org/wiki/BMP_file_format) images.

An image format plugins is composed of two functions: `encode` and
`decode`.

```
import { Format } from "@jimp/types";import WEBP, { WEBPOptions } from "webp-js";
export default function webp() {  return {    mime: "image/webp",    // Encode the bitmap as a WEBP image    encode: (bitmap, options: WEBPOptions = {}) =>      WEBP.encode(bitmap, quality).data,    // Decode a WEBP image into a bitmap    decode: (data) => WEBP.decode(data),  } satisfies Format<"image/webp">;}
```

## Image manipulation plugins

Image manipulation plugins are responsible for manipulating images.

For example, the
[blur](https://github.com/jimp-dev/jimp/tree/main/packages/plugin-blur)
package is responsible for applying a blur effect to an image.

An image manipulation method takes an image as its first argument, does
something to it, and returns a new image.

```
import { JimpClass } from "@jimp/types";import { BlurOptions } from "blur-ts";
export const methods = {  /**   * A blur effect   *   * @example   * ```ts   * import { Jimp } from "jimp";   *   * const image = await Jimp.read("test/image.png");   *   * image.blur(5);   * ```   */  blur<I extends JimpClass>(image: I, r: number) {    // Implementation  },};
```

Generally our method APIs follow the following pattern:

- They can take 1 primitive as an option (other than `image`)
- Otherwise they take and options object defined and validated by `zod`.

[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNyAxMUg5LjQxbDMuMy0zLjI5YTEuMDA0IDEuMDA0IDAgMSAwLTEuNDItMS40MmwtNSA1YTEgMSAwIDAgMC0uMjEuMzMgMSAxIDAgMCAwIDAgLjc2IDEgMSAwIDAgMCAuMjEuMzNsNSA1YTEuMDAyIDEuMDAyIDAgMCAwIDEuNjM5LS4zMjUgMSAxIDAgMCAwLS4yMTktMS4wOTVMOS40MSAxM0gxN2ExIDEgMCAwIDAgMC0yWiIgLz48L3N2Zz4=)
Previous  
Using in Browser ](../browser/index.md)
[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNy45MiAxMS42MmExLjAwMSAxLjAwMSAwIDAgMC0uMjEtLjMzbC01LTVhMS4wMDMgMS4wMDMgMCAxIDAtMS40MiAxLjQybDMuMyAzLjI5SDdhMSAxIDAgMCAwIDAgMmg3LjU5bC0zLjMgMy4yOWExLjAwMiAxLjAwMiAwIDAgMCAuMzI1IDEuNjM5IDEgMSAwIDAgMCAxLjA5NS0uMjE5bDUtNWExIDEgMCAwIDAgLjIxLS4zMyAxIDEgMCAwIDAgMC0uNzZaIiAvPjwvc3ZnPg==)
Next  
Custom Jimp ](../custom-jimp/index.md)
