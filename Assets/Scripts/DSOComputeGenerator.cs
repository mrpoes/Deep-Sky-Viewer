using UnityEngine;
using UnityEngine.Rendering;
using System;

public static class DSOComputeGenerator
{
    static ComputeShader _shader;

    static ComputeShader Shader
    {
        get
        {
            if (_shader == null)
                _shader = Resources.Load<ComputeShader>("DSOCompute");
            return _shader;
        }
    }

    public static void Generate(DeepSkyObject.DSOType type, int size, Action<Texture2D> callback)
    {
        var cs = Shader;
        if (cs == null)
        {
            Debug.LogError("DSOCompute.compute not found in Resources folder");
            return;
        }

        // create output texture
        RenderTexture rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;
        rt.Create();

        float seed = UnityEngine.Random.Range(0f, 100f);

        int kernel;

        switch (type)
        {
            case DeepSkyObject.DSOType.Nebula:
                kernel = cs.FindKernel("GenerateNebula");
                SetNebulaParams(cs, seed);
                break;
            case DeepSkyObject.DSOType.Galaxy:
                kernel = cs.FindKernel("GenerateGalaxy");
                SetGalaxyParams(cs, seed);
                break;
            default:
                kernel = cs.FindKernel("GenerateCluster");
                break;
        }

        cs.SetTexture(kernel, "Result", rt);
        cs.SetInt("Size", size);
        cs.SetFloat("Seed", seed);

        int groups = Mathf.CeilToInt(size / 8f);
        cs.Dispatch(kernel, groups, groups, 1);

        // async readback — non-blocking
        AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error");
                rt.Release();
                return;
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixelData(request.GetData<byte>(), 0);
            tex.Apply();
            rt.Release();
            callback(tex);
        });
    }

    static void SetNebulaParams(ComputeShader cs, float seed)
    {
        // save and restore random state
        UnityEngine.Random.State saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState((int)(seed * 1000));

        Vector2 lobeOffset = new Vector2(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-0.2f, 0.2f));
        Vector2 starClusterPos = lobeOffset + new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f));

        int numStars = UnityEngine.Random.Range(3, 8);
        Vector4[] starPositions = new Vector4[8];
        float[] starSizes = new float[8];
        for (int s = 0; s < numStars; s++)
        {
            starPositions[s] = starClusterPos + new Vector2(UnityEngine.Random.Range(-0.08f, 0.08f), UnityEngine.Random.Range(-0.08f, 0.08f));
            starSizes[s] = UnityEngine.Random.Range(0.01f, 0.03f);
        }

        int numPillars = UnityEngine.Random.Range(2, 5);
        float[] pillarAngles = new float[5];
        float[] pillarLengths = new float[5];
        float[] pillarWidths = new float[5];
        for (int p = 0; p < numPillars; p++)
        {
            pillarAngles[p] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            pillarLengths[p] = UnityEngine.Random.Range(0.3f, 0.7f);
            pillarWidths[p] = UnityEngine.Random.Range(0.04f, 0.10f);
        }

        float oiiiStrength = UnityEngine.Random.Range(0.3f, 0.8f);
        float nebulaScale = UnityEngine.Random.Range(0.7f, 1.3f);

        UnityEngine.Random.state = saved;

        cs.SetVector("LobeOffset", lobeOffset);
        cs.SetVector("StarClusterPos", starClusterPos);
        cs.SetInt("NumStars", numStars);
        cs.SetVectorArray("StarPositions", starPositions);
        cs.SetFloats("StarSizes", starSizes);
        cs.SetInt("NumPillars", numPillars);
        cs.SetFloats("PillarAngles", pillarAngles);
        cs.SetFloats("PillarLengths", pillarLengths);
        cs.SetFloats("PillarWidths", pillarWidths);
        cs.SetFloat("OiiiStrength", oiiiStrength);
        cs.SetFloat("NebulaScale", nebulaScale);
    }

    static void SetGalaxyParams(ComputeShader cs, float seed)
    {
        UnityEngine.Random.State saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState((int)(seed * 1000));

        int numArms = UnityEngine.Random.Range(2, 5);
        float spiralTightness = UnityEngine.Random.Range(3.5f, 6.5f);
        float[] armBrightnesses = new float[4];
        for (int a = 0; a < numArms && a < 4; a++)
            armBrightnesses[a] = UnityEngine.Random.Range(0.7f, 1.0f);
        float barLength = UnityEngine.Random.Range(0.15f, 0.35f);
        float barWidth = UnityEngine.Random.Range(0.04f, 0.08f);

        UnityEngine.Random.state = saved;

        cs.SetInt("NumArms", numArms);
        cs.SetFloat("SpiralTightness", spiralTightness);
        cs.SetFloats("ArmBrightnesses", armBrightnesses);
        cs.SetFloat("BarLength", barLength);
        cs.SetFloat("BarWidth", barWidth);
    }
}