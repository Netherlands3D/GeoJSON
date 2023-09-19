﻿using System;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using UnityEngine;

namespace Netherlands.GeoJSON
{
    public static class GeoJSONExtensionMethods
    {
        private const int DEFAULT_GEOJSON_CRS_EPSG_CODE = 4326;

        public static int EPSGId(this GeoJSONObject geoJsonObject)
        {
            int epsgId = DEFAULT_GEOJSON_CRS_EPSG_CODE;

            NamedCRS crsObject = geoJsonObject.CRS as NamedCRS;
            if (crsObject == null) return epsgId;
    
            crsObject.Properties.TryGetValue("name", out var crsName);

            if (crsName is not string crsNameString || !crsNameString.StartsWith("urn:ogc:def:crs:EPSG")) return epsgId;
            
            int.TryParse(crsNameString.Split(':').Last(), out epsgId);

            return epsgId;
        }

        /// <summary>
        /// Suppliers of GeoJSON files are not required to use the ID field of a feature; but they will regularly pass
        /// an ID or OBJECTID property with the id. Because of this, we need to check all permutations to make sure we
        /// get an id.
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="defaultIdentifier"></param>
        /// <returns></returns>
        public static string TryGetIdentifier(this Feature feature, string defaultIdentifier)
        {
            // Use the official ID
            object id = feature.Id;
            if (id != null) return id.ToString();
            
            // .. or the unofficial id if it exists
            feature.Properties.TryGetValue("id", out id); 
            if (id != null) return id.ToString();
            
            // .. or the unofficial ID if it exists
            feature.Properties.TryGetValue("ID", out id); 
            if (id != null) return id.ToString();
            
            // .. or the autogenerated objectid if it exists
            feature.Properties.TryGetValue("objectid", out id);
            if (id != null) return id.ToString();

            // .. or the autogenerated OBJECT if it exists
            feature.Properties.TryGetValue("OBJECTID", out id); 
            if (id != null) return id.ToString();

            // Supplier doesn't care, so we use the defaultIdentifier that was passed
            return defaultIdentifier; 
        }
        
        public static double[] DerivedBoundingBoxes(this FeatureCollection featureCollection)
        {
            // We use max values as initial values so that we _know_ for sure that the first encountered position
            // will initialize the array. Using 0 as initial value can cause issues
            double[] boundingBox = { double.MaxValue, double.MaxValue, double.MinValue, Double.MinValue };

            foreach (Feature feature in featureCollection.Features)
            {
                MultiPolygon geometry = feature.Geometry as MultiPolygon;
                if (geometry != null)
                {
                    foreach (var poly in geometry.Coordinates)
                    {
                        AdjustBoundingBoxBasedOnPolygon(poly, boundingBox);
                    }

                    continue;
                }

                Polygon polygon = feature.Geometry as Polygon;
                if (polygon != null)
                {
                    AdjustBoundingBoxBasedOnPolygon(polygon, boundingBox);
                }
            }

            return boundingBox;
        }

        private static void AdjustBoundingBoxBasedOnPolygon(Polygon polygon, double[] boundingBox)
        {
            foreach (var lineString in polygon.Coordinates)
            {
                foreach (var position in lineString.Coordinates)
                {
                    if (position.Longitude < boundingBox[0]) boundingBox[0] = position.Longitude;
                    if (position.Longitude > boundingBox[2]) boundingBox[2] = position.Longitude;
                    if (position.Latitude < boundingBox[1]) boundingBox[1] = position.Latitude;
                    if (position.Latitude > boundingBox[3]) boundingBox[3] = position.Latitude;
                }
            }
        }
    }
}