# Security policy

Do not post exploitable vulnerabilities, sensitive logs or machine identifiers in public issues.

On GitHub use Security → Advisories → Report a vulnerability if private reporting is enabled. The maintainer must enable it before publication. No repository/private contact is configured in this checkout; no address is invented. If unavailable, request a private channel without posting details.

Supported candidate: 0.9.0-beta.1. Include version/commit, reproduction, impact and redacted evidence. No response-time guarantee.

PR CI has read-only permissions, no signing secrets and no pull_request_target execution. Actions are commit-pinned. Candidates build from matching version tags in the release environment. Before publication configure environment reviewers, restrict tags, protect the default branch and enable private reports. OIDC/attestation write scope exists only in the release job. Future signing must use protected trusted-tag jobs and a legitimate certificate.

[Review](docs/SECURITY_REVIEW.md) · [Trust boundaries](docs/SECURITY_AND_TRUST.md). Open source is not a security guarantee.
