using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEngine;

namespace CSVis.IO
{
    public static class CsvUtils
    {
        
        /// <summary>
        /// Returns a dictionary of column value lists by their header names
        /// </summary>
        public static Dictionary<string, List<T>> GetColumnsByHeader<T>(string csvFilePath, params string[] headers)
        {
            var csvFile = Resources.Load(csvFilePath) as TextAsset;
            var valuesByName = new Dictionary<string, List<T>>();
            
            if (csvFile == null)
            {
                Debug.LogFormat("Csv file {0} was not loaded.", csvFilePath);
                return valuesByName;
            }

            using (TextReader reader = new StringReader(csvFile.text))
            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                csv.ReadHeader();
                
                Array.ForEach(headers, i => valuesByName.Add(i, new List<T>()));
                
                while (csv.Read())
                {
                    Array.ForEach(headers, i => valuesByName[i].Add(csv.GetField<T>(i)));
                }
            }

            return valuesByName;
        }
        
        /// <summary>
        /// Returns a dictionary of column value lists by their indices 
        /// </summary>
        public static Dictionary<int, List<T>> GetColumnsByIndex<T>(string csvFilePath, params int[] indices)
        {
            var csvFile = Resources.Load(csvFilePath) as TextAsset;
            var valuesByName = new Dictionary<int, List<T>>();
            
            if (csvFile == null)
            {
                Debug.LogFormat("Csv file {0} was not loaded.", csvFilePath);
                return valuesByName;
            }

            using (TextReader reader = new StringReader(csvFile.text))
            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                csv.ReadHeader();
                
                Array.ForEach(indices, i => valuesByName.Add(i, new List<T>()));
                
                while (csv.Read())
                {
                    Array.ForEach(indices, i => valuesByName[i].Add(csv.GetField<T>(i)));
                }
            }

            return valuesByName;
        }

    }
}