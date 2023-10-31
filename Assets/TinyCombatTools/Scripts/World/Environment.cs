using UnityEngine;
using TimeSpan = System.TimeSpan;

namespace Falcon.World
{
    public class Environment : MonoBehaviour
    {
        [Header("Time of Day")]
        public float Timescale = 0f;
        [SerializeField] private Transform Sun = null;
        [SerializeField, Range(0, SecondsInDay)]
        private float TimeOfDaySeconds = 36000f;
        [SerializeField] private Gradient SunColor = new Gradient();
        [SerializeField] private Gradient AmbientColor = new Gradient();

        [Header("Fog")]
        public float FogStart = 25000f;
        public float FogEnd = 50000f;

        public const float SecondsInDay = 86400f;
        public const float SecondsInHour = 3600f;

        public TimeSpan TODTimespan { get; private set; } = new TimeSpan();

        public Vector3 SunDirection => Sun.forward;

        public Color TODSunColor { get; private set; } = Color.white;
        public Color TODAmbientColor { get; private set; } = Color.black;

        private void OnValidate()
        {
            if (Sun != null)
                SetTimeOfDaySeconds(TimeOfDaySeconds);
        }

        private void Start()
        {
            // Reset this on start to force the shader to update.
            SetTimeOfDaySeconds(TimeOfDaySeconds);
        }

        private void Update()
        {
            if (Timescale > 0f)
            {
                TimeOfDaySeconds += Time.deltaTime * Timescale;
                TimeOfDaySeconds %= SecondsInDay;
                SetTimeOfDaySeconds(TimeOfDaySeconds);
            }
        }

        public void SetTimeOfDayHours(float hours)
        {
            var timeInSeconds = HoursToSeconds(hours);
            SetTimeOfDaySeconds(timeInSeconds);
        }

        public void SetTimeOfDaySeconds(float seconds)
        {
            TimeOfDaySeconds = seconds;
            TODTimespan = TimeSpan.FromSeconds(seconds);

            // Determine the angle the sun needs to be rotated to.
            float timeOfDayLerp = Mathf.InverseLerp(0, SecondsInDay, TimeOfDaySeconds);
            float angle = Mathf.Lerp(360, 0, timeOfDayLerp);
            angle -= 90f;

            // Rotate the sun.
            Sun.transform.localRotation = Quaternion.Euler(angle, 90f, 0f);

            // Set the sun's color based on the time of day gradient.
            TODSunColor = SunColor.Evaluate(timeOfDayLerp);
            TODAmbientColor = AmbientColor.Evaluate(timeOfDayLerp);
        }

        private float HoursToSeconds(float hour24)
        {
            return hour24 * SecondsInHour;
        }
    }
}
