using System;
using System.IO;
using BaseX;
using CustomEntityFramework;
using CustomEntityFramework.Functions;
using FrooxEngine;
using HarmonyLib;
using LiteDB;
using NeosModLoader;
using Newtonsoft.Json;

namespace LocalKeyValueStore;

public class LocalKeyValueStore : NeosMod
{
    public const string DynVarSpaceName = "lkvs";

    public static ModConfiguration config;


    [AutoRegisterConfigKey] public static ModConfigurationKey<string> DATA_PATH_KEY = new(
        "data_path", "The path in which item data will be stored. Changing this setting requires a game restart to apply.",
        () => Path.Combine(Engine.Current.DataPath, "lkvs")
    );


    private LiteDatabase _db;

    private JsonSerializerSettings _jsonSettings;

    public override string Author => "KyuubiYoru";
    public override string Link => "https://github.com/KyuubiYoru/LocalKeyValueStore.git";
    public override string Name => "LocalKeyValueStore";
    public override string Version => "1.0.0";

    public override void OnEngineInit()
    {
        var harmony = new Harmony("net.KyuubiYoru.LocalKeyValueStore");
        config = GetConfiguration();


        if (Engine.Current == null)
        {
            Msg("Engine.Current is null");
        }
        else if (Engine.Current.DataPath == null)
        {
            Msg("Engine.Current.LocalDB is null");
        }
        else if (Engine.Current.LocalDB.PermanentPath == null)
        {
            Msg("Engine.Current.LocalDB.PermanentPath is null");
        }


        CustomFunctionLibrary.RegisterFunction(GetFuncName("version"), () => Version);
        CustomFunctionLibrary.RegisterFunction(GetFuncName("write"), Write);
        CustomFunctionLibrary.RegisterFunction(GetFuncName("read"), Read);
        //CustomFunctionLibrary.RegisterFunction("LKVS.Delete", Delete );


        string folderPath = Path.Combine(config.GetValue(DATA_PATH_KEY), "lkvs");

        Msg("Path: " + folderPath);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "lkvs.liteDB");

        //Create liteDB database
        _db = new LiteDatabase(filePath);

        _jsonSettings = GetJsonTypeDefinition();
    }

    private string GetFuncName(string funcName)
    {
        return DynVarSpaceName + "." + funcName;
    }

    public JsonSerializerSettings GetJsonTypeDefinition()
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();

        settings.Converters.Add(new VectorJsonConverter<float2x2, float>(nameof(float2x2.m00), nameof(float2x2.m01), nameof(float2x2.m10), nameof(float2x2.m11)));

        settings.Converters.Add(new VectorJsonConverter<float3x3, float>(nameof(float3x3.m00), nameof(float3x3.m01), nameof(float3x3.m02), nameof(float3x3.m10),
            nameof(float3x3.m11), nameof(float3x3.m12), nameof(float3x3.m20), nameof(float3x3.m21), nameof(float3x3.m22)));

        settings.Converters.Add(new VectorJsonConverter<float4x4, float>(nameof(float4x4.m00), nameof(float4x4.m01), nameof(float4x4.m02), nameof(float4x4.m03),
            nameof(float4x4.m10), nameof(float4x4.m11), nameof(float4x4.m12), nameof(float4x4.m13), nameof(float4x4.m20), nameof(float4x4.m21), nameof(float4x4.m22),
            nameof(float4x4.m23), nameof(float4x4.m30), nameof(float4x4.m31), nameof(float4x4.m32), nameof(float4x4.m33)));

        settings.Converters.Add(new VectorJsonConverter<floatQ, float>(nameof(floatQ.x), nameof(floatQ.y), nameof(floatQ.z), nameof(floatQ.w)));
        settings.Converters.Add(new VectorJsonConverter<color, float>(nameof(color.r), nameof(color.g), nameof(color.b), nameof(color.a)));

        settings.Converters.Add(new VectorJsonConverter<double2x2, double>(nameof(double2x2.m00), nameof(double2x2.m01), nameof(double2x2.m10), nameof(double2x2.m11)));
        settings.Converters.Add(new VectorJsonConverter<double3x3, double>(nameof(double3x3.m00), nameof(double3x3.m01), nameof(double3x3.m02), nameof(double3x3.m10),
            nameof(double3x3.m11), nameof(double3x3.m12), nameof(double3x3.m20), nameof(double3x3.m21), nameof(double3x3.m22)));
        settings.Converters.Add(new VectorJsonConverter<double4x4, double>
        (nameof(double4x4.m00), nameof(double4x4.m01), nameof(double4x4.m02), nameof(double4x4.m03),
            nameof(double4x4.m10), nameof(double4x4.m11), nameof(double4x4.m12), nameof(double4x4.m13),
            nameof(double4x4.m20), nameof(double4x4.m21), nameof(double4x4.m22), nameof(double4x4.m23),
            nameof(double4x4.m30), nameof(double4x4.m31), nameof(double4x4.m32), nameof(double4x4.m33)));

        settings.Converters.Add(new ColorXJsonConverter());

        return settings;
    }

    private void Write(Slot _slot, DynamicVariableSpace _space, string Key, string Table = "Default")
    {
        DynamicVariable value;
        try
        {
            value = _space.GetVariable("Value");
        }
        catch (Exception e)
        {
            Msg(e);
            return;
        }

        try
        {
            var collection = _db.GetCollection<ValueEntry>(Table);
            var entry = collection.FindOne(x => x.Key == Key && x.Type == value.Type.AssemblyQualifiedName);
            var valueJson = JsonConvert.SerializeObject(value.Value, _jsonSettings);

            if (entry != null)
            {
                entry.Value = valueJson;
                collection.Update(entry);
            }
            else
            {
                collection.Insert(new ValueEntry { Key = Key, Value = valueJson, Type = value.Type.AssemblyQualifiedName });
            }
        }
        catch (Exception e)
        {
            Msg(e);
        }
    }


    private void Read(Slot _slot, DynamicVariableSpace _space, string Key, string Table = "Default")
    {
        Msg("Try to get value");
        DynamicVariable value;
        try
        {
            value = _space.GetVariable("Value");
        }
        catch (Exception e)
        {
            Msg(e);
            return;
        }

        var collection = _db.GetCollection<ValueEntry>(Table);

        var entry = collection.FindOne(x => x.Key == Key && x.Type == value.Type.AssemblyQualifiedName);

        if (entry != null)
        {
            Msg("Set value: " + entry.Value);
            try
            {
                Type type = Type.GetType(entry.Type);
                var valueObj = JsonConvert.DeserializeObject(entry.Value, type, _jsonSettings);
                Msg("Value: " + valueObj + " Type: " + valueObj.GetType() + "");

                value.Value = Convert.ChangeType(valueObj, type);
            }
            catch (Exception e)
            {
                Msg(e);
            }
        }
    }
}

[Serializable]
public class ValueEntry
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}, Type: {Type}";
    }
}