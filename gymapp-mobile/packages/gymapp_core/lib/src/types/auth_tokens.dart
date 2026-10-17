/// The two halves of an authenticated session, as distinct types so they
/// cannot be swapped when stored (both are opaque `String`s on the wire).
library;

/// Short-lived JWT sent as `Authorization: Bearer …` on every request.
extension type const AccessToken(String value) implements String {}

/// Long-lived token exchanged at `/identity/refresh-token` for a new pair.
extension type const RefreshToken(String value) implements String {}
