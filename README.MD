# ![icon](./mp32descR/Asset/Icon/icon.ico) mp32descR

> [!WARNING]
> Under construction

* TODO
  * [x] Použít písmo, které má alespoň monospace čísla
  * [x] {Duration} v sekundách nic moc
  * [x] App icon
  * [x] Windows build
  * [x] Linux build
  * [x] About
  * [ ] GitHub Readme
  * [x] Duplicate checker

## Dependencies

* .NET 10
* [Avalonia](https://github.com/AvaloniaUI/Avalonia) ([MIT](https://github.com/AvaloniaUI/Avalonia/blob/main/licence.md))
* [Zeugma440/atldotnet](https://github.com/Zeugma440/atldotnet) ([MIT](https://github.com/Zeugma440/atldotnet/blob/main/LICENSE))
* [Inconsolata](https://levien.com/type/myfonts/inconsolata.html) ([SIL Open Font License](https://github.com/google/fonts/blob/main/ofl/inconsolata/OFL.txt))

## Building locally

Prerequisite: [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Build & run on Linux

```bash
make
cd PUBLISHED/
./mp32descR
```

### Build & run on Windows

```powershell
.\makew.ps1
cd .\PUBLISHED\
.\mp32descR.exe
```
