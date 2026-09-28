using HOSASBridge.Profiles;
using HOSASBridge.Infrastructure;

namespace HOSASBridge.App.Services;

public sealed class ProfileSession(ProfileRepository repository, Profile profile, BridgeRuntime runtime)
{
    public Profile Current { get; private set; } = profile;
    public event Action? Changed;
    public void Save(Profile next)
    {
        ProfileValidator.EnsureValid(next); repository.Save(AppPaths.Profile, next);
        Current = next; runtime.Activate(next); Changed?.Invoke();
    }
}
