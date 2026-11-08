# Artwork

## Responsible for
Drawing `folder.png` and `thumb.png` for a copy at 1000×562 (F3, D6): backdrop darkened on the left, big white rank number, logo over the picture; title in large text if there's no logo; poster on dark if there's no backdrop. If drawing fails, says so, so Run uses the source backdrop or poster as-is (spec F3).

## Belongs here
SkiaSharp drawing code and the embedded font Montserrat ExtraBold (SIL OFL, D15), kept inside this folder, behind the `IArtwork` interface Run uses. Takes in: rank, title text, backdrop, logo and poster bytes (each may be missing). Gives back PNG bytes, or "failed" with a reason. Layout numbers are in spec F3. Every SkiaSharp call sits behind one internal type, so if SkiaSharp can't load the error is caught and reported as "failed".

## Does not belong
Finding the backdrop or logo in Emby (EmbyBridge), saving into the copy's folder (Run).

## May use
Logic, SkiaSharp (compiled against, not shipped: Emby's own copy is used, D15).

## Must not use
Store, EmbyBridge, Run, Settings, any `MediaBrowser.*` type, file access, `System.Net`, the root namespace.
