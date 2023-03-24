using System;
using System.IO;
using System.Linq;
using BaseX;
using CustomEntityFramework;
using CustomEntityFramework.Functions;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using LiteDB;
using NeosModLoader;
using Newtonsoft.Json;

namespace LocalKeyValueStore;

public class LocalKeyValueStore : NeosMod
{
    public const string DynVarSpaceName = "lkvs";

    public static ModConfiguration config;


    [AutoRegisterConfigKey] 
    public static ModConfigurationKey<string> DATA_PATH_KEY = new ModConfigurationKey<string>(
        "data_path", "The path in which item data will be stored. Changing this setting requires a game restart to apply.",
        () => Path.Combine(Engine.Current.LocalDB.PermanentPath, "lkvs")
    );
    
    //Config entry for allowing new entries to be created from public space
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<bool> ALLOW_PUBLIC_CREATION_KEY = new ModConfigurationKey<bool>(
        "allow_public_creation", "Allow new entries to be created from public space.",
        () => false
    );
    
    //Config entry for ignoring permissions for debug purposes
    [AutoRegisterConfigKey]
    public static ModConfigurationKey<bool> IGNORE_PERMISSIONS_KEY = new ModConfigurationKey<bool>(
        "ignore_permissions", "Ignore permissions for debug purposes.",
        () => false
    );


    private static LiteDatabase _db;

    private JsonSerializerSettings _jsonSettings;

    public override string Author => "KyuubiYoru";
    public override string Link => "https://github.com/KyuubiYoru/LocalKeyValueStore.git";
    public override string Name => "LocalKeyValueStore";
    public override string Version => "1.0.0";

    public override void OnEngineInit()
    {
        var harmony = new Harmony("net.KyuubiYoru.LocalKeyValueStore");
        harmony.PatchAll();
        config = GetConfiguration();

        CustomFunctionLibrary.RegisterFunction(GetFuncName("version"), () => Version);
        CustomFunctionLibrary.RegisterFunction(GetFuncName("write"), Write);
        CustomFunctionLibrary.RegisterFunction(GetFuncName("read"), Read);
        CustomFunctionLibrary.RegisterFunction(GetFuncName("listTables"), ListTables);
        CustomFunctionLibrary.RegisterFunction(GetFuncName("listKeys"), ListKeys);

        // CustomEntityFramework.AddComponent("Write",new DictionaryList<string, Type>()
        // {
        //     { "Key", typeof(string) },
        //     {"Table", typeof(string)},
        //     {"Value", typeof(Slot)},
        // });


            _jsonSettings = GetJsonTypeDefinition();

        Engine.Current.OnReady += () =>
        {
            try
            {
                string folderPath = config.GetValue(DATA_PATH_KEY);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, "lkvs.liteDB");
                _db = new LiteDatabase(filePath);
            }
            catch (Exception e)
            {
                Error(e);
            }
        };
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

    private void Write(Slot _slot, DynamicVariableSpace _space, string Key, string Table = "Default", bool saveNonPersistent = false)
    {
        try
        {
            bool isUserspace = _slot.World.IsUserspace();
            
            DynamicVariable value = _space.GetVariable("Value");
            
            if (value == null)
            {
                Error("Value is not defined in the CF DynamicVariableSpace!");
                return;
            }
            var collection = _db.GetCollection<ValueEntry>(Table);
            string typeString = $"{value.Type.FullName}, {value.Type.Assembly.GetName().Name}";//Type string without version number
            var entry = collection.FindOne(x => x.Key == Key && x.Type == typeString);

            if (!config.GetValue(IGNORE_PERMISSIONS_KEY))
            {
                if (!isUserspace && !entry.Permissions.HasFlag(PublicPermissions.Write)) return; // No write permission Todo: Feedback to user that write is not allowed

                if (!isUserspace && entry.Permissions.HasFlag(PublicPermissions.WriteReviewReq))
                {
                    //Write to pending collection and commit later from userspace
                    collection = _db.GetCollection<ValueEntry>(Table + "_Pending");
                    entry = collection.FindOne(x => x.Key == Key && x.Type == typeString);
                }
            }

            string valueJson = "";
            if (value.Type == typeof(Slot))
            {
                var slot = value.Value as Slot;
                if (slot != null && slot.GetComponentInChildren<SimpleAvatarProtection>(sap => !sap.CanSave) == null)
                {
                    var savedGraph = slot.SaveObject(DependencyHandling.CollectAssets, saveNonPersistent);
                    valueJson = DataTreeConverter.ToJSON(savedGraph.Root);
                }
            }
            else
            {
                valueJson = JsonConvert.SerializeObject(value.Value, _jsonSettings);
            }
            
            if (entry != null)
            {
                entry.Value = valueJson;
                collection.Update(entry);
            }
            else
            {
                if (config.GetValue(ALLOW_PUBLIC_CREATION_KEY))
                {
                    collection.Insert(new ValueEntry { Key = Key, Value = valueJson, Type = typeString });
                }
            }
        }
        catch (Exception e)
        {
            Msg(e);
        }
    }

    private Slot Read(Slot _slot, DynamicVariableSpace _space, string Key, string Table = "Default", bool ReadPending = true)
    {
        try
        {
            bool isUserspace = _slot.World.IsUserspace();
            DynamicVariable value = _space.GetVariable("Value");
            Type type = value.Type;
            string typeString = $"{type.FullName}, {type.Assembly.GetName().Name}";
            
            var collection = _db.GetCollection<ValueEntry>(Table);
            var collectionPending = _db.GetCollection<ValueEntry>(Table + "_Pending");
            
            ValueEntry entry = ReadPending
                ? collectionPending.FindOne(x => x.Key == Key && x.Type == typeString) ?? 
                  collection.FindOne(x => x.Key == Key && x.Type == typeString)
                : collection.FindOne(x => x.Key == Key && x.Type == typeString);
            
            if (!config.GetValue(IGNORE_PERMISSIONS_KEY) && !isUserspace && entry?.Permissions.HasFlag(PublicPermissions.Read) != true) {
                Msg("No read permission!");
                entry = null; // No read permission
            }
            
            if (entry != null)
            {
                Msg("Entry found.");
                
                if (type == null)
                {
                    Error($"Type {entry.Type} not found!");
                    return _slot;
                }

                if (type == typeof(Slot))
                {
                    var dataTreeDictionary = DataTreeConverter.FromJSON(entry.Value);
                    var newSlot = _slot.AddSlot("Value");
                    newSlot.LoadObject(dataTreeDictionary);
                    value.Value = newSlot;
                    return newSlot;
                }

                var valueObj = JsonConvert.DeserializeObject(entry.Value, type, _jsonSettings);
                value.Value = Convert.ChangeType(valueObj, type);
            }
            else if (value.Type == typeof(Slot))
            {
                Msg("Entry not found.");
                value.Value = null;
                return null;
            }
        }
        catch (Exception e)
        {
            Error(e);
        }

        return _slot;
    }
    
    //List all tables in the database returns a string with all tables separated by a newline
    private void ListTables(ref string Value)
    {
        //Todo: Add Permissions, need to add metadata to the database
        var tables = _db.GetCollectionNames();
        Value = string.Join(Environment.NewLine, tables);
    }
    
    //List all keys in a table returns a string with all keys,type separated by a newline
    private void ListKeys(ref string Value, string Table = "Default")
    {
        var collection = _db.GetCollection<ValueEntry>(Table);
        //select all entries where Permissions has List flag if not ignore permissions is set
        var entries = config.GetValue(IGNORE_PERMISSIONS_KEY) ? collection.FindAll() : collection.Find(x => x.Permissions.HasFlag(PublicPermissions.List));
        Value = string.Join(Environment.NewLine, entries.Select(x => $"{x.Key},{Type.GetType(x.Type)?.Name}"));
    }
}

[Serializable]
public class ValueEntry
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
    
    public PublicPermissions Permissions { get; set; }

    public override string ToString()
    {
        return $"Key: {Key}, Value: {Value}, Type: {Type}";
    }
}

//enum for permissions as Flags
[Flags]
public enum PublicPermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    WriteReviewReq = 4,
    List = 8,
}

