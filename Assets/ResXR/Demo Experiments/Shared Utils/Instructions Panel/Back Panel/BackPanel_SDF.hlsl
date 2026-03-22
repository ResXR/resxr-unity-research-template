#ifndef UI_BACKFACE_SDF_INCLUDED
#define UI_BACKFACE_SDF_INCLUDED

inline void SDF_RoundedRect_float(
    float2 p, float2 halfSize, float r, float feather, float border,
    out float fillA, out float borderA)
{
    // init outs (prevents "not initialized" error)
    fillA = 0.0;
    borderA = 0.0;

    r = min(r, min(halfSize.x, halfSize.y));
    float2 q = abs(p) - (halfSize - r);
    float d = length(max(q, 0.0)) - r;

    float aOuter = saturate(0.5 - d / max(feather, 1e-6));
    float aInner = saturate(0.5 - (d + border) / max(feather, 1e-6));

// new: fill excludes the ring completely
    fillA = aInner; // only the *inside*, not the border ring
    borderA = saturate(aOuter - aInner); // just the ring
}

#endif
