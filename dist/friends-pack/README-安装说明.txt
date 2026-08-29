StS1 return layer plus AFTP multiplayer fix pack v4
Build date: 2026-08-29

1. Contents

mods/Spire1/
  StS1 content layer.
  Ironclad, Silent, and Defect are enabled by default.
  306 card classes, StS1 relics, potions, events, and optional acts.

mods/ActsFromThePast/
  AFTP legacy acts using the fork DLL.
  The fork includes multiplayer fixes for ClassicSlimed, RebalancedMode,
  DARV option generation, and shared event pool filtering.
  The PCK is the complete workshop resource file. Do not replace it with
  a small localization-only PCK.

2. Requirements

  Slay the Spire 2 public beta v0.111.x.
  BaseLib v3.4.5 or newer.
  Both multiplayer players must install the same Spire1 and AFTP files.

3. Install

  Extract this archive into the game directory next to SlayTheSpire2.exe.
  The result must contain mods/Spire1 and mods/ActsFromThePast.
  Enable both mods in the game mod list.
  If the workshop AFTP mod is also subscribed, keep the subscription.
  The local copy has priority and the workshop copy is disabled.

4. Multiplayer status

  The three known desync fix families have code in the pack:
  ClassicSlimed, RebalancedMode, and DARV plus DustyTome.
  The fixes are complete in code but still require a real two-player run.
  The test checklist is:
  - play a ClassicSlimed card
  - enter the DUPLICATOR event
  - enter DARV and let one player choose DustyTome
  - finish one complete act
  Record the local time of any disconnect or black screen and send the host
  godot.log excerpt.

5. File checksums

  mods/Spire1/Spire1.dll
    8d510cee7022b94a1abdb65138d9a061
  mods/Spire1/Spire1.pck
    aae4930e99f24a2c983b4f323299507a
  mods/ActsFromThePast/ActsFromThePast.dll
    317ad0345f64fccef14d727ddbc46563
  mods/ActsFromThePast/ActsFromThePast.pck
    ba60133a597bf7b80bddcccdd4c493db

6. Light install

  Subscribe to the workshop AFTP mod to obtain its complete PCK.
  Copy only the pack DLL to mods/ActsFromThePast and overwrite the old DLL.
  The DLL checksum must be 317ad0345f64fccef14d727ddbc46563.

7. Character default

  mods/Spire1/character.txt contains all.
  This enables all three StS1 characters.
  For a single character, replace the content with ironclad, silent, or
  defect. The DLL and PCK do not need to change.
