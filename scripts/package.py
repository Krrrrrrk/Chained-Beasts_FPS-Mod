"""Build an allowlisted local test package; never installs or starts anything."""
from pathlib import Path
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[1]
VERSION = "0.2.0"


def main():
    dll = ROOT / "src/ChainedBeasts.FirstPerson/bin/Release/net6.0/ChainedBeasts.FirstPerson.dll"
    audit = json.loads((ROOT / "artifacts/native-audit.json").read_text())
    sha = lambda data: hashlib.sha256(data).hexdigest()
    data = dll.read_bytes()
    readme = (ROOT / "README.md").read_bytes()
    manifest = {
        "name": "Chained Beasts First Person", "version": VERSION, "channel": "local-experimental",
        "gameVersion": "1.0.2", "steamBuild": "26231777", "toggle": "F9",
        "startsInThirdPerson": True, "gameAssemblySha256": audit["gameAssemblySha256"],
        "dllSha256": sha(data), "readmeSha256": sha(readme), "runtimeVerified": False,
        "multiplayerRuntimeVerified": False, "installedByPackaging": False,
    }
    entries = {
        "BepInEx/plugins/ChainedBeasts.FirstPerson.dll": data,
        "README.md": readme,
        "manifest.json": (json.dumps(manifest, indent=2) + "\n").encode(),
    }
    for name in ("docs/CONFIGURATION.md", "docs/DEVELOPMENT.md", "CHANGELOG.md", "LICENSE",
                 "docs/screenshots/first-person-enemies.png", "docs/screenshots/first-person-duel.png"):
        entries[name] = (ROOT / name).read_bytes()
    output = ROOT / f"artifacts/ChainedBeasts-FirstPerson-{VERSION}-experimental.zip"
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, content in entries.items():
            archive.writestr(name, content)
    # Independently reopen and enforce the exact delivery allowlist and every payload byte.
    with zipfile.ZipFile(output) as archive:
        assert len(archive.infolist()) == len(entries)
        assert set(archive.namelist()) == set(entries)
        assert archive.testzip() is None
        for name, content in entries.items():
            assert archive.read(name) == content, name
    manifest["zipSha256"] = sha(output.read_bytes())
    (ROOT / "artifacts/package-audit.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps(dict(zip=str(output), **manifest), indent=2))


if __name__ == "__main__":
    main()
