# Getting Started

Jimp (JavaScript Image Manipulation Program) is a package that makes it
easy to load and manipulate images in the browser and Node.js. It aims
to run anywhere that JavaScript is supported.

[Read the full
documentation](https://jimp-dev.github.io/api/jimp/classes/jimp/) to
learn more about the API.

```
pnpm add jimp
```

Terminal window

Jimp supports the following image formats:

- [bmp](https://en.wikipedia.org/wiki/BMP_file_format)
- [gif](https://en.wikipedia.org/wiki/GIF)
- [jpeg](https://en.wikipedia.org/wiki/JPEG)
- [png](https://en.wikipedia.org/wiki/Portable_Network_Graphics)
- [tiff](https://en.wikipedia.org/wiki/TIFF)

![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9InN0YXJsaWdodC1hc2lkZV9faWNvbiBhc3Ryby15c215bXlibyIgd2lkdGg9IjE2IiBoZWlnaHQ9IjE2IiB2aWV3Ym94PSIwIDAgMjQgMjQiIGZpbGw9ImN1cnJlbnRDb2xvciIgc3R5bGU9Ii0tc2wtaWNvbi1zaXplOiAxZW07Ij48cGF0aCBkPSJNMTIgMTZhMSAxIDAgMSAwIDAgMiAxIDEgMCAwIDAgMC0yWm0xMC42NyAxLjQ3LTguMDUtMTRhMyAzIDAgMCAwLTUuMjQgMGwtOCAxNEEzIDMgMCAwIDAgMy45NCAyMmgxNi4xMmEzIDMgMCAwIDAgMi42MS00LjUzWm0tMS43MyAyYTEgMSAwIDAgMS0uODguNTFIMy45NGExIDEgMCAwIDEtLjg4LS41MSAxIDEgMCAwIDEgMC0xbDgtMTRhMSAxIDAgMCAxIDEuNzggMGw4LjA1IDE0YTEgMSAwIDAgMSAuMDUgMS4wMnYtLjAyWk0xMiA4YTEgMSAwIDAgMC0xIDF2NGExIDEgMCAwIDAgMiAwVjlhMSAxIDAgMCAwLTEtMVoiIC8+PC9zdmc+)
Watch out!

Jimp is built on JavaScript implementations of image formats. These
implementations are not optimized for performance and may allocate a lot
of memory before using.

## Usage

The workflow for using jimp

1.  Load an image.

    ```
    import { Jimp } from "jimp";
    const image = await Jimp.read("test/image.png");
    ```

2.  Manipulate the image.

    ```
    image.resize({ width: 100 });
    ```

3.  Save the image.

    ```
    await image.write("test/output.png");
    ```

[![](data:image/svg+xml;base64,PHN2ZyBhcmlhLWhpZGRlbj0idHJ1ZSIgY2xhc3M9ImFzdHJvLWRpNWdwNnZzIGFzdHJvLXlzbXlteWJvIiB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdib3g9IjAgMCAyNCAyNCIgZmlsbD0iY3VycmVudENvbG9yIiBzdHlsZT0iLS1zbC1pY29uLXNpemU6IDEuNXJlbTsiPjxwYXRoIGQ9Ik0xNy45MiAxMS42MmExLjAwMSAxLjAwMSAwIDAgMC0uMjEtLjMzbC01LTVhMS4wMDMgMS4wMDMgMCAxIDAtMS40MiAxLjQybDMuMyAzLjI5SDdhMSAxIDAgMCAwIDAgMmg3LjU5bC0zLjMgMy4yOWExLjAwMiAxLjAwMiAwIDAgMCAuMzI1IDEuNjM5IDEgMSAwIDAgMCAxLjA5NS0uMjE5bDUtNWExIDEgMCAwIDAgLjIxLS4zMyAxIDEgMCAwIDAgMC0uNzZaIiAvPjwvc3ZnPg==)
Next  
Using in Browser ](../browser/index.md)
