using UnityEngine;

namespace Falcon.World
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Environment))]
    public class EnvironmentToShader : MonoBehaviour
    {
        private int sunColorId = -1;
        private int sunDirectionId = -1;
        private int ambientLightId = -1;

        private int fogId = -1;

        private int screenHeightId = -1;
        private int screenWidthId = -1;

        private Environment environment;

        private void Awake()
        {
            GetShaderIDs();
            environment = GetComponent<Environment>();
        }

        private void OnValidate()
        {
            GetShaderIDs();
            environment = GetComponent<Environment>();
        }

        private void Update()
        {
            Shader.SetGlobalVector(sunDirectionId, -environment.SunDirection);
            Shader.SetGlobalColor(sunColorId, environment.TODSunColor);

            Shader.SetGlobalColor(ambientLightId, environment.TODAmbientColor);

            Vector4 fogVector = Vector4.zero;
            fogVector.x = environment.FogStart;
            fogVector.y = environment.FogEnd;
            Shader.SetGlobalVector(fogId, fogVector);

            Shader.SetGlobalInt(screenHeightId, Screen.height);
            Shader.SetGlobalInt(screenWidthId, Screen.width);
        }

        private void GetShaderIDs()
        {
            sunColorId = Shader.PropertyToID("SunColor");
            sunDirectionId = Shader.PropertyToID("SunDirection");
            ambientLightId = Shader.PropertyToID("AmbientColor");

            fogId = Shader.PropertyToID("FogFade");

            screenHeightId = Shader.PropertyToID("ScreenHeight");
            screenWidthId = Shader.PropertyToID("ScreenWidth");
        }
    }
}

