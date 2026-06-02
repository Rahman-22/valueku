namespace ValueKu.Common;

/// <summary>Whether Google sign-in is configured; drives the UI button and external endpoints.</summary>
public sealed record GoogleAuthState(bool Enabled);
