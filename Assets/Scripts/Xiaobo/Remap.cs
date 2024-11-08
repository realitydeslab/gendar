using UnityEngine;

namespace Xiaobo.UnityToolkit.Core
{
    using Vector2D = UnityEngine.Vector2;
    using Vector3D = UnityEngine.Vector3;
    using Vector4D = UnityEngine.Vector4;

    public class Remap
    {
        #region enums

        /// <summary>
        /// vvvv like modi for the Map function
        /// </summary>
        public enum MapMode
        {
            /// <summary>
            /// Maps the value continously
            /// </summary>
            Float,
            /// <summary>
            /// Maps the value, but clamps it at the min/max borders of the output range
            /// </summary>
            Clamp,
            /// <summary>
            /// Maps the value, but repeats it into the min/max range, like a modulo function
            /// </summary>
            Wrap,
            /// <summary>
            /// Maps the value, but mirrors it into the min/max range, always against either start or end, whatever is closer
            /// </summary>
            Mirror
        };

        #endregion enums
        /// <summary>
        /// This Method can be seen as an inverse of Lerp (in Mode Float). Additionally it provides the infamous Mapping Modes, author: velcrome
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="start">Minimum of input value range</param>
        /// <param name="end">Maximum of input value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="VVVV.Utils.VMath.TMapMode">TMapMode</see></param>
        /// <returns>Input value mapped from input range into destination range</returns>
        public static float Ratio(float Input, float start, float end, MapMode mode = MapMode.Float)
        {
            if (end.CompareTo(start) == 0) return 0;

            float range = end - start;
            float ratio = (Input - start) / range;

            if (mode == MapMode.Float) { }
            else if (mode == MapMode.Clamp)
            {
                if (ratio < 0) ratio = 0;
                if (ratio > 1) ratio = 1;
            }
            else
            {
                if (mode == MapMode.Wrap)
                {
                    // includes fix for inconsistent behaviour of old delphi Map 
                    // node when handling integers
                    int rangeCount = Mathf.FloorToInt(ratio);
                    ratio -= rangeCount;
                }
                else if (mode == MapMode.Mirror)
                {
                    // merke: if you mirror an input twice it is displaced twice the range. same as wrapping twice really
                    int rangeCount = Mathf.FloorToInt(ratio);
                    rangeCount -= rangeCount & 1; // if uneven, make it even. bitmask of one is same as mod2
                    ratio -= rangeCount;

                    if (ratio > 1) ratio = 2 - ratio; // if on the max side of things now (due to rounding down rangeCount), mirror once against max
                }
            }
            return ratio;
        }

        /// <summary>
        /// The infamous Map function of vvvv for values
        /// </summary>
        /// <param name="Input">Input value to convert</param>
        /// <param name="InMin">Minimum of input value range</param>
        /// <param name="InMax">Maximum of input value range</param>
        /// <param name="OutMin">Minimum of destination value range</param>
        /// <param name="OutMax">Maximum of destination value range</param>
        /// <param name="mode">Defines the behavior of the function if the input value exceeds the destination range 
        /// <see cref="VVVV.Utils.VMath.TMapMode">TMapMode</see></param>
        /// <returns>Input value mapped from input range into destination range</returns>
        public static float Map(float Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode = MapMode.Float)
        {
            float ratio = Ratio(Input, InMin, InMax, mode);
            return Mathf.Lerp(OutMin, OutMax, ratio);
        }

        public static Vector2D Map(Vector2D Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode = MapMode.Float)
        {
            return new Vector2D(Map(Input.x, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.y, InMin, InMax, OutMin, OutMax, mode));
        }

        public static Vector3D Map(Vector3D Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode = MapMode.Float)
        {
            return new Vector3D(Map(Input.x, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.y, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.z, InMin, InMax, OutMin, OutMax, mode));
        }

        public static Vector4D Map(Vector4D Input, float InMin, float InMax, float OutMin, float OutMax, MapMode mode = MapMode.Float)
        {
            return new Vector4D(Map(Input.x, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.y, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.z, InMin, InMax, OutMin, OutMax, mode),
                                Map(Input.w, InMin, InMax, OutMin, OutMax, mode));
        }

        public static Vector2D Map(Vector2D Input, Vector2D InMin, Vector2D InMax, Vector2D OutMin, Vector2D OutMax, MapMode mode = MapMode.Float)
        {
            return new Vector2D(Map(Input.x, InMin.x, InMax.x, OutMin.x, OutMax.x, mode),
                                Map(Input.y, InMin.y, InMax.y, OutMin.y, OutMax.y, mode));
        }

        public static Vector3D Map(Vector3D Input, Vector3D InMin, Vector3D InMax, Vector3D OutMin, Vector3D OutMax, MapMode mode = MapMode.Float)
        {
            return new Vector3D(Map(Input.x, InMin.x, InMax.x, OutMin.x, OutMax.x, mode),
                                Map(Input.y, InMin.y, InMax.y, OutMin.y, OutMax.y, mode),
                                Map(Input.z, InMin.z, InMax.z, OutMin.z, OutMax.z, mode));
        }

        public static Vector4D Map(Vector4D Input, Vector4D InMin, Vector4D InMax, Vector4D OutMin, Vector4D OutMax, MapMode mode = MapMode.Float)
        {
            return new Vector4D(Map(Input.x, InMin.x, InMax.x, OutMin.x, OutMax.x, mode),
                                Map(Input.y, InMin.y, InMax.y, OutMin.y, OutMax.y, mode),
                                Map(Input.z, InMin.z, InMax.z, OutMin.z, OutMax.z, mode),
                                Map(Input.w, InMin.w, InMax.w, OutMin.w, OutMax.w, mode));
        }
    }
}
