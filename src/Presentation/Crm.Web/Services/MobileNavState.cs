namespace Crm.Web.Services
{
    public sealed class MobileNavState
    {
        public bool IsOpen { get; private set; }

        public event Action? OnChange;

        public void Toggle()
        {
            IsOpen = !IsOpen;
            OnChange?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            OnChange?.Invoke();
        }
    }
}
