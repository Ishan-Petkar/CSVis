using MixedReality.Toolkit;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CSVis.UI
{
    /// <summary>A button built by <see cref="UIBuilder"/>: an MRTK3 PressableButton with our content on top.</summary>
    public sealed class UIButton : MonoBehaviour
    {
        public string Key;
        public StatefulInteractable Interactable;
        public CanvasGroup Content;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Subtitle;
        public Image Icon;

        public bool IsEnabled { get; private set; } = true;

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (Interactable != null)
            {
                Interactable.enabled = enabled;
            }

            if (Content != null)
            {
                Content.alpha = enabled ? 1f : 0.4f;
            }
        }

        public void SetTitle(string text)
        {
            if (Title != null) Title.text = text;
        }

        public void SetSubtitle(string text)
        {
            if (Subtitle != null) Subtitle.text = text;
        }

        public void SetIconColor(Color color)
        {
            if (Icon != null) Icon.color = color;
        }

        public void AddListener(UnityAction action)
        {
            if (Interactable != null && action != null)
            {
                Interactable.OnClicked.AddListener(action);
            }
        }
    }

    /// <summary>Small helpers for building world-space (millimetre) canvases out of uGUI and MRTK3 parts.</summary>
    public static class UIBuilder
    {
        /// <summary>One canvas unit is one millimetre.</summary>
        public const float CanvasScale = 0.001f;

        public static Canvas CreateCanvas(string name, Transform parent, Vector2 sizeMm)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = sizeMm;
            rect.localScale = Vector3.one * CanvasScale;
            rect.localPosition = Vector3.zero;
            return canvas;
        }

        /// <summary>Anchors a rect to the parent's top-left and positions it in millimetres, y measured downwards.</summary>
        public static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform Empty(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>A rounded, glassy MRTK3 panel background tinted with <paramref name="fill"/>.</summary>
        public static Graphic Backplate(CSVisUIAssets assets, Transform parent, Color fill, string name = "Backplate")
        {
            var instance = Object.Instantiate(assets.panelPrefab, parent, false);
            instance.name = name;
            Stretch((RectTransform)instance.transform);
            var graphic = instance.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = fill;
                graphic.raycastTarget = false;
            }

            return graphic;
        }

        public static TextMeshProUGUI Text(
            CSVisUIAssets assets, Transform parent, string name, string text, float size, Color color,
            bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = bold ? assets.fontSemibold : assets.fontRegular;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Image Bar(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// A wide "card" button: icon on the left, title and description beside it, chevron on the right.
        /// </summary>
        public static UIButton Card(
            CSVisUIAssets assets, Transform parent, string key, float x, float y, float width, float height,
            string title, string description, Sprite icon, Color iconColor, bool showChevron = true)
        {
            var button = CreateButton(assets, parent, key, x, y, width, height, out var content);

            const float pad = 14f;
            var iconSize = height - 2f * 14f;
            var iconImage = Icon(content, "Icon", icon, iconColor);
            Place(iconImage.rectTransform, pad, (height - iconSize) / 2f, iconSize, iconSize);

            var textX = pad + iconSize + 14f;
            var chevronWidth = showChevron ? 34f : 8f;
            var textWidth = width - textX - chevronWidth;

            button.Title = Text(assets, content, "Title", title, UITheme.CardTitleSize, UITheme.TextPrimary, true);
            Place(button.Title.rectTransform, textX, height / 2f - 25f, textWidth, 24f);

            button.Subtitle = Text(assets, content, "Description", description, UITheme.CardBodySize, UITheme.TextSecondary);
            button.Subtitle.textWrappingMode = TextWrappingModes.Normal;
            button.Subtitle.verticalAlignment = VerticalAlignmentOptions.Top;
            Place(button.Subtitle.rectTransform, textX, height / 2f - 1f, textWidth, 26f);

            if (showChevron)
            {
                var chevron = Icon(content, "Chevron", IconFactory.Get(UIIcon.Chevron), UITheme.TextSecondary);
                Place(chevron.rectTransform, width - 34f, (height - 22f) / 2f, 22f, 22f);
            }

            button.Icon = iconImage;
            return button;
        }

        /// <summary>A square icon-only button (pin, minimise, delete...).</summary>
        public static UIButton IconButton(
            CSVisUIAssets assets, Transform parent, string key, float x, float y, float size, UIIcon icon, Color color)
        {
            var button = CreateButton(assets, parent, key, x, y, size, size, out var content);
            var glyph = Icon(content, "Glyph", IconFactory.Get(icon), color);
            var inset = size * 0.24f;
            Place(glyph.rectTransform, inset, inset, size - 2f * inset, size - 2f * inset);
            button.Icon = glyph;
            return button;
        }

        /// <summary>A square button showing a sprite (used for the minimised menu's logo).</summary>
        public static UIButton SpriteButton(
            CSVisUIAssets assets, Transform parent, string key, float x, float y, float size, Sprite sprite, Color color)
        {
            var button = CreateButton(assets, parent, key, x, y, size, size, out var content);
            var glyph = Icon(content, "Glyph", sprite, color);
            var inset = size * 0.16f;
            Place(glyph.rectTransform, inset, inset, size - 2f * inset, size - 2f * inset);
            button.Icon = glyph;
            return button;
        }

        /// <summary>A compact text button with an icon (Back, Home...).</summary>
        public static UIButton LabelButton(
            CSVisUIAssets assets, Transform parent, string key, float x, float y, float width, float height,
            string label, UIIcon icon, Color color)
        {
            var button = CreateButton(assets, parent, key, x, y, width, height, out var content);

            var glyphSize = height * 0.5f;
            var glyph = Icon(content, "Glyph", IconFactory.Get(icon), color);
            Place(glyph.rectTransform, 12f, (height - glyphSize) / 2f, glyphSize, glyphSize);

            button.Title = Text(assets, content, "Label", label, UITheme.FooterSize, UITheme.TextPrimary, true,
                TextAlignmentOptions.MidlineLeft);
            Place(button.Title.rectTransform, 12f + glyphSize + 8f, 0f, width - (12f + glyphSize + 8f) - 8f, height);

            button.Icon = glyph;
            return button;
        }

        private static UIButton CreateButton(
            CSVisUIAssets assets, Transform parent, string key, float x, float y, float width, float height,
            out RectTransform content)
        {
            var instance = Object.Instantiate(assets.buttonPrefab, parent, false);
            instance.name = key;
            Place((RectTransform)instance.transform, x, y, width, height);

            // Voice commands aren't used; hide MRTK's "see it, say it" label
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t != instance.transform && t.name.StartsWith("SeeItSayIt"))
                {
                    t.gameObject.SetActive(false);
                }
            }

            // Content goes inside AnimatedContent so it moves with MRTK's press animation
            Transform animated = null;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "AnimatedContent")
                {
                    animated = t;
                    break;
                }
            }

            content = Empty("Content", animated != null ? animated : instance.transform);
            Stretch(content);

            var button = instance.AddComponent<UIButton>();
            button.Key = key;
            button.Interactable = instance.GetComponent<StatefulInteractable>();
            button.Content = content.gameObject.AddComponent<CanvasGroup>();
            return button;
        }
    }
}
