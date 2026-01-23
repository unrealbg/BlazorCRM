namespace Crm.Web.Services
{
    public sealed class ThemeState
    {
        public bool IsDark { get; private set; }

        public event Action? OnChange;

        public void Set(bool isDark)
        {
            if (IsDark == isDark)
            {
                return;
            }

            IsDark = isDark;
            OnChange?.Invoke();
        }
    }
}
