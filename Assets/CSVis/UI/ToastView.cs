using System.Collections.Generic;
using CSVis.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSVis.UI
{
    /// <summary>
    /// Shows <see cref="Notifier"/> messages as a small card low in the user's view. One at a time, queued,
    /// fading in and out, colour coded by severity.
    /// </summary>
    public sealed class ToastView : MonoBehaviour
    {
        private const float Width = 420f;
        private const float MinHeight = 60f;
        private const float TextWidth = Width - 100f;
        private const float FadeSeconds = 0.25f;
        private const float DistanceMeters = 0.8f;
        private const float DropMeters = 0.33f;

        private readonly Queue<Notification> queue = new Queue<Notification>();

        private CSVisUIAssets assets;
        private Canvas canvas;
        private RectTransform canvasRect;
        private CanvasGroup group;
        private Image accentBar;
        private Image icon;
        private TextMeshProUGUI label;

        private bool showing;
        private float shownAt;
        private float duration;
        private bool snapToTarget = true;

        public static ToastView Create(CSVisUIAssets assets)
        {
            var go = new GameObject("CSVis Toasts");
            var view = go.AddComponent<ToastView>();
            view.Build(assets);
            return view;
        }

        public bool IsShowing => showing;

        public string CurrentMessage => showing && label != null ? label.text : null;

        private void Build(CSVisUIAssets uiAssets)
        {
            assets = uiAssets;
            canvas = UIBuilder.CreateCanvas("Canvas", transform, new Vector2(Width, MinHeight));
            canvasRect = (RectTransform)canvas.transform;
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            UIBuilder.Backplate(assets, canvas.transform, UITheme.ToastFill);

            accentBar = UIBuilder.Bar(canvas.transform, "Accent", UITheme.Accent);
            icon = UIBuilder.Icon(canvas.transform, "Icon", IconFactory.Get(UIIcon.Info), UITheme.Accent);
            label = UIBuilder.Text(assets, canvas.transform, "Message", string.Empty, UITheme.ToastSize, UITheme.TextPrimary);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;

            canvas.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Notifier.Posted += OnPosted;
        }

        private void OnDisable()
        {
            Notifier.Posted -= OnPosted;
        }

        private void OnPosted(Notification notification)
        {
            // Collapse repeats so a failing loop can't fill the queue
            if (showing && label.text == notification.Message)
            {
                shownAt = Time.unscaledTime;
                return;
            }

            foreach (var queued in queue)
            {
                if (queued.Message == notification.Message)
                {
                    return;
                }
            }

            if (queue.Count >= 4)
            {
                queue.Dequeue();
            }

            queue.Enqueue(notification);
        }

        private void LateUpdate()
        {
            if (!showing && queue.Count > 0)
            {
                Show(queue.Dequeue());
            }

            if (!showing)
            {
                return;
            }

            var age = Time.unscaledTime - shownAt;

            // A backlog shortens how long each toast stays
            var visibleFor = queue.Count > 0 ? Mathf.Min(duration, 2f) : duration;
            if (age >= visibleFor + FadeSeconds)
            {
                showing = false;
                group.alpha = 0f;
                canvas.gameObject.SetActive(false);
                snapToTarget = true;
                return;
            }

            group.alpha = Mathf.Clamp01(Mathf.Min(age / FadeSeconds, (visibleFor + FadeSeconds - age) / FadeSeconds));
            Follow();
        }

        private void Show(Notification notification)
        {
            var color = UITheme.ForLevel(notification.Level);
            label.text = notification.Message;

            var preferred = label.GetPreferredValues(notification.Message, TextWidth, 0f).y;
            var height = Mathf.Max(MinHeight, preferred + 26f);
            canvasRect.sizeDelta = new Vector2(Width, height);

            UIBuilder.Place(accentBar.rectTransform, 0f, 0f, 6f, height);
            accentBar.color = color;

            UIIcon glyph;
            switch (notification.Level)
            {
                case NotificationLevel.Success: glyph = UIIcon.Check; break;
                case NotificationLevel.Warning: glyph = UIIcon.Warning; break;
                case NotificationLevel.Error: glyph = UIIcon.Error; break;
                default: glyph = UIIcon.Info; break;
            }

            icon.sprite = IconFactory.Get(glyph);
            icon.color = color;
            UIBuilder.Place(icon.rectTransform, 22f, (height - 34f) / 2f, 34f, 34f);
            UIBuilder.Place(label.rectTransform, 72f, 0f, TextWidth, height);

            duration = notification.Duration;
            shownAt = Time.unscaledTime;
            showing = true;
            canvas.gameObject.SetActive(true);
            Follow();
        }

        /// <summary>Stays low in the user's view, smoothly following their head.</summary>
        private void Follow()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var flat = camera.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
            {
                flat = Vector3.forward;
            }

            flat.Normalize();
            var target = camera.transform.position + flat * DistanceMeters + Vector3.down * DropMeters;
            var rotation = Quaternion.LookRotation(flat, Vector3.up);

            if (snapToTarget)
            {
                transform.SetPositionAndRotation(target, rotation);
                snapToTarget = false;
                return;
            }

            var t = 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target, t), Quaternion.Slerp(transform.rotation, rotation, t));
        }
    }
}
