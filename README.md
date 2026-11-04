# Mustek Studio

[Русская инструкция](README-RU.md) · [English instructions](README.md)

A scanner application for the **Mustek ScanExpress 1200 UB Plus**, USB ID
**05D8:4002**, on **Windows 10 x64**. Other models sharing this USB ID have
not been verified.

## Download and install

1. Download [MustekStudio-Setup.exe](release/MustekStudio-Setup.exe). On the
   GitHub file page, click **Download raw file**.
2. Run the installer. Choose English or Russian and a folder writable by
   your Windows account. The default is `%LOCALAPPDATA%\Programs\MustekStudio`.
3. Optionally create a desktop shortcut. Keep **Complete scanner setup and
   open Mustek Studio** selected on the final page.
4. If the connection assistant appears, connect the scanner and its power
   supply. Click **Configure WinUSB** and approve the Windows prompt.
5. In the bundled Zadig, select the scanner with **USB ID 05D8:4002**. If it
   is hidden, use **Options → List All Devices**. Choose **WinUSB** on the
   right and click **Install Driver** or **Replace Driver**.
6. When installation finishes, open Mustek Studio. You can reopen the
   connection assistant from the Mustek Studio group in the Start menu.

Installing the application does not require administrator privileges;
binding WinUSB requires a separate administrator confirmation. The installer
includes Zadig and the scanning components and works offline. Python and
Visual Studio are not needed to use the application.

To check the installer checksum in PowerShell:

```powershell
Get-FileHash .\MustekStudio-Setup.exe -Algorithm SHA256
```

Compare it with [release/Setup-SHA256.txt](release/Setup-SHA256.txt).

## Preview and scan

1. Put the document face down on the glass and close the lid.
2. Select **Preview resolution** and click **Preview**. A preview is not
   added to the document. Mouse wheel, **+**, **−**, **100%** and **Fit**
   change the display scale, not the scanning DPI.
3. Select **Scan resolution** and **Color mode**, then click **Scan page**.
4. Replace the sheet and scan again to add another page. Use the thumbnail
   strip to select, reorder, rotate or remove pages.
5. Select **PDF**, **All pages** and click **Save PDF…** to create one
   multipage document. **Selected page** exports only the selected page.
6. Select **PNG** to save separate images. You can also import PNG, JPEG
   and BMP using **Add images…** or drag and drop.

Available DPI settings are 50, 75, 150, 300, 600 and 1200. Higher resolutions
take more time and memory. Previous hardware checks covered 75 dpi preview
and 150 dpi color scanning; availability in the menu does not imply that
every mode has been tested on hardware. PDF export supports original scan
size and A4 with 5 mm margins. OCR is not included.

## Language, theme and saved work

Open **Settings…** in the top right to choose Russian or English and a light
or dark theme. Settings apply without restarting. Dark mode uses purple
primary buttons. Scanned paper keeps its original colors.

The bottom right credit changes with the interface language:
`@Навайбкодил 5U5L1K` / `@Vibe-coded by 5U5L1K`.

The installed application stores its working document, page images,
preferences and logs in `MustekStudio.Data` beside `MustekStudio.exe`.
Updates and uninstallation preserve this directory. Back it up together
with exported documents. Remove previous pages from the strip before
starting a separate document. Removing a page from the strip does not
delete its working copy.

If the scanner is not found, check power, USB and the WinUSB binding for
05D8:4002, then use **Check scanner**. Reopen the connection assistant if
needed. An incomplete scan or export is reported as an error.

## Source layout

- `app/`: C# WinForms application, localization, themes, icon, build scripts
  and existing tests.
- `setup/`: Inno Setup script, connection assistant and installation text.
- `patches/`, `fix_*.py`, `*-fix.json`: native SANE fixes and their hashes.
- `downloads/backends-1.4.0.tar.bz2`: upstream SANE source archive.
- `release/`: the setup executable and its SHA256 checksum.
- `Restore-Dependencies.ps1`: restores local build dependencies from an
  installed copy of this setup, without committing DLLs to the repository.

This branch does not include standalone portable or development EXEs,
scanner output, user profiles, logs or unpacked runtime binaries. The setup
contains the self-contained application it installs.

## Build from source

Use Windows x64, Windows PowerShell 5.1 and the .NET Framework C# compiler
included with Windows 10. Python 3 is required for portable packaging.
The native dependencies are restored from the supplied installer; this
workflow does not rebuild SANE from its upstream C sources.

1. Install the supplied setup. No scan is needed to restore dependencies.
2. Open **Windows PowerShell** in the root of this source checkout:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Dependencies.ps1
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\app\build.ps1
   ```

   For a custom installation path, pass
   `-InstalledExe 'D:\Apps\MustekStudio\MustekStudio.exe'` to the restore script.
   Start the development build with `Start-Scanner.cmd`.
3. To build the portable application, make `python.exe` available in PATH:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\app\Build-Portable.ps1
   ```

4. To rebuild setup, place the Inno Setup 7 compiler and its supporting
   files in `tools\InnoSetup7` (`ISCC.exe` must be there), then run:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\setup\Build-Setup.ps1
   ```

   The restored, signed Zadig binary is checked by hash and signature.
   Build portable first because setup packages that executable.

The existing `app/test.ps1` and `app/test-appearance.ps1` create fixture
documents and check application behavior. Hardware test scripts perform
real scans and require the connected scanner. New builds and restored
dependencies are excluded by `.gitignore`.

See [THIRD-PARTY.md](THIRD-PARTY.md) for dependency notices.
