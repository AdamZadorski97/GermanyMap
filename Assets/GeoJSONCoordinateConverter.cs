using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class GeoJSONCoordinateConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(GeoJSONCoordinate));
    }
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Array)
        {
            if (token[0].Type == JTokenType.Float)
            {
                // This will handle the simpler case where the coordinates array is directly an array of doubles
                double[] coords = token.ToObject<double[]>();
                return new GeoJSONCoordinate { coordinates = coords };
            }
            else if (token[0].Type == JTokenType.Array)
            {
                // We are assuming this is a Polygon or MultiPolygon
                JArray firstArray = (JArray)token[0];

                if (firstArray[0].Type == JTokenType.Float)
                {
                    double[] coords = firstArray.ToObject<double[]>();
                    return new GeoJSONCoordinate { coordinates = coords };
                }
                else if (firstArray[0].Type == JTokenType.Array)
                {
                    // This indicates a MultiPolygon
                    JArray coordArray = (JArray)firstArray[0];
                    double[] coords = coordArray.ToObject<double[]>();
                    return new GeoJSONCoordinate { coordinates = coords };
                }
            }
        }

        throw new JsonSerializationException("Unexpected token type: " + token.Type);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}