# Require absolute storage path
Currently `config.jsonc` has a mandatory section:
  ```
  "storage": {
    "directory": "./storage/"
  },
  ```

We support relative paths now. It's unclear what it's relative _to_, and we have bugs relating to this. Rather than fix it, we will ban relative paths and instead make the "storage" section optional. If not specified, the path will be "../storage/" relative to `dotnet/Logs.exe` or `server/index.js` (same thing, and they are the two users of config.jsonc). NOT relative to the current working directory and NOT relative to `config.jsonc` itself. If it is specified in the `config.jsonc`, then it must be an absolute path. Furthermore, require a Windows-style path like `C:\...` or `C:/...`.

- [ ] Implement the change in `server/` to require an absolute path if a storage directory is specified, otherwise it is `../storage/` relative to the `index.js`; that is, `storage` and `server` are siblings.
- [ ] Implement the change in `dotnet/Logs/` to require an absolute path if a storage directory is specified, otherwise it is `../storage/` relative to the `Logs.exe`; that is, `storage` and `dotnet` are siblings.
- [ ] Update our example `config.jsonc` to comment out the `storage` section. Change the directory to `C:\\Tools\\arcadia\\storage` as an example, but still commented out.
- [ ] Update `server/INSTALLING.md`.
