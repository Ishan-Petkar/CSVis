using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSVis.Core
{
    /// <summary>
    /// Tracks every plot in the scene: enforces a sane limit for HoloLens 2, places new plots in a fan around
    /// the user so they never spawn on top of each other, and tears plots down completely.
    /// </summary>
    public static class PlotManager
    {
        /// <summary>HoloLens 2 renders thousands of point meshes fine, but not unlimited plots.</summary>
        public const int MaxPlots = 8;

        public const float SpawnDistance = 1.25f;

        // Yaw offsets (degrees) around the user for successive plots: centre first, then alternate left/right
        private static readonly float[] SlotYaw = { 0f, 32f, -32f, 64f, -64f, 96f, -96f, 128f };

        public static int Count => PlotInstance.All.Count;

        public static bool CanAdd(out string reason)
        {
            reason = null;
            if (Count >= MaxPlots)
            {
                reason = "You already have " + MaxPlots + " plots open. Delete one to add another.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers a plot that a loader just built and places it in front of the user.
        /// Safe to call more than once for the same holder.
        /// </summary>
        public static PlotInstance Place(GameObject holder, string title = null)
        {
            if (holder == null)
            {
                return null;
            }

            var existing = PlotInstance.Find(holder);
            if (existing != null)
            {
                return existing;
            }

            PlacePose(PlotInstance.NextFreeSlot(), out var position, out var rotation);
            holder.transform.SetPositionAndRotation(position, rotation);

            var instance = PlotInstance.Create(holder, string.IsNullOrEmpty(title) ? FindTitle(holder) : title);
            return instance;
        }

        public static void ClearAll()
        {
            var plots = new List<PlotInstance>(PlotInstance.All);
            foreach (var plot in plots)
            {
                if (plot != null)
                {
                    plot.Delete();
                }
            }

            // Anything tagged as a plot that never got registered (e.g. a loader failed half way)
            foreach (var orphan in GameObject.FindGameObjectsWithTag(PlotInstance.PlotTag))
            {
                var root = orphan.transform.parent != null ? orphan.transform.parent.gameObject : orphan;
                UnityEngine.Object.Destroy(root);
            }
        }

        /// <summary>
        /// Destroys plot containers that were created but never registered, which is what a loader that
        /// threw half way through leaves behind.
        /// </summary>
        public static void RemoveUnregistered()
        {
            foreach (var holder in GameObject.FindGameObjectsWithTag(PlotInstance.PlotTag))
            {
                if (PlotInstance.Find(holder) == null)
                {
                    var root = holder.transform.parent != null ? holder.transform.parent.gameObject : holder;
                    UnityEngine.Object.Destroy(root);
                }
            }
        }

        public static void PlacePose(int slot, out Vector3 position, out Quaternion rotation)
        {
            var camera = Camera.main;
            var origin = camera != null ? camera.transform.position : new Vector3(0f, 1.6f, 0f);
            var forward = camera != null ? camera.transform.forward : Vector3.forward;

            // Ignore pitch so plots stay upright even when the user looks down at the menu
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var yaw = SlotYaw[Mathf.Clamp(slot, 0, SlotYaw.Length - 1)] + DesktopPreview.PlotYaw;
            var direction = Quaternion.AngleAxis(yaw, Vector3.up) * forward;

            position = origin + direction * DesktopPreview.PlotDistance;
            position.y = origin.y - 0.05f;
            rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private static string FindTitle(GameObject holder)
        {
            // Plotters name their title object "title"
            foreach (var text in holder.GetComponentsInChildren<TextMesh>(true))
            {
                if (text.name == "title" && !string.IsNullOrEmpty(text.text))
                {
                    return text.text;
                }
            }

            return "Plot";
        }
    }
}
