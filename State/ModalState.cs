
namespace PipeUi.State
{
    public class ModalState
    {
        public string TypeName => GetType().FullName ?? "(null)";

        // 111
        public Guid InstanceId { get; } = Guid.NewGuid();

        public bool IsOpen { get; private set; }

        public string Title { get; private set; } = "";
        public string Body { get; private set; } = "";

        public string ConfirmText { get; private set; } = "OK";
        public string CancelText { get; private set; } = "Cancel";
        public bool ShowCancel { get; private set; } = true;

        private Func<Task>? _onConfirm;
        private Func<Task>? _onCancel;

        public event Action? OnChange;

        public void Show(

            string title,
            string body,
            string? confirmText = null,
            string? cancelText = null,
            bool showCancel = true,
            Func<Task>? onConfirm = null,
            Func<Task>? onCancel = null)
        {
            Console.WriteLine("Modal.Show() called");
            Title = title;
            Body = body;

            // if (!string.IsNullOrWhiteSpace(confirmText)) ConfirmText = confirmText!;
            // if (!string.IsNullOrWhiteSpace(cancelText)) CancelText = cancelText!;
            // 简化写法：if 加花括号
            if (!string.IsNullOrWhiteSpace(confirmText))
            {
                ConfirmText = confirmText!;
            }
            if (!string.IsNullOrWhiteSpace(cancelText))
            {
                CancelText = cancelText!;
            }
            ShowCancel = showCancel;

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            IsOpen = true;
            NotifyStateChanged();
        }

        public void Close()
        {
            IsOpen = false;
            _onConfirm = null;
            _onCancel = null;
            NotifyStateChanged();
        }

        public async Task ConfirmAsync()
        {
            var cb = _onConfirm;
            Close();
            // if (cb != null) await cb();
            // 简化写法：加花括号
            if (cb != null)
            {
                await cb();
            }
        }

        public async Task CancelAsync()
        {
            var cb = _onCancel;
            Close();
            // if (cb != null) await cb();
            // 简化写法：加花括号
            if (cb != null)
            {
                await cb();
            }
        }

        // private void NotifyStateChanged() => OnChange?.Invoke();
        // 简化写法：用花括号体
        private void NotifyStateChanged()
        {
            if (OnChange != null)
            {
                OnChange.Invoke();
            }
        }
    }
}
