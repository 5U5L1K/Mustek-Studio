# Third-party components / Сторонние компоненты

- **SANE backends 1.4.0 / GT68xx**: source archive in
  `downloads/backends-1.4.0.tar.bz2`; changes in `patches/` and `fix_*.py`.
  The archive includes SANE's `COPYING` and `LICENSE` notices. Copies of
  dependency notices are also embedded in the installed application.
  [Upstream source](https://gitlab.com/sane-project/backends/-/tree/1.4.0).
- **PDFsharp 1.50.5147**: MIT; see `app/PDFsharp-LICENSE.txt`.
  [Project](https://github.com/empira/PDFsharp).
- **Zadig 2.9 / libwdi 1.5.1**: see `setup/ZADIG-NOTICE.txt` for the GPL/LGPL
  notices, source links and the original binary checksum. The setup contains
  the original signed binary.
- **Native runtime libraries**: package URLs and checksums are recorded in
  `downloads/dependencies.json`. License notices are embedded in the
  application payload under `licenses/`; the restore script puts them in
  `runtime/ucrt64/share/licenses/` and `share/doc/sane-backends/`.
- **SBfw.usb scanner firmware**: manufacturer firmware; provenance in
  `firmware/provenance.json`. Manufacturer rights remain with their owners.
  It is included in the setup's application payload.

Third-party licenses remain applicable to their respective components.
Сторонние лицензии сохраняют действие для соответствующих компонентов.
