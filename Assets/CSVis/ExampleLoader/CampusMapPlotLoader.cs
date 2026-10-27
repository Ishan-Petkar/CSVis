using System.Collections.Generic;
using System.Linq;
using CSVis.ExampleLoader.Helper;
using CSVis.IO;
using DataVisualization.Plotter;
using UnityEngine;

namespace CSVis.ExampleLoader
{
    public class CarletonUniversityMapPlotLoader : PlotLoader
    {
        private List<string> _locationStrings = new List<string>
        {
            "45.380942, -75.699051", // Lobe Cafe
            "45.381952, -75.698955", // Tunnel Junction
            "45.383331, -75.694009", // bent coin
            "45.383662, -75.698118", // Tims (res)
            "45.383549, -75.697850", // Bakers
            "45.383319, -75.697646", // Pizza Pizza
            "45.383424, -75.697512", // subway
            "45.387141, -75.697389", // the caf 
            "45.387190, -75.697215", // oasis
            "45.386240, -75.693781", // Tims (fitness)
            "45.384492, -75.693803"  // T & J chiken 
        };
        
        private List<float> _locationValues = new List<float>
        {
            4.2f, // Lobe Cafe
            4.6f, // Tunnel Junction
            4.7f, // bent coin
            3.3f, // Tims (res)
            4.2f, // Bakers
            2.8f, // Pizza Pizza
            2.4f, // subway
            3.7f, // the caf 
            3.7f, // oasis
            3.8f, // Tims (fitness)
            4.1f  // T & J chicken 
        };
        
        private const float Longitude = 45.3831f;
        
        private const float Latitude = -75.6976f;
        
        private const int Zoom = 16;
        
        private const string Title = "CU Dining Reviews";
        
        private const float SpawnScale = 0.001f;
        
        private const float HeightScaleMax = 0.2f;
        
        private const float HeightScaleMin = 0f;
        
        public override void LoadPlot()
        {
            var map = Instantiate(GetCarletonMapper());
            SetSpawn(map.MapHolder);
        }

        public mrMap GetCarletonMapper()
        {
            var geoMapPlotterHelper = gameObject.AddComponent<GeoMapPlotHelper>();

            var colors = Enumerable.Range(0, _locationStrings.Count).Select(i =>
                new Color(Random.value, Random.value, Random.value)
            ).ToList();

            var data = new GeoMapPlotHelper.GeoMapData(
                Title,
                Longitude,
                Latitude,
                Zoom,
                GeoMapPlotHelper.GeoDefaultPlotScale,
                _locationStrings,
                _locationValues,
                colors,
                SpawnScale,
                HeightScaleMax,
                HeightScaleMin,
                PlotHelper.GetPlotHolder()
            );
            
            return geoMapPlotterHelper.GetMrMap(data);
        }
    }
}