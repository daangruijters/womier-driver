# Releasing

Releases are built by GitHub Actions ([release.yml](../.github/workflows/release.yml)) and packed with
[Velopack](https://velopack.io). Installed copies of the driver find a new release on GitHub by themselves.

## A new version

1. Set `<Version>` in `Directory.Build.props` to the new version and merge to `main` (the tag decides the version
   of the release; the props file only keeps local builds honest).
2. Write the release notes in Markdown to a file outside the repository and tag `main` with it. Without
   `--cleanup=verbatim` git drops every line starting with `#`, so the headings would disappear:

   ```powershell
   git fetch origin
   git tag -a v0.3.0 --cleanup=verbatim -F $env:TEMP
otes-0.3.0.md origin/main
   git push origin v0.3.0
   ```

3. The workflow tests, publishes the self-contained app, downloads the previous release (for a delta package),
   runs `vpk pack` and `vpk upload github`. The release gets:

   | Asset | Purpose |
   |---|---|
   | `WomierDriver-win-Setup.exe` | per-user installer (also what winget runs) |
   | `WomierDriver-win-Portable.zip` | no installation; updates itself in its own folder |
   | `WomierDriver-<version>-full.nupkg`, `-delta.nupkg` | update packages |
   | `releases.win.json`, `RELEASES`, `assets.win.json` | the feed the driver reads |

   Do not rename or delete these assets on a published release: installed copies read the feed from the latest
   release. A broken release is fixed by publishing a newer version, not by editing the old one.

4. If the winget package exists and the `WINGET_TOKEN` secret is set, the `winget` job opens a pull request in
   [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) for the new version.

## Testing the update flow locally

Pack two versions into one folder and let the older one update itself from there. Installing touches the real
`HKCU\…\Run\WomierDriver` entry (the installer points it at the installed copy, the uninstaller removes it), so
note its value first and restore it afterwards.

```powershell
dotnet tool install vpk --version 1.2.161 --tool-path C:\temp\vpk
foreach ($v in "0.9.0", "0.9.1") {
  dotnet publish src/WomierDriver.App -c Release -r win-x64 --self-contained true -p:Version=$v -o C:\temp\pub-$v
  C:\temp\vpk\vpk.exe pack -u WomierDriver -v $v -p C:\temp\pub-$v -e WomierDriver.exe -o C:\temp\releases
  if ($v -eq "0.9.0") { Copy-Item C:\temp\releases\WomierDriver-win-Setup.exe C:\temp\Setup-0.9.0.exe }
}
$env:WOMIER_DRIVER_DATA = "C:\temp\womier"              # a copy of %APPDATA%\WomierDriver
$env:WOMIER_DRIVER_UPDATE_SOURCE = "C:\temp\releases"
C:\temp\Setup-0.9.0.exe --silent
& "$env:LOCALAPPDATA\WomierDriver\current\WomierDriver.exe"
# after a minute: "Restart to update to 0.9.1" under the version; click it, the log shows "driver 0.9.1"
& "$env:LOCALAPPDATA\WomierDriver\Update.exe" --silent uninstall
```

## Winget

The package identifier is `DaanGruijters.WomierDriver`. Anyone may submit a package; there is no publisher account.

**First submission (once, by hand)** – the manifests in [packaging/winget](../packaging/winget) are filled in
for v0.2.0.

1. For another version, update the version, the URLs and `InstallerSha256`:

   ```powershell
   Invoke-WebRequest https://github.com/daangruijters/womier-driver/releases/download/v0.2.0/WomierDriver-win-Setup.exe -OutFile Setup.exe
   (Get-FileHash Setup.exe).Hash
   ```

2. Check the manifest and try it locally (`winget settings --enable LocalManifestFiles` once, as administrator):

   ```powershell
   winget validate --manifest packaging/winget
   winget install --manifest packaging/winget
   ```

3. Submit with [wingetcreate](https://github.com/microsoft/winget-create) (`winget install Microsoft.WingetCreate`).
   It forks winget-pkgs under your account and opens the pull request:

   ```powershell
   wingetcreate submit packaging/winget --token <personal access token with public_repo>
   ```

   Microsoft's pipeline validates and scans the installer; a moderator merges it, usually within a few days.

**Later versions** – create a classic personal access token with the `public_repo` scope and store it as the
repository secret `WINGET_TOKEN`. From then on every release submits its update automatically. Without the
secret the job is skipped.

Winget only installs; the driver updates itself afterwards. `winget upgrade` still works and simply installs
the newest release over it (the installer stops a running driver first).

## Code signing

Releases are not signed, so SmartScreen warns on the first download. Options for a private individual in the EU:
the free [SignPath Foundation](https://signpath.org) programme for open-source projects (signs in CI; the
certificate names SignPath Foundation), or a Certum *Open Source Code Signing* certificate. Azure Artifact
Signing accepts individuals only in the US and Canada. `vpk pack` signs with `--signParams` (signtool) or
`--azureTrustedSignFile`.
