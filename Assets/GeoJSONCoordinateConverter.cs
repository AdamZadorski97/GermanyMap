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
            double[] coords = token.ToObject<double[]>();
            return new GeoJSONCoordinate { coordinates = coords };
        }
        else if (token.Type == JTokenType.Float)
        {
            double singleCoord = token.ToObject<double>();
            return new GeoJSONCoordinate { coordinates = new double[] { singleCoord } };
        }
        throw new JsonSerializationException("Unexpected token type: " + token.Type);
    }
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}