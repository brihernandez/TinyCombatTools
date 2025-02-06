using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.World;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Falcon.Weapons
{
    public enum BulletType
    {
        Machinegun,
        TankShell,
        AntiAir,
        Aircraft,
    }

    class ColorRGBConverter : JsonConverter<Color> {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer) {
            writer.WriteStartArray();
            writer.WriteValue((int)(value.r * 255));
            writer.WriteValue((int)(value.g * 255));
            writer.WriteValue((int)(value.b * 255));
            writer.WriteEndArray();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }

    class IgnoreBaseContractResolver : DefaultContractResolver {
        private Type _stopAtBaseType;

        public IgnoreBaseContractResolver(Type stopAtBaseType)
        {
            _stopAtBaseType = stopAtBaseType;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> defaultProperties = base.CreateProperties(type, memberSerialization);
            List<string> includedProperties = Utilities.GetPropertyNames(type, _stopAtBaseType);

            return defaultProperties.Where(p => includedProperties.Contains(p.PropertyName)).ToList();
        }


        private static class Utilities
        {
            /// <summary>
            /// Gets a list of all public instance properties of a given class type
            /// excluding those belonging to or inherited by the given base type.
            /// </summary>
            /// <param name="type">The Type to get property names for</param>
            /// <param name="stopAtBaseType">A base type inherited by type whose properties should not be included.</param>
            /// <returns></returns>
            public static List<string> GetPropertyNames(Type type, Type stopAtBaseType)
            {
                List<string> propertyNames = new List<string>();

                if (type == null || type == stopAtBaseType) return propertyNames;

                Type currentType = type;

                do
                {
                    var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);

                    foreach (var property in properties)
                        if (!propertyNames.Contains(property.Name))
                            propertyNames.Add(property.Name);

                    currentType = currentType.BaseType;
                } while (currentType != null && currentType != stopAtBaseType);

                return propertyNames;
            }
        }
    }

    [Serializable]
    [CreateAssetMenu(fileName = "Bullet", menuName = "Tiny Combat Arena/Bullet", order = 1)]
    public class BulletData : LoadableData
    {
        public BulletType Type = BulletType.Machinegun;

        [Header("Tracer")]
        [JsonConverter(typeof(ColorRGBConverter))]
        // public int[] TracerColor = { 255, 255, 255 };
        public Color TracerColor = Color.white;
        public float TracerSize = 1.5f;
        public int TracerLength = 3;

        [Header("Motion")]
        public float TimeToLive = 5f;
        public float Gravity = 0f;
        public bool IsThick = false;
        public float BulletDiameter = 1f;

        [Header("Impact Damage")]
        public int ImpactDamage = 5;
        public int ImpactPenetration = 0;
        public float ImpactForce = 50f;
        public bool IsFriendlyFireEnabled = false;

        [Header("Explosive Damage")]
        public bool ExplodeOnImpact = false;
        public bool ExplodeOnTimeout = false;
        public float BlastRadius = 5f;
        public int SplashPenetration = 0;
        public int SplashDamage = 0;

        [Header("Effects")]
        public CraterSize CraterSize = CraterSize.Small;
        public string GroundImpactFxName = "GrassMG";
        public string WaterImpactFxName = "WaterMG";
        public string HitFxName = "HitMG";
        public string PenetrateFxName = "PenetrateMG";
        public string ExplodeFxName = "ExplosionMG";

        [Button]
        private void Export() {

            // Debug.Log(this.ToJSONString(new TCASerializer()));
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            settings.ContractResolver = new IgnoreBaseContractResolver(typeof(UnityEngine.Object));
            Debug.Log(JsonConvert.SerializeObject(this, settings));

        }

        public Color GetTracerColor()
        {
            return new Color(TracerColor[0] / 255f, TracerColor[1] / 255f, TracerColor[2] / 255f, 1f);
        }
    }
}
