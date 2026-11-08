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

## Blender Studio / community — Human Base Meshes v1.0.0

Official listing: https://www.blender.org/download/demo-files/
Pinned download: https://download.blender.org/demo/asset-bundles/human-base-meshes/human-base-meshes-bundle-v1.0.0.zip
Archive SHA256: `46a912c0524072ac3b78c35d5d2471df7b8df102394a050ca8cd7184e3393648`.

Source collection **Body Male - Realistic**, object `GEO-body_male_realistic`. Embedded asset metadata: author **Julien Kaspar**, license **CC0**. The official bundle listing independently describes CC0 base meshes. License: https://creativecommons.org/publicdomain/zero/1.0/ . The Blender application license is separate from this asset license.

Derived rigged body, eyes, morphs and garments are in `Assets/CharacterSources/Adult.fbx`; provenance in `source.json`. Project authoring script: `tools/author_adult.py` (Blender 4.5.9). Modifications: 25-bone rig and skin weights, shorts/shirt/trousers/shoes derived from mesh regions, hair and iris geometry, heavy/gaunt shape keys. The body has 15,093 vertices before derived parts. Unity editor retargeting reuses the previously included CC0 Quaternius animation clips and axe/spear geometry; generated clips, materials and prefabs are in `Resources/Survival/Human`. These are derivatives of third-party assets, not original human anatomy created for this project.

## Project-generated material

PC combat stage (2026-11-06): shove cloth-contact audio is generated locally by the existing SynthAudio code. Weapon reach indicator and crowd avoidance are project code. Punch and reversed Walk reuse the licensed animation derivatives described above. No new external assets were acquired.

The Dusk shader, geometry for houses/vegetation, procedural wind, steps and impacts are project code. The radio script was written for this game. The temporary Microsoft Irina Desktop recordings were removed from Resources in the November 5 visual-polish stage. The current receiver uses project-generated static and timed captions, with no spoken dialogue. Publication remains deferred.

Combat stage (2026-11-04): the already included Matt Axe and Spear skinned meshes are now enabled for equipped weapons, retaining the original bone bindings. Other unused weapon meshes remain hidden. No additional third-party files were downloaded.

Historical radio story stage: message0.wav, message1.wav and message2.wav were generated locally with Microsoft Irina Desktop using System.Speech. These files and the older radio.wav were removed from the current project in the November 5 visual-polish stage. Older packaged releases may still contain them. Porch scraping is project-generated procedural audio.

Street stage: StreetSurface and VergeGrass shaders, ground patches, grass meshes and fence geometry are project-generated. Existing licensed vehicles and street props are reused. No new external assets were acquired.

Interaction stage: hinged doors, handles and frames are project geometry. The Search clip is baked on the existing Matt skeleton using its Idle pose and project-generated arm motion. Container rummage remains project-generated procedural audio. No new external assets were acquired.

Inventory stage: item icons are rendered at runtime from project-authored item geometry. The school backpack and status icons are project-generated. The body portrait reuses the existing licensed Matt model. No new third-party assets were acquired.

Needs stage: water bottle, tin geometry and additional needs icons are project-generated. No new external assets were acquired.

Provisions stage: DrinkProvision, EatProvision and RestProvision are baked from the existing licensed Matt Idle/Duck clips with project-generated arm posing. Bottle, tin and refill-reserve geometry are project-generated. No new third-party assets were acquired.

Visual-polish stage: WorldSurface and Canopy shaders, branching tree meshes and leaf geometry are project-generated. The existing licensed infected models retain their skeletons and attacks; material treatment, head proportions and tongue visibility were adjusted. No external assets or new speech recordings were acquired.

Scavenging stage: storage boxes, meal, overturned chair, discarded clothing, dressing tray and footprints are project-authored geometry. Item icon details and wooden-drawer audio are project-generated. No new external assets or licenses were acquired.

## Character art iteration (2026-11-08)

No new external assets. The existing Julien Kaspar CC0 body remains the anatomical source. `Assets/CharacterSources/AdultBase.fbx` preserves the pre-stage derivative. `tools/CharacterArt/author_surface.py` and the packed `AdultStudio.blend` contain the local face adjustments, garment-edge smoothing/folds, hand grip shape, UVs and baked skin/cloth colour with ambient occlusion. These are procedural authored materials, not scanned human textures.

`CharacterMotionAuthor.cs` authors Hero Idle/Walk/Run/Slash/Stab/Punch/HitReact and Lean Idle/Walk/Punch/HitReact directly on the final skeleton. Other clips still derive from the existing licensed animations described above. `CharacterToolAuthor.cs` replaces the runtime axe and spear meshes with project-authored geometry and wood/steel materials. The anatomy was not created from scratch. No motion capture or external speech service was used.

## World life and progression (2026-11-08)

No new external assets or paid services. Gas station, pumps, engine, house roof variants and furnishings, books, food packets, material/tool icons and the character journal are project-authored geometry/code. Existing licensed characters, world props and infected recordings are reused. Page-turn audio reuses the project-generated cloth noise at low volume; educational radio uses project static and written captions, without speech recordings. Daylight and enemy perception are project code. No claim of new scanned materials, original human anatomy or new infected models is made.
