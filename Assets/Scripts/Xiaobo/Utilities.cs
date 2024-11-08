using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utilities
{
    public static float SmoothValue(float cur, float dst, float t = 0.2f)
    {
        if (t == 0)
            return dst;

        float cur_vel = 0;
        return Mathf.SmoothDamp(cur, dst, ref cur_vel, t);
    }

    public static float Remap(float v, float src_min, float src_max, float dst_min, float dst_max, bool need_clamp = false)
    {
        if (need_clamp)
            v = Mathf.Clamp(v, src_min, src_max);

        if (src_min == src_max)
            v = 0;
        else
            v = (v - src_min) / (src_max - src_min);

        return v * (dst_max - dst_min) + dst_min;
    }
}
