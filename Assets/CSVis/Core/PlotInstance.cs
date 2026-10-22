using System;
using System.Collections.Generic;
using CSVis.UI;
using UnityEngine;

namespace CSVis.Core
{
    /// <summary>
    /// One plot in the scene. Lives on the plot's container object so destroying the container
    /// removes the whole plot (points, labels, bounds handles, toolbar).
    /// </summary>
    public sealed class PlotInstance : MonoBehaviour
    {
        public const string PlotTag = "plotContainer";

        private static readonly List<PlotInstance> AllInstances = new List<PlotInstance>();

        public static IReadOnlyList<PlotInstance> All => AllInstances;

        /// <summary>Raised whenever a plot is added or removed.</summary>
        public static event Action CountChanged;

        private PlotToolbar toolbar;
        private int slot;

        public GameObject Holder { get; private set; }

        public string Title { get; private set; }

        public static PlotInstance Find(GameObject holder)
        {
            foreach (var instance in AllInstances)
            {
                if (instance != null && instance.Holder == holder)
                {
                    return instance;
                }
            }

            return null;
        }

        public static int NextFreeSlot()
        {
            var used = new HashSet<int>();
            foreach (var instance in AllInstances)
            {
                if (instance != null)
                {
                    used.Add(instance.slot);
                }
            }

            var slot = 0;
            while (used.Contains(slot))
            {
                slot++;
            }

            return slot;
        }

        public static PlotInstance Create(GameObject holder, string title)
        {
            // The container is the parent when there is one, so the whole hierarchy goes away together
            var root = holder.transform.parent != null ? holder.transform.parent.gameObject : holder;
            var instance = root.AddComponent<PlotInstance>();
            instance.Holder = holder;
            instance.Title = title;
            instance.slot = NextFreeSlot();
            AllInstances.Add(instance);
            CountChanged?.Invoke();
            return instance;
        }

        private void Start()
        {
            toolbar = PlotToolbar.TryCreate(this);
        }

        private void LateUpdate()
        {
            if (Holder == null)
            {
                // The holder was destroyed from elsewhere; don't leave an empty shell around
                Destroy(gameObject);
            }
        }

        /// <summary>Turns the plot back toward the user, upright, keeping its position.</summary>
        public void FaceUser()
        {
            if (Holder == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var toPlot = Holder.transform.position - camera.transform.position;
            toPlot.y = 0f;
            if (toPlot.sqrMagnitude > 0.0001f)
            {
                Holder.transform.rotation = Quaternion.LookRotation(toPlot.normalized, Vector3.up);
            }
        }

        /// <summary>Moves the plot back to its default spot in front of the user.</summary>
        public void Recenter()
        {
            if (Holder == null)
            {
                return;
            }

            PlotManager.PlacePose(slot, out var position, out var rotation);
            Holder.transform.SetPositionAndRotation(position, rotation);
            Holder.transform.localScale = Vector3.one;
        }

        public void Delete()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (AllInstances.Remove(this))
            {
                CountChanged?.Invoke();
            }
        }
    }
}
