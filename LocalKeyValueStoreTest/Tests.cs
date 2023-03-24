using System;
using BaseX;
using FrooxEngine;
using LiteDB;
using LocalKeyValueStore;
using Newtonsoft.Json;
using NUnit.Framework;

namespace NeosCCFTest
{
    [TestFixture]
    public partial class Tests
    {
        private LiteDatabase _db;


        [Test]
        public void SerializerTest()
        {
            Type type = typeof(Slot);
            string differentVersion = "FrooxEngine.Slot, FrooxEngine, Version=2022.1.28.1335, Culture=neutral, PublicKeyToken=null";
            string typeString = $"{type.FullName}, {type.Assembly.GetName().Name}";
            
            string json = JsonConvert.SerializeObject(type);
            
            Type type2 = Type.GetType(typeString);
            Type type3 = JsonConvert.DeserializeObject<Type>(json);
            Type type4 = Type.GetType(differentVersion);
            
            
            Assert.AreEqual(type, type2);

        }
    }
}