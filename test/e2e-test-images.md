# E2E Test Image Catalog

This document catalogs test images for repost detection testing.

## High Similarity Pairs (>= 0.95)

### French Cat (different formats)
- Original PNG: https://media.discordapp.net/attachments/1319991503891861534/1449832200702001183/frenchcat.png?ex=694054f5&is=693f0375&hm=987e474fcc62613908de83ee2db12365472ec3bde4a27525185bbb80184e44e5&=&format=webp&quality=lossless&width=918&height=1155
- JPEG version: https://media.discordapp.net/attachments/1319991503891861534/1449832201049870376/frenchcat.jpg?ex=694054f5&is=693f0375&hm=b8b5a37d8b72b4261dd61781d1ba828285b3ed9e5e00f1e044c69a274e3b05af&=&format=webp&width=918&height=1155
- WebP version: https://media.discordapp.net/attachments/1319991503891861534/1449832201821753567/frenchcat.webp?ex=694054f5&is=693f0375&hm=2d3c0b2ee955a4f22f4ac1f5733e295cd1284f7fd0ab96a3458c1ed601f59e44&=&format=webp&width=918&height=1155
- WebP 50% quality: https://media.discordapp.net/attachments/1319991503891861534/1449832201385676992/frenchcat_50.webp?ex=694054f5&is=693f0375&hm=da1d9257e122b9b62b8990629a3ba1c5d02e8833443e090026365841f008021a&=&format=webp&width=900&height=1133
- Expected: All should match with similarity >= 0.98

## Medium Similarity Pairs (0.80 - 0.95)

### Similar Memes (same template, different text)
- LLMs meme: https://media.discordapp.net/attachments/1319991503891861534/1449824811017572372/RDT_20251204_1703516204720909397219942.jpg?ex=69404e13&is=693efc93&hm=304eabce7576e515e2bcd7d1cc77ea6744febbce860773ee624615ffb2f9ed5d&=&format=webp&width=1210&height=1155
- Obsidian dog: https://media.discordapp.net/attachments/1319991503891861534/1449824812309414089/RDT_20251204_0806282633527631759635038.jpg?ex=69404e13&is=693efc93&hm=aec59f299be7616c28d3f3f146cc4121ecb44473e8b9bc769077b217b19abf79&=&format=webp&width=800&height=1054
- Expected: Similarity between 0.80 and 0.95 (requires validation)

## Low Similarity (< 0.80)

### Unrelated Images
- French snails: https://media.discordapp.net/attachments/1319991503891861534/1449824813462851675/RDT_20251203_1901397855856549389737722.jpg?ex=69404e13&is=693efc93&hm=9295add85964d52e714ff95792e0b6123bdbaa6149982afa93e3ec0d287c69aa&=&format=webp&width=854&height=1155
- Family Guy Bill Gates: https://media.discordapp.net/attachments/1319991503891861534/1449825108611825915/RDT_20250125_1000203858268434480552263.jpg?ex=69404e5a&is=693efcda&hm=01d74376049da7f5b1bb16484c9b7dfa7990f7ef28b2155670f1941e69e827e3&=&format=webp&width=1329&height=1155
- Expected: Similarity < 0.80 (requires validation)

## Notes

- URLs are from existing `test/upload-image-message.http`
- Similarity ranges need empirical validation with actual ML service
- Add more pairs as needed based on real-world test results
- Discord CDN URLs may expire - update as needed
