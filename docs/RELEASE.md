# Release and verification

First public version: 0.9.0-beta.1, not 1.0.0. Earlier local 1.0.0 labels were development packages.

1. Configure the real public repository URL, protected branch/tags and release environment reviewers; enable private vulnerability reporting.
2. Review commit, license inventory, automated tests and manual owner checklist.
3. Push matching tag v0.9.0-beta.1. CI verifies version, restores lockfiles, builds/tests and packages unsigned candidates.
4. Download public-beta-candidate from that run. Publish those files and notes without rebuilding. This workflow uploads candidates; public publication remains a maintainer action.
5. CI attests ZIP, installer and SBOM when supported. Failure fails the job; never claim unexecuted provenance.
6. Compare PowerShell Get-FileHash -Algorithm SHA256 results with SHA256SUMS.txt. This checks integrity relative to the checksum file.
7. Use gh attestation verify FILE --repo OWNER/REPOSITORY against the actual repository and inspect signer/workflow/ref. About, tag, metadata and commit must agree.

Local builds explicitly label dirty/uncommitted source. No fake URL, tag or attestation. SourceLink is not configured without a real repository URL. Source revision metadata is embedded.

Managed compilation is deterministic with pinned SDK/packages/actions/native dependencies. ZIP timestamps, SBOM serial/time, installer metadata and future signing timestamps may differ.

Future Authenticode: use a legitimate certificate in protected trusted-tag jobs. Sign the published executable before packaging, sign the installer, regenerate checksums, then attest final bytes. Never expose secrets to PR jobs or distribute test certificates.
