Below is a compact “recipe” you can drop into a TypeScript project to send **one image + one question** to a multimodal GPT-4o model and read back the answer.  I walk through the pieces afterward so you can adapt it to your own flow.

```ts
// src/askAboutImage.ts
import fs from "node:fs/promises";
import path from "node:path";
import OpenAI from "openai";

/**
 * Ask a GPT-4o model a question about an image.
 */
async function askAboutImage() {
  // 1️⃣  Create the client (reads OPENAI_API_KEY from env)
  const openai = new OpenAI();

  // 2️⃣  Load an image (local file ► base64; could also use a public URL)
  const imagePath = path.resolve("assets/photo.jpg");
  const base64 = await fs.readFile(imagePath, { encoding: "base64" });

  // 3️⃣  Build a “multimodal” chat request
  const completion = await openai.chat.completions.create({
    model: "gpt-4o-mini",          // full GPT-4o works too
    max_tokens: 300,               // answer budget
    messages: [
      {
        role: "user",
        content: [
          {
            type: "image_url",
            image_url: {
              // EITHER a data-URI…
              url: `data:image/jpeg;base64,${base64}`,
              // …or something like "https://my-cdn.com/photo.jpg"
              detail: "high"       // "auto" (default) | "low" | "high"
            }
          },
          {
            type: "text",
            text: "What breed of dog is in this photo?"
          }
        ]
      }
    ]
  });

  // 4️⃣  Read the answer
  console.log("Assistant:", completion.choices[0].message.content);
}

askAboutImage().catch(console.error);
```

---

### How the key pieces work

| Step  | What it does                                                                                                               | Why it matters                                                                                                                             |
| ----- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **1** | `new OpenAI()` uses the official SDK’s type-safe wrapper and picks up `OPENAI_API_KEY` from environment variables.         | Keeps secrets out of code and gives you full IntelliSense in TS.                                                                           |
| **2** | You can pass an **inline `data:` URI** (shown) or a **public URL** (`"https://..."`).                                      | Both formats appear under the same `image_url` object that the vision‐enabled models expect. ([platform.openai.com][1])                    |
| **3** | Messages are an **array of “content parts.”** Each part can be `text` or `image_url`.                                      | That exact nested shape is required—passing a bare string throws a validation error. ([community.openai.com][2], [platform.openai.com][1]) |
|       | The optional `detail` field controls how many tokens the model spends on the image (`"auto"` ≈ 85 tokens, `"high"` ≈ 400). | Using `"high"` buys sharper analysis but costs more. ([github.com][3])                                                                     |
| **4** | The answer is ordinary text in `choices[0].message.content`.                                                               | From here you can display it, feed it to another function, or stream it in chunks.                                                         |

---

### Quick tips & pitfalls

* **Model names:** use any `gpt-4o-***` or `gpt-4-turbo-***` variant that lists “Vision” support in the model catalog. The lighter-weight `gpt-4o-mini` is perfect for image Q\&A at lower cost. ([cursor-ide.com][4])
* **File size & formats:** JPEG/PNG/WebP up to \~20 MB work best. Very tall or wide images may be auto-rescaled.
* **Token math:** an inline image counts as \~ 85 tokens (`"auto"` detail) before you even ask your question, so keep `max_tokens` generous enough for the answer.
* **Local vs. remote images:** remote URLs must be **publicly reachable**; otherwise convert to base64 or upload to a short-lived object store.
* **Streaming:** add `stream: true` to the request to pipe the answer in real time.
* **Error “only text is supported”:** you either (a) hit a model that lacks vision, or (b) passed a raw string instead of the `{ image_url: { url }}` object.

---

That’s all you need to get started—copy the snippet into a TypeScript project (`npm i openai@latest`) and run `ts-node src/askAboutImage.ts`. You should see the model’s description or identification of the object in your image printed to the console.

Happy building!

[1]: https://platform.openai.com/docs/guides/vision?utm_source=chatgpt.com "OpenAI GPT-4-Vision guide"
[2]: https://community.openai.com/t/image-url-for-gpt-4o-api-giving-error-expected-an-object-but-got-a-string-instead/748188?utm_source=chatgpt.com "Image_url for gpt-4o api giving error \"expected an object, but got a ..."
[3]: https://github.com/vercel/ai/discussions/4967?utm_source=chatgpt.com "Detail parameter for input image (OpenAI Vision) #4967 - GitHub"
[4]: https://www.cursor-ide.com/blog/gpt4o-image-api-guide-2025-english?utm_source=chatgpt.com "Complete Guide to GPT-4o Image API: Vision & Generation [2025 ..."
