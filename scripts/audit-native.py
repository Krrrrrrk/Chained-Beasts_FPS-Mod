"""Read-only native-entry audit for this prototype; never starts the game."""
from collections import Counter
from pathlib import Path
import hashlib
import json
import os
import re
import struct
import subprocess
import shutil

ROOT = Path(__file__).resolve().parents[1]
GAME = Path(os.environ.get("CHAINED_BEASTS_GAME_DIR", r"C:\Program Files (x86)\Steam\steamapps\common\Chained Beasts"))
ILSPY = os.environ.get("ILSPY_PATH") or shutil.which("ilspycmd")
if not ILSPY:
    raise SystemExit("Install ilspycmd or set ILSPY_PATH to its executable before running this audit.")
EXPECTED_HASH = "ba3ef02fa87da4c41bb27d7610539c9578132204f0fb5713833e2ef93415ce29"
TARGETS = {"Player": ["GetModifiedPlayerInput", "GetMouseGroundPos"],
           "NetworkAgent": ["UpdateModelFacing"]}


def main():
    binary = (GAME / "GameAssembly.dll").read_bytes()
    actual = hashlib.sha256(binary).hexdigest()
    assert actual == EXPECTED_HASH, f"Uninspected game binary: {actual}"
    pe = struct.unpack_from("<I", binary, 0x3C)[0]
    section_count = struct.unpack_from("<H", binary, pe + 6)[0]
    optional_size = struct.unpack_from("<H", binary, pe + 20)[0]
    image_base = struct.unpack_from("<Q", binary, pe + 48)[0]
    sections = [struct.unpack_from("<IIII", binary, pe + 24 + optional_size + i * 40 + 8)
                for i in range(section_count)]

    def offset(address):
        for _, rva, size, raw in sections:
            if rva <= address - image_base < rva + size:
                return raw + address - image_base - rva
        raise ValueError(hex(address))

    def address(raw):
        for _, rva, size, start in sections:
            if start <= raw < start + size:
                return image_base + rva + raw - start
        raise ValueError(raw)

    name = binary.index(b"Assembly-CSharp.dll\0")
    needle = struct.pack("<Q", address(name))
    cursor = 0
    tables = []
    while (cursor := binary.find(needle, cursor)) >= 0:
        try:
            count = struct.unpack_from("<I", binary, cursor + 8)[0]
            pointer = struct.unpack_from("<Q", binary, cursor + 16)[0]
            if 5000 < count < 200000:
                tables.append(struct.unpack_from("<" + str(count) + "Q", binary, offset(pointer)))
        except (ValueError, struct.error):
            pass
        cursor += 8
    assert len(tables) == 1
    table = tables[0]
    counts = Counter(table)
    entries = []
    for owner, names in TARGETS.items():
        # Decompile CURRENT installed bindings, rather than relying on old snapshots.
        result = subprocess.run([str(ILSPY), "-t", owner,
                                 str(GAME / "BepInEx/interop/Assembly-CSharp.dll")],
                                capture_output=True, text=True, check=True)
        for method in names:
            matches = re.findall(r"NativeMethodInfoPtr_(" + method + r"_\w+) = "
                                 r"IL2CPP.GetIl2CppMethodByToken\([^;]*?, (\d+)\);", result.stdout)
            assert len(matches) == 1, (owner, method, matches)
            field, token_text = matches[0]
            token = int(token_text)
            entry = table[(token & 0xFFFFFF) - 1]
            assert entry and counts[entry] == 1, (method, "shared native entry", counts[entry])
            first_bytes = binary[offset(entry):offset(entry) + 16]
            assert first_bytes[0] not in (0xC2, 0xC3), (method, "return stub")
            entries.append(dict(type=owner, method=method, token=token, rva=hex(entry - image_base),
                                sharedEntryCount=counts[entry], firstBytes=first_bytes.hex()))
    report = dict(gameAssemblySha256=actual, interopSha256=hashlib.sha256(
        (GAME / "BepInEx/interop/Assembly-CSharp.dll").read_bytes()).hexdigest(), hooks=entries,
        runtimeVerified=False)
    output = ROOT / "artifacts/native-audit.json"
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
