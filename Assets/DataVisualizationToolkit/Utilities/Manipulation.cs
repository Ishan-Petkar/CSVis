using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

namespace DataVisualization.Plotter
{
    /// <summary>
    /// Makes plots grabbable, movable, rotatable and scalable with MRTK3
    /// (replaces the MRTK2 BoundingBox + ManipulationHandler pair).
    /// </summary>
    public static class Manipulation
    {
        // Copy of MRTK3's BoundingBoxWithTraditionalHandles prefab, created by the CSVis setup menu
        private const string BoundsVisualsPath = "CSVis/BoundsVisuals";

        /// <summary>
        /// Adds an ObjectManipulator and (when the visuals prefab is available) a BoundsControl.
        /// The target must already have a collider.
        /// </summary>
        public static ObjectManipulator MakeManipulable(GameObject target)
        {
            // Components are added while inactive so their Awake runs after configuration
            var wasActive = target.activeSelf;
            target.SetActive(false);

            var manipulator = target.GetComponent<ObjectManipulator>();
            if (manipulator == null)
            {
                manipulator = target.AddComponent<ObjectManipulator>();
            }
            manipulator.AllowedManipulations = TransformFlags.Move | TransformFlags.Rotate | TransformFlags.Scale;

            var boundsVisuals = Resources.Load<GameObject>(BoundsVisualsPath);
            if (boundsVisuals != null && target.GetComponent<BoundsControl>() == null)
            {
                var boundsControl = target.AddComponent<BoundsControl>();
                boundsControl.BoundsVisualsPrefab = boundsVisuals;
            }

            target.SetActive(wasActive);
            return manipulator;
        }

        /// <summary>
        /// Restricts rotation to the Y axis, like the MRTK2 RotationConstraintType.YAxisOnly.
        /// </summary>
        public static void ConstrainRotationToYAxis(GameObject target)
        {
            var constraint = target.GetComponent<RotationAxisConstraint>();
            if (constraint == null)
            {
                constraint = target.AddComponent<RotationAxisConstraint>();
            }
            constraint.ConstraintOnRotation = AxisFlags.XAxis | AxisFlags.ZAxis;
        }
    }
}
