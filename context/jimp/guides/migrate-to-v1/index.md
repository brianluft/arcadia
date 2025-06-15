# Migrating to v1

The goals of v1 were to:

1.  Make jimp easier to use in any environment
2.  Make jimp’s API more consistent and easier to use
3.  Many constants have been removed and are string value powered by TS

## Async/Sync

In v0 of jimp there were a mix of async and sync methods for export. In
v1 all “export” methods are async.

They have also been renamed:

- `getBufferAsync` -\> `getBuffer`
- `getBase64Async` -\> `getBase64`
- `writeAsync` -\> `write`

## Importing

`Jimp` no longer uses a default export. Instead it uses named exports.

```
import { Jimp } from "jimp";
```

## Positional Arguments to Options Objects

In jimp v0 there were many ways to provide arguments to a method. Most
methods used positional arguments, which leads to code thats harder to
read and extend.

For example the resize method used to look like this:

```
image.resize(100, 100);
```

Now it looks like this:

```
image.resize({ w: 100, h: 100 });
```

## `Jimp` Constructor

The constructor for `Jimp` has changed. Much in the same vein as above,
the constructor now takes an options object.

To create and empty jimp image:

```
import { Jimp } from "jimp";
const image = new Jimp({ width: 100, height: 100 });
```

Even give it a background color:

```
const image = new Jimp({ width: 100, height: 100, color: 0xff0000ff });
```

### `Jimp.read`

In v0 of jimp the constructor was async! This is a huge anit-pattern so
it had to go.

Now you should instead use the `Jimp.read` method.

In node environments it will read a file from disk.

```
import { Jimp } from "jimp";
async function main() {  const image = await Jimp.read("test/image.png");}
```

In the browser it fetch the file from the url.

```
import { Jimp } from "jimp";
async function main() {  const image = await Jimp.read("https://example.com/image.png");}
```

It will also read from a `Buffer` or `ArrayBuffer`.

### `Jimp.fromBuffer`

You can load an image from a buffer. In v0 this was done through the
constructor. In v1 it is done through the `Jimp.fromBuffer` method.

```
import { Jimp } from "jimp";
async function main() {  const image = await Jimp.fromBuffer(buffer);}
```

### `Jimp.fromBitmap`

You can load an image (or canvas data) from a bitmap. In v0 this was
done through the constructor. In v1 it is done through the
`Jimp.fromBitmap` method.

```
import { Jimp } from "jimp";
async function main() {  const canvas = document.getElementById("my-canvas");  const ctx = canvas.getContext("2d");
  const image = await Jimp.fromBitmap(    ctx.getImageData(0, 0, canvas.width, canvas.height)  );}
```

## Encoding and Decoding Options

Another area where the API has changed is the way that encodings and
decoding are handled.

Previously the options were global and it was confusing where they might
be applied (unless you have experience with the underlying image
codecs).

For example in v0 if you wanted to export a JPEG with the quality set to
80% you would do this:

```
import { Jimp } from "jimp";
const image = new Jimp(...);
const resized = await image  .resize(512, Jimp.AUTO)  .quality(80)  .getBuffer(Jimp.MIME_JPEG);
```

In v1 the options are passed when you get the encoded image:

```
import { Jimp } from "jimp";
const image = new Jimp(...);
const resized = await image  .resize({ w: 512 })  .getBuffer('image/jpeg', { quality: 80 });
```

## Removed Constants

Most constants have been moved to named exports.

Other changes:

- `Jimp.AUTO` - This constant was only needed for positional arguments.
  It is no longer needed with the new API.

### `Jimp.MIME_*`

These have moved to a named export `JimpMime`.

```
import { JimpMime } from "jimp";
JimpMime.jpeg;
```

## Moved Functions

- `Jimp.intToRGBA` was moved `import { intToRGBA } from "jimp";`
- `image.getHeight()` was moved `image.height`
- `image.getWidth()` was moved `image.width`
- `image.getMIME()` was moved `image.mime`

[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNyAxMUg5LjQxbDMuMy0zLjI5YTEuMDA0IDEuMDA0IDAgMSAwLTEuNDItMS40MmwtNSA1YTEgMSAwIDAgMC0uMjEuMzMgMSAxIDAgMCAwIDAgLjc2IDEgMSAwIDAgMCAuMjEuMzNsNSA1YTEuMDAyIDEuMDAyIDAgMCAwIDEuNjM5LS4zMjUgMSAxIDAgMCAwLS4yMTktMS4wOTVMOS40MSAxM0gxN2ExIDEgMCAwIDAgMC0yWiIgLz48L3N2Zz4=)
Previous  
Custom Jimp ](../custom-jimp/index.md)
[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNy45MiAxMS42MmExLjAwMSAxLjAwMSAwIDAgMC0uMjEtLjMzbC01LTVhMS4wMDMgMS4wMDMgMCAxIDAtMS40MiAxLjQybDMuMyAzLjI5SDdhMSAxIDAgMCAwIDAgMmg3LjU5bC0zLjMgMy4yOWExLjAwMiAxLjAwMiAwIDAgMCAuMzI1IDEuNjM5IDEgMSAwIDAgMCAxLjA5NS0uMjE5bDUtNWExIDEgMCAwIDAgLjIxLS4zMyAxIDEgMCAwIDAgMC0uNzZaIiAvPjwvc3ZnPg==)
Next  
WEBP/WASM ](../webp/index.md)
