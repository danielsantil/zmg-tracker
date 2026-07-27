# Seeded checklist text — review before the migration (v2.9)

**Edit the `ES:` lines directly in this file.** I'll read it back and transcribe it into `SeedData.cs`
verbatim. Don't renumber or rename the `code` slugs — they're permanent identifiers that rules and
already-stamped release tasks key off.

Rules the current copy follows, so you can apply them consistently while editing:

- **Domain jargon stays English** — DSP, BMI, MLC, SoundExchange, Musixmatch, Canvas, Artist Pick,
  Discovery Mode are proper nouns; *smart link*, *pre-save*, *waterfall*, *multitracks*, *master*,
  *splits*, *focus tracks*, *tracklist*, *pitch*, *streams*, *EPK* are the terms ZMG already uses in
  English. Translating them makes the checklist harder to read, not easier.
- **`ES: —` means no Spanish row at all**, deliberately. The task falls back to its English text. Use it
  when a translation would be identical to the English anyway. `SeedDataTests` pins this exact set, so a
  *forgotten* translation fails a test instead of quietly passing as English inside a Spanish checklist.
- Blank Spanish and `ES: —` are the same thing at runtime. Write `—` so the intent is explicit.

Two tasks carry a timeframe (`7–14 days before release`); they're marked. Everything else has none.

---

## Base checklist — 31 tasks

Seeded into **both** the Single and the Album template. As of v2.9 the two copies are independent rows:
correcting one tab no longer changes the other, so a fix here needs applying on both tabs in the app.
Seeding, though, writes this list to both — so an edit in *this* file lands everywhere.

### Pre

**1. `mix-master`**
- EN: Mix/master
- ES: Mezcla/master

**2. `design-cover`**
- EN: Design cover for DSPs
- ES: Diseñar la portada para los DSPs

**3. `distribute-to-dsps`** · timeframe 7–14 days · ⚠️ **load-bearing: `Release.IsDistributed` keys off this code**
- EN: Distribute to DSPs
- ES: Distribuir a los DSPs

**4. `youtube-video-assets`**
- EN: Make video for YouTube, thumbnail and additional YouTube resources
- ES: Hacer el video para YouTube, la miniatura y los demás recursos de YouTube

**5. `pitch-amazon`**
- EN: Pitch to Amazon
- ES: Pitch a Amazon

**6. `pitch-spotify`** · timeframe 7–14 days
- EN: Pitch to Spotify
- ES: Pitch a Spotify

### Release

**7. `smart-link`**
- EN: Setup smart link to all stores
- ES: Configurar el smart link a todas las tiendas

**8. `smart-link-redirect`**
- EN: Setup smart link redirect from zionmusicgroup.com/<song-name>
- ES: Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>

**9. `register-bmi`**
- EN: Register composition to BMI
- ES: Registrar la composición en BMI

**10. `register-mlc`**
- EN: Register composition to MLC
- ES: Registrar la composición en MLC

**11. `register-soundexchange`**
- EN: Register to SoundExchange
- ES: Registrar en SoundExchange

**12. `musixmatch-lyrics`**
- EN: Musixmatch lyrics, add/sync
- ES: Letra en Musixmatch: agregar/sincronizar

**13. `check-deezer`**
- EN: Check release in Deezer (wrong artist)
- ES: Revisar el lanzamiento en Deezer (artista equivocado)

**14. `check-amazon`**
- EN: Check release in Amazon (wrong artist)
- ES: Revisar el lanzamiento en Amazon (artista equivocado)

**15. `check-apple`**
- EN: Check release in Apple (wrong artist)
- ES: Revisar el lanzamiento en Apple (artista equivocado)

**16. `spotify-canvas`**
- EN: Spotify Canvas
- ES: Spotify: agregar canvas

**17. `spotify-artist-pick`**
- EN: Spotify Artist Pick
- ES: Spotify: selección de artista

**18. `youtube-banner`**
- EN: Update YouTube banner
- ES: Actualizar el banner de YouTube

**19. `youtube-home-video`**
- EN: Update YouTube home video
- ES: Actualizar el video de inicio en canal de YouTube

**20. `youtube-cards`**
- EN: Update cards in existing videos
- ES: Actualizar las tarjetas en los videos existentes

**21. `youtube-pinned-comment`**
- EN: Update pinned comment in existing videos with link to new video
- ES: Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo

**22. `instagram-bio-youtube-link`**
- EN: Update YouTube link on Instagram bios
- ES: Actualizar el enlace de YouTube en las bios de Instagram

**23. `instagram-bio-song`**
- EN: Update song on Instagram bios
- ES: Actualizar la canción en las bios de Instagram

**24. `master-splits`**
- EN: Send master splits to collaborators
- ES: Enviar los splits de master a los colaboradores

### Post

**25. `meta-ads-initial`**
- EN: Meta ads, initial release campaign
- ES: Meta ads: campaña inicial de lanzamiento

**26. `meta-ads-ongoing`**
- EN: Meta ads, ongoing campaign
- ES: Meta ads: campaña continua

**27. `spotify-discovery-mode`**
- EN: Spotify Discovery Mode
- ES: Spotify: campaña Discovery Mode

**28. `youtube-video-ads`**
- EN: YouTube video ads
- ES: Anuncios de video en YouTube

**29. `tiktok-ads`**
- EN: TikTok ads
- ES: Anuncios en TikTok

**30. `youtube-lyrics-video`**
- EN: Create YouTube lyrics video
- ES: Crear el video de letras para YouTube

**31. `multitracks-setup`**
- EN: Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos
- ES: Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos

---

## Album extras — 10 tasks

Appended to the base checklist in the **Album** template only, giving it **41** tasks.

### Pre

**32. `album-tracklist-sequencing`**
- EN: Finalize tracklist and sequencing (locked once submitted to distributor)
- ES: Acordar el tracklist y orden de las canciones (queda fijo al enviarlo a la distribuidora)

**33. `album-isrc-upc-metadata`**
- EN: Confirm ISRC/UPC and per-track metadata/credits
- ES: Confirmar ISRC/UPC y la metadata/créditos de cada canción

**34. `album-focus-tracks-waterfall`**
- EN: Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)
- ES: Elegir los focus tracks y planear 2-4 sencillos previos al álbum (waterfall: cada sencillo nuevo se reempaqueta con los anteriores y el álbum hereda sus streams)

**35. `album-pre-save`**
- EN: Album pre-save campaign
- ES: Campaña de pre-save del álbum

**36. `album-bio-press-epk`**
- EN: Update artist bio / press release / EPK
- ES: Actualizar la biografía del artista / comunicado de prensa / press kits

**37. `album-batch-content`**
- EN: Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)
- ES: Producir contenido por lotes antes de la semana de lanzamiento (comentario canción por canción, videos de letras, versiones acústicas)

**38. `album-physical-media`**
- EN: Physical media if applicable (vinyl/CD lead times are months)
- ES: Medios físicos si aplica (los tiempos de producción de vinilo/CD son de meses)

### Release

**39. `album-per-track-registrations`**
- EN: Registrations (BMI, MLC, Musixmatch, splits) repeat per track
- ES: Los registros (BMI, MLC, Musixmatch, splits) se repiten por cada canción

### Post

**40. `album-rotate-focus-tracks`**
- EN: Rotate focus tracks every few weeks with per-track playlist pitching
- ES: Rotar los focus tracks cada pocas semanas con pitching de playlists por canción

**41. `album-remaining-lyric-videos`**
- EN: Lyric videos for remaining tracks
- ES: Videos de letras para las canciones restantes

---

## Totals

| Template | Tasks | With Spanish | English-only |
|---|---|---|---|
| Single | 31 | 31 | 0 |
| Album | 41 | 41 | 0 |

**Reviewed and shipped (2026-07-27).** ZMG's pass translated the three Spotify proper nouns that were
originally left English-only, so every seeded task now carries both languages — which is why
`SeedDataTests` asserts exactly that, a stronger pin than the "these three are deliberately
untranslated" set it replaced. A null `TitleEs` is still a supported state for tasks added in the app;
no seeded task uses it.

This file stays the copy of record. To change seeded text, edit here and transcribe into
`SeedData.cs`; to correct a *live* template, use the templates screen instead — no migration needed.
