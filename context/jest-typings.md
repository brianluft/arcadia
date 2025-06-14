# Question

> Jest 30 (@jest/types) only provides types for Jest configuration and internal Jest APIs, but not for the global testing functions like describe, it, and expect. 
> According to the official Jest documentation, there are two recommended approaches:
> - Use import {describe, expect, it} from '@jest/globals' to explicitly import the globals
> - Install @types/jest for implicit globals (which is NOT outdated - version 29.5.14 was published 8 months ago and is actively maintained)

Is this true?

# Answer

### What that other answer gets **right**

| Claim                                                                              | Reality                                                                                                                 |
| ---------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| **“`@jest/types` only contains config and internal-API typings.”**                 | Correct. That package is a low-level building-block; it deliberately *doesn’t* declare `describe`, `it`, `expect`, etc. |
| **“Docs propose two ways: import from `@jest/globals`, or install `@types/jest`.** | Also correct. The current *Getting-Started → TypeScript* section shows exactly those two options ([jestjs.io][1]).      |

### …and what it **misses**

1. **Jest 30 ships *complete* test-API typings—just not in `@jest/types`.**
   They live in two other packages that install alongside `jest`:

   | Package         | What it gives you                                                                                                                               |
   | --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
   | `@jest/globals` | **E S M/CommonJS module** that exports `describe`, `it/test`, `expect`, `beforeAll`, … *with types attached*. You import the functions you use: |

   ````ts
   import {describe, it, expect} from '@jest/globals';
   ``` |
   | `jest` (root package) | Contains a `build/jest.d.ts` that *re-exports* those same declarations and can be pulled in as an ambient type by adding `"types": ["jest"]` to `tsconfig.json`. |

   So you **do not need `@types/jest`** to get typed globals; it’s just an alternative convenience layer.

   ````

2. **`@types/jest` lags the core release.**
   The latest publish is **29.5.14 (Oct 2024, 8 months ago)** ([npmjs.com][2]), while Jest itself is **30.0.0 (Jun 2025)** ([jestjs.io][3]).  It will compile against Jest 30, but any brand-new APIs or matcher-type tweaks (e.g. the stricter `CalledWith` inference added in 30) won’t appear until volunteers update the DefinitelyTyped package.

3. **The docs explicitly warn about that lag.**
   The same TypeScript section notes that `@types/jest` “may not cover the latest Jest features or versions” and advises matching major versions when you do use it ([jestjs.io][1]).

---

## What to do in practice

### ✨ Modern, explicit-import style — *no extra dep*

```bash
npm i -D jest @jest/globals ts-jest typescript
```

**tsconfig.json**

```jsonc
{
  "compilerOptions": {
    // no "types": [] needed – you import everything explicitly
  }
}
```

**sum.test.ts**

```ts
import {describe, it, expect} from '@jest/globals';
import {sum} from './sum';

describe('sum', () => {
  it('adds numbers', () => {
    expect(sum(1, 2)).toBe(3);
  });
});
```

*Pros*: always up-to-date types, no ambient globals, smaller risk of name clashes.
*Cons*: you have to write the imports (VS Code auto-import makes this painless).

---

### 💤 Legacy implicit-globals style — keep `@types/jest`

```bash
npm i -D jest@29 @types/jest@29 ts-jest@29
```

or wait until `@types/jest` publishes a **30.x** line, then:

```bash
npm i -D jest @types/jest@30
```

Add (or keep) `"types": ["jest", "node"]` in **tsconfig.json** and your existing test files continue to compile without adding imports.

---

### ⚖️ Decision matrix

| Need                                                  | Recommended setup                                                                             |
| ----------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Want latest Jest 30 *today* and don’t mind imports    | **Use `@jest/globals`** (no `@types/jest`)                                                    |
| Want to avoid imports and can wait a bit              | Stay on Jest 29 until `@types/jest` 30 arrives                                                |
| Already on Jest 30, large code-base with global style | Add `"types": ["jest"]` or rely on `@types/jest` 29 knowing that very new APIs may be missing |
| Only one or two missing typings                       | Patch with a local `*.d.ts`, or temporarily `skipLibCheck: true`                              |

---

### Bottom line

* `@jest/types` **isn’t supposed to** expose the test helpers; that lives in `@jest/globals`.
* TypeScript users are **not stuck**—you can run Jest 30 today without waiting for DefinitelyTyped.
* Use `@jest/globals` (explicit imports) or `"types": ["jest"]` (ambient). Reach for `@types/jest` only if you truly want the implicit globals and accept the version-lag trade-off.

[1]: https://jestjs.io/docs/getting-started "Getting Started · Jest"
[2]: https://www.npmjs.com/package/%40types/jest?utm_source=chatgpt.com "types/jest - NPM"
[3]: https://jestjs.io/blog/2025/06/04/jest-30 "Jest 30: Faster, Leaner, Better · Jest"
