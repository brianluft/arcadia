# Warning fixes
- [ ] Fix this build warning: `warning NETSDK1194: The "--output" option isn't supported when building a solution. Specifying a solution-level output path results in all projects copying outputs to the same directory, which can lead to inconsistent builds.`. We will fix it by enabling artifacts output in the default path and then copying the outputs into `build/dotnet/` ourselves.
    - Add `<UseArtifactsOutput>true</UseArtifactsOutput>` to `Database.csproj` and `Logs.csproj`
    - This will cause the build outputs to go to a different path. Figure it out and copy the output files yourself into `build/dotnet/`, overwriting files as needed because we _want_ them to share .NET files.
    - Get `build.sh` and `publish.sh` working again without the NETSDK1194 warning.
- [ ] Fix this test warning: "Skipping read-only directory test (not supported on this platform)" -- don't log anything in that case.
