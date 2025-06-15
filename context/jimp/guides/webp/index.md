# Using WEBP (And other WASM plugins)

The default build of Jimp only includes image formats written in
javascript. To utilize webp (and anything else we don’t have a JS
implementation for) we need to use format plugins and create a custom
jimp.

```
import { createJimp } from "@jimp/core";import { defaultFormats, defaultPlugins } from "jimp";import webp from "@jimp/wasm-webp";
// A custom jimp that supports webpconst Jimp = createJimp({  formats: [...defaultFormats, webp],  plugins: defaultPlugins,});
```

  

Full code for example

```
import React, { useEffect, useState } from "react";
import { defaultFormats, defaultPlugins } from "jimp";import webp from "@jimp/wasm-webp";import { createJimp } from "@jimp/core";
const Jimp = createJimp({  formats: [...defaultFormats, webp],  plugins: defaultPlugins,});
export function WebpExample() {  const [selectedFile, setSelectedFile] = useState("");  const [output, setOutput] = React.useState("");
  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {    const file = e.target.files?.[0];
    if (!file) {      return;    }
    const reader = new FileReader();
    reader.onload = async (e) => {      const data = e.target?.result;
      if (!data || !(data instanceof ArrayBuffer)) {        return;      }
      // Manipulate images uploaded directly from the website.      const image = await Jimp.fromBuffer(data);      image.quantize({ colors: 16 }).blur(8).pixelate(8);      setSelectedFile(URL.createObjectURL(file));      setOutput(await image.getBase64("image/webp"));    };
    reader.readAsArrayBuffer(file);  }
  useEffect(() => {    // Or load images hosted on the same domain.    Jimp.read("/jimp/tree.webp").then(async (image) => {      setSelectedFile(await image.getBase64("image/png"));      image.quantize({ colors: 16 }).blur(8).pixelate(8);      setOutput(await image.getBase64("image/png"));    });  }, []);
  return (    <div>      {/* A file input that takes a png/jpeg */}      <input type="file" accept="image/webp" onChange={handleFile} />
      <div        style={{          display: "flex",          alignItems: "center",          gap: 20,          width: "100%",        }}      >        {selectedFile && (          <img            style={{ flex: 1, minWidth: 0, objectFit: "contain", margin: 0 }}            src={selectedFile}            alt="Input"          />        )}        {output && (          <img            style={{ flex: 1, minWidth: 0, objectFit: "contain", margin: 0 }}            src={output}            alt="Output"          />        )}      </div>    </div>  );}
```

example.jsx

## Browser Usage

Since you’re no longer using a pre-bundled version of jimp you need
configure your bundler to handle the node code.

For example in vite/astro you can use `vite-plugin-node-polyfills`.

```
import { nodePolyfills } from "vite-plugin-node-polyfills";
export default defineConfig({  plugins: [    // You only need to polyfill buffer if you're using a browser    plugins: [nodePolyfills({ include: ["buffer"] })],  ],});
```

## All WASM Plugins

- [@jimp/wasm-avif](https://github.com/jimp-dev/jimp/tree/main/plugins/wasm-avif)
- [@jimp/wasm-jpeg](https://github.com/jimp-dev/jimp/tree/main/plugins/wasm-jpeg)
- [@jimp/wasm-png](https://github.com/jimp-dev/jimp/tree/main/plugins/wasm-png)
- [@jimp/wasm-webp](https://github.com/jimp-dev/jimp/tree/main/plugins/wasm-webp)

[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNyAxMUg5LjQxbDMuMy0zLjI5YTEuMDA0IDEuMDA0IDAgMSAwLTEuNDItMS40MmwtNSA1YTEgMSAwIDAgMC0uMjEuMzMgMSAxIDAgMCAwIDAgLjc2IDEgMSAwIDAgMCAuMjEuMzNsNSA1YTEuMDAyIDEuMDAyIDAgMCAwIDEuNjM5LS4zMjUgMSAxIDAgMCAwLS4yMTktMS4wOTVMOS40MSAxM0gxN2ExIDEgMCAwIDAgMC0yWiIgLz48L3N2Zz4=)
Previous  
Migrate to v1 ](../migrate-to-v1/index.md)
[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNy45MiAxMS42MmExLjAwMSAxLjAwMSAwIDAgMC0uMjEtLjMzbC01LTVhMS4wMDMgMS4wMDMgMCAxIDAtMS40MiAxLjQybDMuMyAzLjI5SDdhMSAxIDAgMCAwIDAgMmg3LjU5bC0zLjMgMy4yOWExLjAwMiAxLjAwMiAwIDAgMCAuMzI1IDEuNjM5IDEgMSAwIDAgMCAxLjA5NS0uMjE5bDUtNWExIDEgMCAwIDAgLjIxLS4zMyAxIDEgMCAwIDAgMC0uNzZaIiAvPjwvc3ZnPg==)
Next  
Jimp ](../../api/jimp/classes/jimp/index.md)
