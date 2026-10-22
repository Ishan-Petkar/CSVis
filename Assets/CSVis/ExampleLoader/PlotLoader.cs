using CSVis.Core;
using DataVisualization.Plotter;
using UnityEngine;

namespace CSVis.ExampleLoader
{
    public abstract class PlotLoader : MonoBehaviour
    {
        public abstract void LoadPlot();

        /// <summary>
        /// Registers a freshly built plot and places it in front of the user. Every loader calls this once its
        /// plot exists; <see cref="PlotManager"/> takes care of placement, limits and the plot toolbar.
        /// </summary>
        public void SetSpawn(GameObject plotContainer, string title = null)
        {
            if (plotContainer == null)
            {
                return;
            }

            PlotManager.Place(plotContainer, title);
            Manipulation.ConstrainRotationToYAxis(plotContainer);
        }
    }
}
