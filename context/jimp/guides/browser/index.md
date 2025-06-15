# Using in Browser

Jimp can be used anywhere that javascript is supported. It can be used
in the browser, in Node.js, or in a web worker.

Jimp comes with a pre-bundled browser version. To use it simply import
`jimp` instead.

  

How to use in the browser

```
import React, { useEffect, useState } from "react";
import { Jimp } from "jimp";
export function GrayscaleExample() {  const [selectedFile, setSelectedFile] = useState("");  const [output, setOutput] = React.useState("");
  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {    const file = e.target.files?.[0];
    if (!file) {      return;    }
    const reader = new FileReader();
    reader.onload = async (e) => {      const data = e.target?.result;
      if (!data || !(data instanceof ArrayBuffer)) {        return;      }
      // Manipulate images uploaded directly from the website.      const image = await Jimp.fromBuffer(data);
      image.greyscale();
      setSelectedFile(URL.createObjectURL(file));      setOutput(await image.getBase64("image/png"));    };
    reader.readAsArrayBuffer(file);  }
  useEffect(() => {    // Or load images hosted on the same domain.    Jimp.read("/jimp/dice.png").then(async (image) => {      setSelectedFile(await image.getBase64("image/png"));      image.greyscale();      setOutput(await image.getBase64("image/png"));    });  }, []);
  return (    <div>      {/* A file input that takes a png/jpeg */}      <input type="file" accept="image/*" onChange={handleFile} />
      <div        style={{          display: "flex",          alignItems: "center",          gap: 20,          width: "100%",        }}      >        {selectedFile && (          <img            style={{ flex: 1, minWidth: 0, objectFit: "contain", margin: 0 }}            src={selectedFile}            alt="Input"          />        )}        {output && (          <img            style={{ flex: 1, minWidth: 0, objectFit: "contain", margin: 0 }}            src={output}            alt="Output"          />        )}      </div>    </div>  );}
```

example.jsx

## Usage

There are a few main ways to use Jimp in the browser.

### With hosted file

You can initialize a Jimp instance from a URL or a file path.

```
import { Jimp } from "jimp";
// Read a file hosted on the same domainconst image1 = await Jimp.read("/some/url");
// Read a file hosted on a different domainconst image2 = await Jimp.read("https://some.other.domain/some/url");
```

### With uploaded files

Or you can use Jimp with an `ArrayBuffer`. Here we take a user’s
uploaded image and modify it to greyscale.

```
function handleFile(e: React.ChangeEvent<HTMLInputElement>) {  const reader = new FileReader();
  reader.onload = async (e) => {    const image = await Jimp.fromBuffer(e.target?.result);
    image.greyscale();
    const base64 = await image.getBase64("image/jpeg");  };
  reader.readAsArrayBuffer(e.target.files[0]);}
input.addEventListener("change", handleFile);
```

### Using Canvas

You can also use Jimp with a canvas.

```
const canvas = document.getElementById("my-canvas");const ctx = canvas.getContext("2d");
// Load the canvas into a Jimp instanceconst image = await Jimp.fromBitmap(  ctx.getImageData(0, 0, canvas.width, canvas.height));
// Manipulate the imageimage.greyscale();
const imageData = new ImageData(  new Uint8ClampedArray(image.bitmap.data),  image.bitmap.width,  image.bitmap.height);
// Write back to the canvasctx.putImageData(imageData, 0, 0);
```

## Using Fonts

Jimp supports loading fonts from a URL or a file path. You must host the
fonts and will not be able to use the ones included in the node version
of Jimp.

> PRs welcome!

## Web Workers

Jimp can be slow and you don’t want that running on the main thread.
Workers can make this experience a lot better.

First define a worker. This is where you should import jimp and do your
image transformations.

```
import { Jimp, loadFont } from "jimp";
// eslint-disable-next-line @typescript-eslint/no-explicit-anyconst ctx: Worker = self as any;
ctx.addEventListener("message", async (e) => {  // Initialize Jimp  const image = await Jimp.fromBuffer(e.data.image);  const options = e.data.options;
  // Manipulate the image  if (options.blur) {    image.blur(options.blur);  }
  // Return the result  ctx.postMessage({ base64: await image.getBase64("image/png") });});
```

Then you can use the worker.

```
const fileData: ArrayBuffer = new ArrayBuffer(); // Your image dataconst worker = new Worker(new URL("./jimp.worker.ts", import.meta.url), {  type: "module",});
worker.postMessage({  image: fileData,  options: {    blur: 8  },});
worker.addEventListener("message", (e) => {  setOutput(e.data.base64);  setIsLoading(false);});
```

[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNyAxMUg5LjQxbDMuMy0zLjI5YTEuMDA0IDEuMDA0IDAgMSAwLTEuNDItMS40MmwtNSA1YTEgMSAwIDAgMC0uMjEuMzMgMSAxIDAgMCAwIDAgLjc2IDEgMSAwIDAgMCAuMjEuMzNsNSA1YTEuMDAyIDEuMDAyIDAgMCAwIDEuNjM5LS4zMjUgMSAxIDAgMCAwLS4yMTktMS4wOTVMOS40MSAxM0gxN2ExIDEgMCAwIDAgMC0yWiIgLz48L3N2Zz4=)
Previous  
Getting Started ](../getting-started/index.md)
[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNy45MiAxMS42MmExLjAwMSAxLjAwMSAwIDAgMC0uMjEtLjMzbC01LTVhMS4wMDMgMS4wMDMgMCAxIDAtMS40MiAxLjQybDMuMyAzLjI5SDdhMSAxIDAgMCAwIDAgMmg3LjU5bC0zLjMgMy4yOWExLjAwMiAxLjAwMiAwIDAgMCAuMzI1IDEuNjM5IDEgMSAwIDAgMCAxLjA5NS0uMjE5bDUtNWExIDEgMCAwIDAgLjIxLS4zMyAxIDEgMCAwIDAgMC0uNzZaIiAvPjwvc3ZnPg==)
Next  
Writing Plugins ](../writing-plugins/index.md)
