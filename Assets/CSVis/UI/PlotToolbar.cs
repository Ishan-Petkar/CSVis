using CSVis.Core;
using TMPro;
using UnityEngine;

namespace CSVis.UI
{
    /// <summary>A small title + Recenter + Delete bar that hangs below each plot and faces the user.</summary>
    public sealed class PlotToolbar : MonoBehaviour
    {
        private const float Width = 330f;
        private const float Height = 66f;
        private const float GapMeters = 0.07f;

        private PlotInstance plot;
        private Canvas canvas;

        public static PlotToolbar TryCreate(PlotInstance plot)
        {
            var assets = CSVisUIAssets.Load();
            if (assets == null || !assets.IsComplete(out _))
            {
                return null;
            }

            var go = new GameObject("CSVis Plot Toolbar");
            go.transform.SetParent(plot.transform, false);
            var toolbar = go.AddComponent<PlotToolbar>();
            toolbar.Build(assets, plot);
            return toolbar;
        }

        private void Build(CSVisUIAssets assets, PlotInstance owner)
        {
            plot = owner;
            canvas = UIBuilder.CreateCanvas("Canvas", transform, new Vector2(Width, Height));
            UIBuilder.Backplate(assets, canvas.transform, UITheme.PanelFill);

            var title = UIBuilder.Text(assets, canvas.transform, "Title", plot.Title, 16f, UITheme.TextPrimary, true,
                TextAlignmentOptions.MidlineLeft);
            UIBuilder.Place(title.rectTransform, 18f, 0f, Width - 18f - 2f * 48f - 22f, Height);

            var recenter = UIBuilder.IconButton(assets, canvas.transform, "PlotRecenter", Width - 2f * 48f - 12f, 11f, 44f, UIIcon.Target, UITheme.TextPrimary);
            recenter.AddListener(plot.Recenter);

            var delete = UIBuilder.IconButton(assets, canvas.transform, "PlotDelete", Width - 48f - 6f, 11f, 44f, UIIcon.Trash, UITheme.Danger);
            delete.AddListener(plot.Delete);

            canvas.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (plot == null || plot.Holder == null)
            {
                return;
            }

            var collider = plot.Holder.GetComponent<Collider>();
            if (collider == null)
            {
                canvas.gameObject.SetActive(false);
                return;
            }

            var bounds = collider.bounds;
            var camera = Camera.main;
            var facing = camera != null ? camera.transform.position - bounds.center : Vector3.back;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = Vector3.back;
            }

            canvas.gameObject.SetActive(true);

            // A world-space canvas reads correctly when its forward axis points away from the viewer
            transform.SetPositionAndRotation(
                bounds.center + Vector3.down * (bounds.extents.y + GapMeters),
                Quaternion.LookRotation(-facing.normalized, Vector3.up));
        }
    }
}
