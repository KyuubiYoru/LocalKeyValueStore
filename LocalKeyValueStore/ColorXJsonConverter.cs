using System;
using BaseX;
using Newtonsoft.Json;

namespace LocalKeyValueStore;

public class ColorXJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(colorX);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var values = new float[4];
        int index = 0;
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            values[index] = Convert.ToSingle(reader.Value);
            index++;
        }

        return new colorX(values[0], values[1], values[2], values[3]);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var colorX = (colorX)value;
        writer.WriteStartArray();
        writer.WriteValue(colorX.r);
        writer.WriteValue(colorX.g);
        writer.WriteValue(colorX.b);
        writer.WriteValue(colorX.a);
        writer.WriteEndArray();
    }
}