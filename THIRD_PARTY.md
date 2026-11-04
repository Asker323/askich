# Third-party art and sound — Berezki local atmosphere stage

Retrieved 2026-11-03. No paid assets or services were used.

## Quaternius — Zombie Apocalypse Kit (March 2024)

Creator and license declaration: https://quaternius.com/packs/zombieapocalypsekit.html
License: CC0 1.0 Universal, https://creativecommons.org/publicdomain/zero/1.0/
Original creator download folder: https://drive.google.com/drive/folders/1mWP6sCHun7OUMHQeDNZLrXTteXlzWg_t

Included source files in `Assets/Resources/Survival/Apocalypse`: Matt, Basic/Chubby/Ribcage zombies, two vehicles, street lamp, water tower, couch, barrel, pallet, cone, trash bag and Zombie_Atlas.png. Original skeletal animation clips are included in the FBX files. Generated prefabs and a Unity Standard material are in `Assets/Resources/Survival/Art`.

Modifications: Unity Legacy animation import, normalized world scale, material remapping, hero's unused weapon meshes hidden. Gameplay, animation transitions, equipment attachment, lights and audio are project code. Rebuild prefabs using `Askich.Editor.DistrictArtImport.Prepare`.

The supplied License.txt is preserved verbatim. Its heading says “Ultimate Platformer Pack”, apparently a reused upstream template; the specific Zombie Apocalypse Kit creator page independently states CC0. No claim is made that these third-party models were created for this project.

## artisticdude — Zombies Sound Pack

Source: https://opengameart.org/content/zombies-sound-pack
Original download: https://opengameart.org/sites/default/files/zombies.zip
Creator: artisticdude; published 2011-01-02; originally made for Summoning Wars.
License stated on the asset page: CC0 1.0 Universal, https://creativecommons.org/publicdomain/zero/1.0/

24 WAV recordings in `Assets/Resources/Survival/ZombieAudio`. Originals retained; Unity imports mono Vorbis for playback. In-game pitch, spatial attenuation and low-pass filtering vary per emitter. These are infected vocal effects, not spoken Russian dialogue.

## Project-generated material

The Dusk shader, geometry for houses/vegetation, procedural wind, steps and impacts are project code. The radio script was written for this game; the existing recording uses Microsoft Irina Desktop via Windows System.Speech. This is a temporary synthetic voice, not an actor recording. Before public distribution, confirm that voice's redistribution terms or replace the recording with a clearly licensed voice performance. Publication remains deferred.
