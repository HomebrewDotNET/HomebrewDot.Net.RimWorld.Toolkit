using System;
using UnityEngine;
using Verse;

namespace HomebrewDot.Net.Rimworld.UI.Components
{
    /// <summary>
    /// Simple confirmation popup with cancel/confirm actions.
    /// </summary>
    public sealed class ConfirmWindow : Window
    {
        private readonly string _title;
        private readonly string _message;
        private readonly Action _onConfirm;
        private Vector2 _messageScroll = Vector2.zero;

        public ConfirmWindow(string title, string message, Action onConfirm)
        {
            _title = title ?? string.Empty;
            _message = message ?? string.Empty;
            _onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));

            closeOnClickedOutside = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            doCloseButton = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                const float width = 640f;
                var messageHeight = Text.CalcHeight(_message, width - 40f);
                var desiredHeight = 140f + messageHeight;
                return new Vector2(width, Mathf.Clamp(desiredHeight, 220f, 600f));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var cursorY = inRect.y;

            Widgets.Label(new Rect(inRect.x, cursorY, inRect.width, 24f), _title);
            cursorY += 30f;

            var buttonWidth = 120f;
            var cancelRect = new Rect(inRect.x, inRect.yMax - 36f, buttonWidth, 32f);
            var confirmRect = new Rect(cancelRect.xMax + 8f, cancelRect.y, buttonWidth, 32f);
            var messageOutRect = new Rect(inRect.x, cursorY, inRect.width, Mathf.Max(0f, cancelRect.y - cursorY - 8f));
            var messageTextHeight = Text.CalcHeight(_message, Mathf.Max(0f, messageOutRect.width - 16f));
            var messageViewRect = new Rect(0f, 0f, Mathf.Max(0f, messageOutRect.width - 16f), Mathf.Max(messageOutRect.height, messageTextHeight + 8f));

            Widgets.BeginScrollView(messageOutRect, ref _messageScroll, messageViewRect);
            Widgets.Label(new Rect(0f, 0f, messageViewRect.width, messageViewRect.height), _message);
            Widgets.EndScrollView();

            Widgets.DrawMenuSection(cancelRect);
            if (Widgets.ButtonInvisible(cancelRect))
            {
                Close();
            }
            Widgets.Label(cancelRect.ContractedBy(4f), "Cancel");

            Widgets.DrawMenuSection(confirmRect);
            if (Widgets.ButtonInvisible(confirmRect))
            {
                _onConfirm();
                Close();
            }
            Widgets.Label(confirmRect.ContractedBy(4f), "Confirm");
        }
    }
}
