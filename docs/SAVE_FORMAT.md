# Encrypted save envelope

This implementation is a compatibility port of d3xMachina's MIT-licensed `SuikodenSaveDecrypter` implementation at the commit recorded in [UPSTREAM_SOURCES.md](UPSTREAM_SOURCES.md). The upstream executable was compiled unchanged from a disposable copy and used as an oracle; the ignored source clone was not modified.

## Envelope

An encrypted save is UTF-8 text with this layout:

```text
GR_DATA:<Base64(salt || AES-CBC ciphertext)>
```

- The exact ASCII header is `GR_DATA:`. A UTF-8 BOM before it is accepted.
- Decoded bytes begin with a 16-byte salt.
- Remaining bytes must contain at least one complete 16-byte AES block.
- Plaintext is UTF-8 JSON. The upstream-compatible writer emits its UTF-8 BOM inside the encrypted plaintext.

## Key derivation and cipher

The implementation uses the exact upstream password retained privately in `SaveCrypto`, not a user password. It fills a 48-byte derivation buffer by repeatedly hashing `already-derived bytes || UTF8(password) || salt` with SHA-256. Bytes 0–31 become the AES key and bytes 32–47 become the IV.

Encryption is AES-256-CBC with a 128-bit block size and PKCS#7 padding. Every write obtains a fresh 16-byte salt from `RandomNumberGenerator`. Reusing a fixed salt is possible only through the internal test overload.

## Error handling

The parser distinguishes empty input, wrong header, invalid Base64, missing/short salt and ciphertext, incomplete AES blocks, invalid padding/key material, invalid UTF-8, invalid JSON, ambiguous schemas, and unsupported schemas. Decryption and parse errors never create output.

## Compatibility evidence

Automated tests establish:

- our code decrypts an upstream-produced synthetic fixture;
- each supplied encrypted `gsd1` and `gsd2` slot opens from a temporary copy;
- upstream can decrypt representative outputs created by our writer;
- no-edit and edited output decrypt back to semantically identical intended JSON;
- fresh encryptions use different salts;
- malformed headers, Base64, ciphertext lengths, padding, UTF-8, and JSON fail with categorized errors.

JSON lexical details such as whitespace are not preserved because encryption writes a new serialization. JSON object content, numeric/string values, array ordering and lengths, duplicate array entries, and unknown nodes are preserved semantically.

The supplied `_sharetmpsave0` uses the same decryptable envelope and contains valid JSON, but it is not a `Data0`–`Data16` game slot and lacks the verified signature of either game. Tests decrypt and parse a temporary copy; the application refuses to misclassify or edit it.
