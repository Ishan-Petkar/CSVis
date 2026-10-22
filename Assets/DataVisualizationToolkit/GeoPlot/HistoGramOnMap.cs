namespace DataVisualization.Plotter
{
    using System;
    using Mapbox.Utils;
	using Mapbox.Unity.Map;
    using UnityEngine;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;

    public class HistoGramOnMap : MonoBehaviour
	{
        [Tooltip("The map to place the data points on")]
        public AbstractMap map;
        [Tooltip("The location to place data points on")]
        [Geocode]
		public string[] locationStrings;
        [Tooltip("The location height of each histogram bar")]
        public List<float> heightValues;
        [Tooltip("The colour of each histogram bar (default is white)")]
        public List<Color> colours;

        [Tooltip("changes size scale of the data points")]
        public float spawnScale = 0.015f;
        [Tooltip("changes max height of the bar")]
        public float HeightScaleMax = 0.95f;
        [Tooltip("changes min height of the bar")]
        public float HeightScaleMin = 0.05f;

        List <GameObject> _spawnedObjects;
        private readonly List<Vector2d> _locationList = new List<Vector2d>();
        private readonly List<float> _barHeights = new List<float>();
        private float maxHeight;
        private float minHeight;
		void Start()
		{
            _spawnedObjects = new List<GameObject>();
            if (map == null || locationStrings == null || heightValues == null || locationStrings.Length == 0 || heightValues.Count == 0)
            {
                // Nothing to draw (or the map failed to initialise); keep Update() idle
                return;
            }

            //if colors was not set default to white
            if (colours == null)
            {
                colours = new List<Color>();
            }
            while (colours.Count < locationStrings.Length)
            {
                colours.Add(Color.white);
            }

            maxHeight = Mathf.Max(heightValues.ToArray());
            minHeight = Mathf.Min(heightValues.ToArray());

            // Tolerate lists of different lengths rather than throwing half way through building the bars
            var count = Mathf.Min(locationStrings.Length, heightValues.Count);
			for (int i = 0; i < count; i++)
			{
                Vector2d location;
                try
                {
                    location = Conversions.StringToLatLon(locationStrings[i]);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Skipping map location '" + locationStrings[i] + "': " + e.Message);
                    continue;
                }

                var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // The bars are decoration; a collider would only get in the way of grabbing the map
                Destroy(instance.GetComponent<Collider>());
                instance.GetComponent<Renderer>().material.color = colours[i];
                instance.transform.localScale = new Vector3(spawnScale, (Util.Normalize(heightValues[i],maxHeight,minHeight)* HeightScaleMax)+ HeightScaleMin, spawnScale);
                //set the parent of the histogram bars to the MapHolder not the map itself since we dont wish for the bars to change scale when altering zoom levels
                instance.transform.parent = gameObject.transform.parent;
                instance.transform.localPosition = map.GeoToWorldPosition(location, true);
                _locationList.Add(location);
                _barHeights.Add(heightValues[i]);
				_spawnedObjects.Add(instance);
			}
		}

        private void Update()
		{
            if (_spawnedObjects == null || _spawnedObjects.Count == 0 || map == null || gameObject.transform.parent == null)
            {
                return;
            }

            GameObject mapHolder = gameObject.transform.parent.gameObject;
            var holderCollider = mapHolder.GetComponent<BoxCollider>();
            Vector3 currPos = mapHolder.transform.position;
            Vector3 currScale = mapHolder.transform.localScale;
            Quaternion rot = mapHolder.transform.rotation;
            //reset the plot and scale to origin to do geographical calculations as the API expects the map to be centred at world pos (0,0,0) and scale to be (1,1,1)
            mapHolder.transform.position = new Vector3(0, 0, 0);
            mapHolder.transform.localScale = new Vector3(1, 1, 1);
            mapHolder.transform.rotation = Quaternion.identity;
            int count = _spawnedObjects.Count;
            for (int i = 0; i < count; i++)
            {
                var spawnedObject = _spawnedObjects[i];
                spawnedObject.transform.localPosition = map.GeoToWorldPosition(_locationList[i], true);
                spawnedObject.transform.localScale = new Vector3(spawnScale, (Util.Normalize(_barHeights[i], maxHeight, minHeight) * HeightScaleMax) + HeightScaleMin, spawnScale);
                //postion the histogram bar above map
                spawnedObject.transform.localPosition = new Vector3(spawnedObject.transform.position.x, spawnedObject.transform.position.y + spawnedObject.transform.localScale.y / 2, spawnedObject.transform.position.z);
            }
            //once calculations are finished move plot back to its real postion
            mapHolder.transform.position = currPos;
            mapHolder.transform.rotation = rot;
            mapHolder.transform.localScale = currScale;

            //if the histogram bar appears outside the bounds due to zoom level hide them
            for (int i = 0; i < count; i++)
            {
                var spawnedObject = _spawnedObjects[i];
                spawnedObject.SetActive(holderCollider == null || holderCollider.bounds.Contains(spawnedObject.transform.position));
            }
        }
    }
}
