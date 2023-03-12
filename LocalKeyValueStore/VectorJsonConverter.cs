using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LocalKeyValueStore;

public class VectorJsonConverter<T, TF> : JsonConverter where T : struct where TF : struct
{
    private readonly string[] _fieldNames;

    public VectorJsonConverter(params string[] fieldNames)
    {
        _fieldNames = fieldNames;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(T);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var dictionary = serializer.Deserialize<Dictionary<string, TF>>(reader);
        var values = new object[_fieldNames.Length];
        for (int i = 0; i < _fieldNames.Length; i++)
        {
            values[i] = dictionary.TryGetValue(_fieldNames[i], out TF value) ? Convert.ChangeType(value, typeof(TF)) : 0;
        }

        var vector = Activator.CreateInstance(typeof(T), values);
        return vector;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var vector = (T)value;
        var dictionary = new Dictionary<string, TF>();
        for (int i = 0; i < _fieldNames.Length; i++)
        {
            var fieldValue = vector.GetType().GetField(_fieldNames[i]).GetValue(vector);
            dictionary[_fieldNames[i]] = (TF)Convert.ChangeType(fieldValue, typeof(TF));
        }

        serializer.Serialize(writer, dictionary);
    }
}