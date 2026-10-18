using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using KateVR;

/// <summary>
/// Sculpts Kate's hazmat arm in code: a heavy, bunched, wrinkled sleeve (upper arm, elbow, forearm
/// with an elastic wrist band), and an organic gloved left hand: a contoured padded palm with
/// knuckle mounds, tapered three-jointed fingers, a thumb with webbing, and a fitted rubber cuff.
/// All meshes are saved into one asset (Assets/Meshes/KateArm.asset) and rebuilt on every
/// "Build Greybox Street". Vertex colours carry the sculpt for the shader: R = ridge, G = crease.
/// </summary>
public static class SuitBuilder
{
    const string MeshPath = "Assets/Meshes/KateArm.asset";
    const float Side = 1f;        // left hand, palm facing away: thumb on the hand's +X side
    const float HandScale = 1.15f; // bulky glove

    delegate Vector3 Shape(float y, float theta, out Color tag);

    static readonly List<Mesh> _meshes = new List<Mesh>();

    public static KateArm Build(Transform body)
    {
        _meshes.Clear();
        var fabric = Mat("Suit_Fabric", new Color(0.20f, 0.19f, 0.14f), dust: 0.32f, bump: 0.35f, f1: 140f, f2: 520f, spec: 0.03f, specPow: 10f, rim: 1.5f);
        var leather = Mat("Suit_Leather", new Color(0.17f, 0.125f, 0.085f), dust: 0.18f, bump: 0.22f, f1: 380f, f2: 950f, spec: 0.22f, specPow: 18f, rim: 1.7f);
        var rubber = Mat("Suit_Rubber", new Color(0.10f, 0.085f, 0.068f), dust: 0.15f, bump: 0.12f, f1: 300f, f2: 900f, spec: 0.16f, specPow: 24f, rim: 1.3f);

        var root = new GameObject("Kate's Arm");
        var arm = root.AddComponent<KateArm>();
        arm.body = body;
        arm.thumbSide = Side;

        // ---- Sleeve ----
        arm.upperArm = Piece(root.transform, "Upper Sleeve", UpperSleeve(arm.upperLength), fabric);
        arm.elbow = Piece(root.transform, "Elbow", ElbowMesh(), fabric);
        arm.forearm = Piece(root.transform, "Forearm Sleeve", ForeSleeve(arm.foreLength), fabric);

        // ---- Glove ---- (local +Z = palm facing, +Y = fingers)
        var hand = new GameObject("Kate's Hand").transform;
        hand.SetParent(root.transform, false);
        hand.localScale = Vector3.one * HandScale;
        arm.hand = hand;
        Piece(hand, "Palm", PalmMesh(), leather);
        var web = Piece(hand, "Thumb Web", Ellipsoid("Thumb Web", 0.016f, 0.026f, 0.0085f, webby: true), leather);
        web.localPosition = new Vector3(Side * 0.046f, 0.004f, 0.003f);
        web.localRotation = Quaternion.Euler(0f, 0f, -Side * 24f);

        arm.cuff = Piece(hand, "Glove Cuff", CuffMesh(), rubber);
        arm.cuff.localPosition = arm.wristLocal;

        // Fingers: index (thumb side) .. little.
        float[] fx = { Side * 0.0345f, Side * 0.0115f, -Side * 0.0115f, -Side * 0.034f };
        float[] fy = { 0.050f, 0.053f, 0.051f, 0.046f };
        float[] baseSplay = { 5f, 1f, -2f, -7f };
        float[] radius = { 0.0104f, 0.0108f, 0.0102f, 0.0090f };
        float[,] len =
        {
            { 0.040f, 0.025f, 0.020f },
            { 0.044f, 0.028f, 0.022f },
            { 0.042f, 0.026f, 0.021f },
            { 0.032f, 0.020f, 0.018f },
        };
        arm.fingers = new Transform[12];
        string[] names = { "Index", "Middle", "Ring", "Little" };
        for (int f = 0; f < 4; f++)
        {
            Transform parent = hand;
            Vector3 at = new Vector3(fx[f], fy[f], 0.004f);
            float r = radius[f];
            for (int j = 0; j < 3; j++)
            {
                float rEnd = j == 2 ? r * 0.86f : r * 0.93f;
                var joint = new GameObject($"{names[f]} {j + 1}").transform;
                joint.SetParent(parent, false);
                joint.localPosition = at;
                joint.localRotation = j == 0 ? Quaternion.Euler(0f, 0f, -Side * baseSplay[f]) : Quaternion.identity;
                Piece(joint, "Glove", FingerSegment($"{names[f]} {j + 1}", len[f, j], r, rEnd, knuckle: j == 0 ? 1f : 0.6f, tip: j == 2), leather);
                arm.fingers[f * 3 + j] = joint;
                parent = joint;
                at = new Vector3(0f, len[f, j], 0f);
                r = rEnd;
            }
        }

        // Thumb: from the heel of the hand, out to the side and slightly toward the palm.
        arm.thumb = new Transform[3];
        float[] tl = { 0.030f, 0.029f, 0.024f };
        float[] tr = { 0.0135f, 0.0118f, 0.0106f, 0.0088f };
        Transform tp = hand;
        Vector3 tAt = new Vector3(Side * 0.030f, -0.028f, 0.007f);
        for (int j = 0; j < 3; j++)
        {
            var joint = new GameObject($"Thumb {j + 1}").transform;
            joint.SetParent(tp, false);
            joint.localPosition = tAt;
            joint.localRotation = j == 0 ? Quaternion.Euler(16f, -Side * 30f, -Side * 44f) : Quaternion.Euler(0f, 0f, Side * 6f);
            Piece(joint, "Glove", FingerSegment($"Thumb {j + 1}", tl[j], tr[j], tr[j + 1], knuckle: j == 1 ? 0.8f : 0.3f, tip: j == 2), leather);
            arm.thumb[j] = joint;
            tp = joint;
            tAt = new Vector3(0f, tl[j], 0f);
        }

        SaveMeshes();
        return arm;
    }

    // ============================== Materials ==============================

    static Material Mat(string name, Color baseCol, float dust, float bump, float f1, float f2, float spec, float specPow, float rim)
    {
        var shader = Shader.Find("KateVR/SuitFabric");
        System.IO.Directory.CreateDirectory(WorldKit.MaterialFolder);
        string path = $"{WorldKit.MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.SetColor("_BaseColor", baseCol);
        mat.SetColor("_ShadowColor", baseCol * 0.18f);
        mat.SetColor("_DustColor", new Color(0.52f, 0.40f, 0.25f));
        mat.SetColor("_RimColor", new Color(1.0f, 0.72f, 0.36f));
        mat.SetFloat("_DustAmount", dust);
        mat.SetFloat("_BumpStrength", bump);
        mat.SetFloat("_BumpFreq", f1);
        mat.SetFloat("_BumpFreq2", f2);
        mat.SetFloat("_Spec", spec);
        mat.SetFloat("_SpecPower", specPow);
        mat.SetFloat("_RimStrength", rim);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Transform Piece(Transform parent, string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go.transform;
    }

    static void SaveMeshes()
    {
        System.IO.Directory.CreateDirectory("Assets/Meshes");
        AssetDatabase.DeleteAsset(MeshPath);
        AssetDatabase.CreateAsset(_meshes[0], MeshPath);
        for (int i = 1; i < _meshes.Count; i++) AssetDatabase.AddObjectToAsset(_meshes[i], MeshPath);
        AssetDatabase.SaveAssets();
    }

    // ============================== Sleeve ==============================

    struct Fold { public float y, amp, width, tilt, phase, lobePhase; public int lobes, tiltFreq; }

    // Folds along a tube: spacing tightens and depth grows where the fabric bunches.
    static List<Fold> MakeFolds(System.Random rng, float from, float to, System.Func<float, float> bunch)
    {
        var folds = new List<Fold>();
        float y = from;
        while (y < to)
        {
            float b = bunch(y);
            folds.Add(new Fold
            {
                y = y,
                amp = Mathf.Lerp(0.0018f, 0.0062f, b) * R(rng, 0.6f, 1.25f),
                width = Mathf.Lerp(0.0075f, 0.0042f, b) * R(rng, 0.75f, 1.3f),
                tilt = R(rng, 0.003f, 0.011f),
                tiltFreq = rng.Next(1, 3),
                phase = R(rng, 0f, 6.283f),
                lobes = rng.Next(1, 4),
                lobePhase = R(rng, 0f, 6.283f),
            });
            y += Mathf.Lerp(0.036f, 0.0115f, b) * R(rng, 0.7f, 1.3f);
        }
        return folds;
    }

    // Sum of the folds at a point on the sleeve, plus noise; returns the radial push and the sculpt tags.
    static float FoldDisplace(List<Fold> folds, float y, float th, float sag, float seed, out float ridge, out float crease)
    {
        float d = 0f; ridge = 0f; crease = 0f;
        float under = Mathf.Max(0f, -Mathf.Sin(th)); // mesh -Z faces the elbow's outside / underneath
        foreach (var f in folds)
        {
            float u = (y - (f.y + f.tilt * Mathf.Sin(th * f.tiltFreq + f.phase))) / f.width;
            if (u < -3.5f || u > 4.5f) continue;
            float lobe = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(th * f.lobes + f.lobePhase), 1.5f);
            float amp = f.amp * (0.3f + 0.7f * lobe) * (1f + sag * under);
            float bump = Mathf.Exp(-u * u);
            float cut = Mathf.Exp(-Mathf.Pow((u - 1.55f) / 0.38f, 2f)); // sharp crease just past each fold
            d += amp * (bump - 0.55f * cut);
            ridge = Mathf.Max(ridge, bump * lobe);
            crease = Mathf.Max(crease, cut * (0.4f + 0.6f * lobe));
        }
        // Lumpy heavy cloth + fine wrinkling (sampled around a circle so there's no seam).
        float cx = Mathf.Cos(th), cz = Mathf.Sin(th);
        d += 0.0026f * (Mathf.PerlinNoise(seed + cx * 1.4f + y * 9f, seed * 0.7f + cz * 1.4f + y * 6f) - 0.5f) * 2f;
        d += 0.0007f * (Mathf.PerlinNoise(seed + 40f + cx * 5f + y * 70f, cz * 5f + y * 55f) - 0.5f) * 2f;
        return d;
    }

    // Forearm: from just inside the elbow (y < 0) to the elastic wrist band at y = length.
    static Mesh ForeSleeve(float length)
    {
        var rng = new System.Random(31);
        System.Func<float, float> bunch = y => Smooth(Mathf.InverseLerp(length * 0.45f, length * 0.92f, y));
        var folds = MakeFolds(rng, 0.0f, length - 0.02f, bunch);
        const float band = 0.017f;

        var profile = new List<float>();
        for (float y = -0.035f; y < length; y += 0.0024f) profile.Add(y);
        profile.Add(length);
        int outerRings = profile.Count;

        return Lathe("Forearm Sleeve", profile, 32, (float y, float th, out Color tag) =>
        {
            float t = Mathf.InverseLerp(0f, length, y);
            float r = Mathf.Lerp(0.064f, 0.056f, t) + 0.0065f * Bell(t, 0.78f, 0.16f); // puffs where it bunches
            float ridge = 0f, crease = 0f;
            if (y > length - band)
            {
                // Gathered elastic band: tighter, with little vertical pleats.
                float k = Smooth(Mathf.InverseLerp(length - band, length - band + 0.006f, y));
                r = Mathf.Lerp(r, 0.052f, k) + k * 0.0011f * Mathf.Sin(th * 22f);
                ridge = k * Mathf.Max(0f, Mathf.Sin(th * 22f)) * 0.6f;
                crease = k * Mathf.Max(0f, -Mathf.Sin(th * 22f)) * 0.5f;
                // Rounded lip at the very end.
                float lip = Mathf.InverseLerp(length - 0.004f, length, y);
                r -= 0.004f * lip * lip;
            }
            else
            {
                float fade = 1f - Smooth(Mathf.InverseLerp(length - band - 0.008f, length - band, y));
                r += fade * FoldDisplace(folds, y, th, 0.6f, 3.1f, out ridge, out crease);
                ridge *= fade; crease *= fade;
                // Joint with the band reads as a deep crease.
                crease = Mathf.Max(crease, Bell(y, length - band, 0.0035f) * 0.8f);
            }
            tag = new Color(ridge, crease, 0f, 1f);
            return new Vector3(Mathf.Cos(th) * r, y, Mathf.Sin(th) * r);
        }, capStart: true, lipEnd: new[] { 0.86f, 0.84f }, lipDepth: 0.014f);
    }

    // Upper arm: shoulder (y = 0, out of view) to just past the elbow; creases gather at the crook.
    static Mesh UpperSleeve(float length)
    {
        var rng = new System.Random(57);
        System.Func<float, float> bunch = y => 0.25f + 0.75f * Smooth(Mathf.InverseLerp(length * 0.6f, length, y));
        var folds = MakeFolds(rng, 0.02f, length + 0.02f, bunch);
        var profile = new List<float>();
        for (float y = -0.03f; y < length + 0.03f; y += 0.004f) profile.Add(y);
        return Lathe("Upper Sleeve", profile, 28, (float y, float th, out Color tag) =>
        {
            float t = Mathf.InverseLerp(0f, length, y);
            float r = Mathf.Lerp(0.070f, 0.063f, t);
            // The crook of the elbow (mesh +Z side) folds hardest.
            float crook = Mathf.Max(0f, Mathf.Sin(th));
            float d = FoldDisplace(folds, y, th, 0.3f, 8.4f, out float ridge, out float crease);
            r += d * (0.7f + 0.8f * crook * Smooth(t));
            tag = new Color(ridge, crease, 0f, 1f);
            return new Vector3(Mathf.Cos(th) * r, y, Mathf.Sin(th) * r);
        }, capStart: true, capEnd: true);
    }

    static Mesh ElbowMesh()
    {
        var profile = new List<float>();
        const float r0 = 0.064f;
        for (int k = 1; k < 18; k++) profile.Add(-r0 * Mathf.Cos(Mathf.PI * k / 18f));
        return Lathe("Elbow", profile, 24, (float y, float th, out Color tag) =>
        {
            float r = Mathf.Sqrt(Mathf.Max(0f, r0 * r0 - y * y));
            float n = Mathf.PerlinNoise(2f + Mathf.Cos(th) * 2f + y * 30f, 5f + Mathf.Sin(th) * 2f) - 0.5f;
            r *= 1f + n * 0.12f;
            tag = new Color(Mathf.Max(0f, n) * 1.5f, Mathf.Max(0f, -n) * 1.5f, 0f, 1f);
            return new Vector3(Mathf.Cos(th) * r, y, Mathf.Sin(th) * r);
        }, capStart: true, capEnd: true);
    }

    // ============================== Glove ==============================

    // Palm: a padded, rounded slab. Back (-Z): knuckle mounds, tendon ridges, a stitched seam.
    // Front (+Z): a shallow cup with padding at the finger roots, the heel and the thumb mound.
    static Mesh PalmMesh()
    {
        const float X = 0.047f, Z = 0.0175f, top = 0.040f, topR = 0.019f, bottom = -0.056f;
        float[] kx = { Side * 0.0345f, Side * 0.0115f, -Side * 0.0115f, -Side * 0.034f };
        float[] ky = { 0.045f, 0.048f, 0.046f, 0.041f };
        var profile = new List<float>();
        for (float y = bottom; y < top; y += 0.0025f) profile.Add(y);
        for (int k = 0; k < 9; k++) profile.Add(top + topR * Mathf.Sin(0.5f * Mathf.PI * k / 9f));

        return Lathe("Palm", profile, 44, (float y, float th, out Color tag) =>
        {
            float sTop = y > top ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((y - top) / topR, 2f))) : 1f;
            float w = Smooth(Mathf.InverseLerp(bottom, -0.012f, y));
            float rx = X * Mathf.Lerp(0.80f, 1f, w) * (0.72f + 0.28f * sTop);
            float rz = Z * Mathf.Lerp(0.78f, 1f, w) * sTop;
            float c = Mathf.Cos(th), s = Mathf.Sin(th);
            float x = rx * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.55f);
            float z = rz * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.55f);
            float face = Mathf.Abs(s); // 1 on the flat faces, 0 at the sides
            float ridge = 0f, crease = 0f;
            if (z < 0f)
            {
                float push = 0f;
                for (int i = 0; i < 4; i++)
                {
                    float k = Mathf.Exp(-((x - kx[i]) * (x - kx[i]) + (y - ky[i]) * (y - ky[i])) / (0.0095f * 0.0095f));
                    push += 0.0042f * k;
                    ridge = Mathf.Max(ridge, k);
                    float tendon = Mathf.Exp(-Mathf.Pow((x - kx[i] * 0.75f) / 0.0045f, 2f)) * Bell(y, 0.012f, 0.022f);
                    push += 0.0011f * tendon;
                    ridge = Mathf.Max(ridge, tendon * 0.5f);
                }
                float seam = Bell(y, 0.012f, 0.0012f);           // stitched seam across the back
                push -= 0.0008f * seam;
                crease = Mathf.Max(crease, seam);
                ridge = Mathf.Max(ridge, (Bell(y, 0.0095f, 0.0012f) + Bell(y, 0.0145f, 0.0012f)) * 0.4f);
                z -= push * face;
            }
            else
            {
                float cup = Mathf.Exp(-(x * x + (y - 0.004f) * (y - 0.004f)) / (0.024f * 0.024f));
                float roots = Bell(y, 0.037f, 0.006f);                 // pads under the fingers
                float thenar = Mathf.Exp(-(Mathf.Pow(x - Side * 0.024f, 2f) + Mathf.Pow(y + 0.026f, 2f)) / (0.017f * 0.017f));
                float hypo = Mathf.Exp(-(Mathf.Pow(x + Side * 0.027f, 2f) + Mathf.Pow(y + 0.022f, 2f)) / (0.015f * 0.015f));
                float lifeLine = Mathf.Exp(-Mathf.Pow((Vector2.Distance(new Vector2(x, y), new Vector2(Side * 0.045f, -0.04f)) - 0.042f) / 0.0018f, 2f));
                z += (-0.0042f * cup + 0.0018f * roots + 0.0042f * thenar + 0.0024f * hypo - 0.0009f * lifeLine) * face;
                ridge = Mathf.Max(roots, Mathf.Max(thenar, hypo)) * 0.6f;
                crease = Mathf.Max(cup * 0.45f, lifeLine);
            }
            tag = new Color(ridge, crease, 0f, 1f);
            return new Vector3(x, y, z);
        }, capStart: true, capEnd: true);
    }

    // One finger bone: a tapered capsule (slightly flattened front-to-back), with a knuckle mound on
    // the back at its root, a crease across the palm side, and a softly flattened pad at the tip.
    static Mesh FingerSegment(string name, float length, float r0, float r1, float knuckle, bool tip)
    {
        var profile = new List<float>();
        const int cap = 7;
        for (int k = 1; k <= cap; k++) profile.Add(-r0 * Mathf.Cos(0.5f * Mathf.PI * k / cap));
        for (int k = 1; k < 8; k++) profile.Add(length * k / 8f);
        for (int k = 0; k < cap; k++) profile.Add(length + r1 * Mathf.Sin(0.5f * Mathf.PI * k / cap));

        return Lathe(name, profile, 18, (float y, float th, out Color tag) =>
        {
            float r;
            if (y < 0f) r = Mathf.Sqrt(Mathf.Max(0f, r0 * r0 - y * y));
            else if (y > length) r = Mathf.Sqrt(Mathf.Max(0f, r1 * r1 - (y - length) * (y - length)));
            else r = Mathf.Lerp(r0, r1, Smooth(y / length));
            float s = Mathf.Sin(th);
            float back = Mathf.Max(0f, -s), front = Mathf.Max(0f, s);
            float bump = Bell(y, r0 * 0.15f, r0 * 0.75f) * back * back * knuckle;
            float crease = Bell(y, r0 * 0.25f, 0.0018f) * front;
            float wrinkle = Bell(y, r0 * 0.1f, r0 * 1.1f) * back * (0.5f + 0.5f * Mathf.Sin(y * 2f * Mathf.PI / 0.0032f));
            r += 0.0024f * bump - 0.0011f * crease + 0.00035f * wrinkle;
            float zs = 0.86f;
            if (tip && y > length * 0.6f) zs *= Mathf.Lerp(1f, 0.82f, Mathf.InverseLerp(length * 0.6f, length + r1, y) * front);
            tag = new Color(Mathf.Clamp01(bump * 1.2f + wrinkle * 0.3f), Mathf.Clamp01(crease), 0f, 1f);
            return new Vector3(Mathf.Cos(th) * r, y, s * r * zs);
        }, capStart: true, capEnd: true);
    }

    // The fitted rubber cuff: emerges from the sleeve's band and seals around the wrist, with a
    // raised strap, a little ribbing, and it narrows to meet the heel of the palm.
    static Mesh CuffMesh()
    {
        var profile = new List<float>();
        for (float y = -0.016f; y <= 0.026f; y += 0.0016f) profile.Add(y);
        return Lathe("Glove Cuff", profile, 40, (float y, float th, out Color tag) =>
        {
            float t = Mathf.InverseLerp(-0.016f, 0.026f, y);
            float rx = Mathf.Lerp(0.0405f, 0.0385f, t);
            float rz = Mathf.Lerp(0.033f, 0.0215f, Smooth(t));
            float strap = Smooth(Mathf.InverseLerp(0.0f, 0.002f, y)) * (1f - Smooth(Mathf.InverseLerp(0.009f, 0.011f, y)));
            float ribs = (1f - strap) * (y < 0f ? 1f : 0f) * Mathf.Sin(y * 2f * Mathf.PI / 0.0034f);
            float add = 0.0022f * strap + 0.00045f * ribs;
            float c = Mathf.Cos(th), s = Mathf.Sin(th);
            float x = (rx + add) * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.8f);
            float z = (rz + add) * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.8f);
            float edge = Bell(y, 0.0f, 0.0009f) + Bell(y, 0.011f, 0.0009f);
            tag = new Color(Mathf.Clamp01(strap * 0.8f + Mathf.Max(0f, ribs) * 0.4f), Mathf.Clamp01(edge + Mathf.Max(0f, -ribs) * 0.3f), 0f, 1f);
            return new Vector3(x, y, z);
        }, capStart: true, capEnd: true);
    }

    // A soft flattened blob (the webbing between thumb and index, slightly creased).
    static Mesh Ellipsoid(string name, float rx, float ry, float rz, bool webby)
    {
        var profile = new List<float>();
        for (int k = 1; k < 14; k++) profile.Add(-ry * Mathf.Cos(Mathf.PI * k / 14f));
        return Lathe(name, profile, 20, (float y, float th, out Color tag) =>
        {
            float k = Mathf.Sqrt(Mathf.Max(0f, 1f - (y / ry) * (y / ry)));
            float wr = webby ? 1f + 0.06f * Mathf.Sin(y / ry * 9f) : 1f;
            tag = new Color(webby ? Mathf.Max(0f, Mathf.Sin(y / ry * 9f)) * 0.4f : 0f, 0.15f, 0f, 1f);
            return new Vector3(Mathf.Cos(th) * rx * k * wr, y, Mathf.Sin(th) * rz * k);
        }, capStart: true, capEnd: true);
    }

    // ============================== Mesh helpers ==============================

    // Revolves rings around local +Y. Each profile entry is a ring height; the shape function places
    // every vertex (so rings can be any cross-section). No duplicated seam, so normals stay smooth.
    // capStart/capEnd close the ends; lipEnd turns the end back inside (an open sleeve with a lining).
    static Mesh Lathe(string name, List<float> profile, int sides, Shape shape,
                      bool capStart = false, bool capEnd = false, float[] lipEnd = null, float lipDepth = 0.01f)
    {
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();
        int rings = profile.Count;
        Vector3[] last = new Vector3[sides];
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < sides; j++)
            {
                float th = j * 2f * Mathf.PI / sides;
                verts.Add(shape(profile[i], th, out Color c));
                cols.Add(c);
            }

        if (lipEnd != null)
        {
            // Turn the open end back inward: a rounded lip, then the lining running back inside.
            int lastRing = (rings - 1) * sides;
            for (int j = 0; j < sides; j++) last[j] = verts[lastRing + j];
            for (int l = 0; l < lipEnd.Length; l++)
            {
                float back = l == 0 ? 0.0015f : lipDepth;
                for (int j = 0; j < sides; j++)
                {
                    Vector3 p = last[j];
                    verts.Add(new Vector3(p.x * lipEnd[l], p.y - back, p.z * lipEnd[l]));
                    cols.Add(new Color(0f, l == 0 ? 0.5f : 1f, 0f, 1f));
                }
            }
            rings += lipEnd.Length;
        }

        for (int i = 0; i < rings - 1; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * sides + j, b = i * sides + (j + 1) % sides;
                int c = (i + 1) * sides + j, d = (i + 1) * sides + (j + 1) % sides;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }

        if (capStart) Cap(verts, cols, tris, 0, sides, profile[0] - 0.0004f, true);
        if (capEnd && lipEnd == null) Cap(verts, cols, tris, (rings - 1) * sides, sides, profile[profile.Count - 1] + 0.0004f, false);
        if (lipEnd != null) Cap(verts, cols, tris, (rings - 1) * sides, sides, verts[(rings - 1) * sides].y, true); // deep inside, faces out

        var mesh = new Mesh { name = name };
        if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _meshes.Add(mesh);
        return mesh;
    }

    static void Cap(List<Vector3> verts, List<Color> cols, List<int> tris, int ringStart, int sides, float y, bool bottom)
    {
        Vector3 centre = Vector3.zero;
        for (int j = 0; j < sides; j++) centre += verts[ringStart + j];
        centre /= sides;
        centre.y = Mathf.Lerp(centre.y, y, 0.5f);
        int ci = verts.Count;
        verts.Add(centre);
        cols.Add(new Color(0f, 0.2f, 0f, 1f));
        for (int j = 0; j < sides; j++)
        {
            int a = ringStart + j, b = ringStart + (j + 1) % sides;
            if (bottom) { tris.Add(ci); tris.Add(a); tris.Add(b); }
            else { tris.Add(ci); tris.Add(b); tris.Add(a); }
        }
    }

    static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    static float Bell(float x, float centre, float width) { float u = (x - centre) / width; return Mathf.Exp(-u * u); }
    static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
}
