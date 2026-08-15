using CommunityToolkit.Mvvm.ComponentModel;

namespace Zarpa.Client.Utilities
{
    // Trimmed from the source app: Zarpa's session only tracks the signed-in user
    // (no church/billing state).
    public partial class UserSessionService : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSignedIn))]
        private long _userId;

        public bool IsSignedIn => UserId > 0;

        public void Clear()
        {
            UserId = 0;
        }
    }
}
