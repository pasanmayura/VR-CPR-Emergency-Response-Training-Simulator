# CPR patient model

Source: `Body/male.fbx` and its five `Body/textures` maps, copied here for Unity import.

`MaleSupine.asset` is a static mesh baked from the supplied rig. The left and right
upper arms were rotated by +80 and -80 degrees around the model's world Z axis to
rest beside the body. The posed mesh was rotated 90 degrees around X, scaled by
0.12658, and translated by (0, 0.0048, -0.0165) into the existing mannequin's local
coordinates. The original FBX and rig remain available for future animation work.

`MaleBody.mat` uses Unity's Standard (Specular setup) shader. `BodyAlbedo.png`
combines Diffuse RGB with Opacity in alpha. `BodySpecularSmoothness.png` combines
Specular RGB with Gloss in alpha. The supplied Normal texture is imported as a
normal map. Alpha clipping preserves the model's eyelash details.

`CPRMannequin.prefab` retains its original object/component IDs and uses the baked
mesh for both rendering and collision. MainScene retains its existing interaction
references. Its patient height, contact zones and pad socket heights were fitted
to the new mesh; the upper-hand/chest separation is preserved. The previously
zero X scale on Pad1Socket is restored to one.
