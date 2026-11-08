# Artwork

## Responsible for
Drawing `folder.png` and `thumb.png` for a copy at 1000×562 (F3, D6): backdrop darkened on the left, big white rank number, logo over the picture; title in large text if there's no logo; poster on dark if there's no backdrop. If drawing fails, says so so the copy gets the plain backdrop (D15).

## Belongs here
SkiaSharp drawing code and the embedded SIL OFL bold font (D15). Takes image bytes and a rank in, gives PNG bytes out.

## Does not belong
Finding the backdrop or logo in Emby (EmbyBridge), saving into the copy's folder (Run).

## May use
Logic, SkiaSharp (compiled against, not shipped: Emby's own copy is used, D15).

## Must not use
Store, EmbyBridge, Run, Settings, any `MediaBrowser.*` type.
