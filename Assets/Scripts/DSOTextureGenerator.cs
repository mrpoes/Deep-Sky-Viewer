using UnityEngine;
using Random = UnityEngine.Random;

public static class DSOTextureGenerator
{
    // =====================================================================
    // PUBLIC ENTRY POINT
    // =====================================================================

    public static Texture2D Generate(DeepSkyObject.DSOType type)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float seed = Random.Range(0f, 100f);

        switch (type)
        {
            case DeepSkyObject.DSOType.Nebula:
                GenerateNebula(pixels, size, seed);
                break;
            case DeepSkyObject.DSOType.Galaxy:
                GenerateGalaxy(pixels, size, seed);
                break;
            case DeepSkyObject.DSOType.OpenCluster:
                GenerateOpenCluster(pixels, size, seed);
                break;
            case DeepSkyObject.DSOType.GlobularCluster:
                GenerateGlobularCluster(pixels, size, seed);
                break;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // =====================================================================
    // NEBULA — Emission nebula (HII region)
    // Layered: base cloud, Ha emission filaments, OIII reflection,
    //          dust pillars, outer wisps, embedded stars
    // =====================================================================

    static void GenerateNebula(Color[] pixels, int size, float seed)
    {
        // --- SEEDED PARAMS (generated once, outside pixel loop) ---
        Random.State saved = Random.state;
        Random.InitState((int)(seed * 1000));

        Vector2 lobeOffset = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
        Vector2 starClusterPos = lobeOffset + new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));

        int numStars = Random.Range(3, 8);
        Vector2[] starPos = new Vector2[numStars];
        float[] starSizes = new float[numStars];
        for (int s = 0; s < numStars; s++)
        {
            starPos[s] = starClusterPos + new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f));
            starSizes[s] = Random.Range(0.01f, 0.03f);
        }

        int numPillars = Random.Range(2, 5);
        float[] pillarAngles = new float[numPillars];
        float[] pillarLengths = new float[numPillars];
        float[] pillarWidths = new float[numPillars];
        for (int p = 0; p < numPillars; p++)
        {
            pillarAngles[p] = Random.Range(0f, Mathf.PI * 2f);
            pillarLengths[p] = Random.Range(0.3f, 0.7f);
            pillarWidths[p] = Random.Range(0.04f, 0.10f);
        }

        float oiiiStrength = Random.Range(0.3f, 0.8f);
        float nebulaScale = Random.Range(0.7f, 1.3f);

        Random.state = saved;

        // --- PIXEL LOOP ---
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (size * 0.5f)) - 1f;
                float ny = (y / (size * 0.5f)) - 1f;

                // coordinate setup + domain warps
                float unx = nx / nebulaScale;
                float uny = ny / nebulaScale;

                float warpX = FBM(unx * 2f + seed, uny * 2f + seed, 4) - 0.5f;
                float warpY = FBM(unx * 2f + seed + 5.2f, uny * 2f + seed + 5.2f, 4) - 0.5f;
                float wx = unx + warpX * 0.35f;
                float wy = uny + warpY * 0.35f;

                float warp2X = FBM(wx * 3f + seed + 1.7f, wy * 3f + seed + 1.7f, 3) - 0.5f;
                float warp2Y = FBM(wx * 3f + seed + 8.3f, wy * 3f + seed + 8.3f, 3) - 0.5f;
                wx += warp2X * 0.15f;
                wy += warp2Y * 0.15f;

                float lx = wx - lobeOffset.x;
                float ly = wy - lobeOffset.y;
                float lobeDist = Mathf.Sqrt(lx * lx + ly * ly);

                // layer 1: base cloud shape
                float cloudFBM = FBM(wx * 2.5f + seed, wy * 2.5f + seed, 6);
                float cloudShape = Mathf.Clamp01((1f - Mathf.Pow(lobeDist * 1.4f, 1.2f)) * cloudFBM * 1.8f);

                float lx2 = wx - lobeOffset.x * 0.3f + 0.2f;
                float ly2 = wy - lobeOffset.y * 0.3f - 0.15f;
                float lobeDist2 = Mathf.Sqrt(lx2 * lx2 + ly2 * ly2);
                float cloudShape2 = Mathf.Clamp01((1f - Mathf.Pow(lobeDist2 * 2.2f, 1.4f))
                                   * FBM(wx * 3f + seed + 3f, wy * 3f + seed + 3f, 4) * 1.5f);
                float totalCloud = Mathf.Clamp01(cloudShape * 0.75f + cloudShape2 * 0.4f);

                // layer 2: Ha emission — ridged FBM for filaments
                float ridgeA = 1f - Mathf.Abs(Mathf.PerlinNoise(wx * 4f + seed, wy * 4f + seed) * 2f - 1f);
                float ridgeB = 1f - Mathf.Abs(Mathf.PerlinNoise(wx * 8f + seed + 2f, wy * 8f + seed + 2f) * 2f - 1f);
                float ridgeC = 1f - Mathf.Abs(Mathf.PerlinNoise(wx * 16f + seed + 4f, wy * 16f + seed + 4f) * 2f - 1f);
                float ridgedFBM = (ridgeA + ridgeB * 0.5f + ridgeC * 0.25f) / 1.75f;
                float haEmission = Mathf.Clamp01(totalCloud * ridgedFBM * 1.5f);

                // layer 3: OIII reflection near star cluster
                float sx = wx - starClusterPos.x;
                float sy = wy - starClusterPos.y;
                float sDist = Mathf.Sqrt(sx * sx + sy * sy);
                float oiiiFalloff = Mathf.Clamp01(1f - Mathf.Pow(sDist * 3f, 1.5f));
                float oiiiRegion = Mathf.Clamp01(oiiiFalloff
                                   * FBM(wx * 5f + seed + 10f, wy * 5f + seed + 10f, 3)
                                   * oiiiStrength * totalCloud);

                // layer 4: dust pillars
                float dustMask = 0f;
                float pillarRimGlow = 0f;
                for (int p = 0; p < numPillars; p++)
                {
                    float edgeX = lobeOffset.x + Mathf.Cos(pillarAngles[p]) * 0.7f;
                    float edgeY = lobeOffset.y + Mathf.Sin(pillarAngles[p]) * 0.7f;
                    float dirX = starClusterPos.x - edgeX;
                    float dirY = starClusterPos.y - edgeY;
                    float dirLen = Mathf.Sqrt(dirX * dirX + dirY * dirY) + 0.0001f;
                    dirX /= dirLen;
                    dirY /= dirLen;

                    float px2 = wx - edgeX;
                    float py2 = wy - edgeY;
                    float along = px2 * dirX + py2 * dirY;
                    float perp = px2 * (-dirY) + py2 * dirX;

                    float t2 = Mathf.Clamp01(along / pillarLengths[p]);
                    float taper = pillarWidths[p] * (1f - t2 * 0.6f)
                                  * (0.7f + 0.6f * FBM(wx * 8f + seed + p * 3f, wy * 8f + seed + p * 3f, 3));

                    if (along > 0f && along < pillarLengths[p])
                    {
                        float pillarDist = Mathf.Abs(perp) / Mathf.Max(taper, 0.0001f);
                        dustMask = Mathf.Max(dustMask, Mathf.Clamp01(1f - pillarDist));

                        float rim = Mathf.Clamp01(1f - Mathf.Abs(perp - taper * 0.8f) / Mathf.Max(taper * 0.3f, 0.0001f));
                        pillarRimGlow = Mathf.Max(pillarRimGlow, rim * Mathf.Clamp01(1f - t2) * 0.6f);
                    }
                }

                // layer 5: outer wisps via curl noise
                float curlX = FBM(wx * 1.5f + seed + 15f, wy * 1.5f + seed + 15f, 4)
                             - FBM(wx * 1.5f + seed + 15f, wy * 1.5f + seed + 20f, 4);
                float curlY = FBM(wx * 1.5f + seed + 20f, wy * 1.5f + seed + 15f, 4)
                             - FBM(wx * 1.5f + seed + 25f, wy * 1.5f + seed + 20f, 4);
                float wisps = Mathf.Clamp01(Mathf.Sqrt(curlX * curlX + curlY * curlY)
                             * Mathf.Clamp01(1f - Mathf.Pow(lobeDist * 0.9f, 2f)) * 0.3f);

                // layer 6: embedded stars
                float starBloom = 0f;
                for (int s = 0; s < numStars; s++)
                {
                    float stx = nx - starPos[s].x;
                    float sty = ny - starPos[s].y;
                    float stD = Mathf.Sqrt(stx * stx + sty * sty);
                    float core = Mathf.Clamp01(1f - stD / starSizes[s]);
                    float halo = Mathf.Clamp01(1f - stD / (starSizes[s] * 4f)) * 0.3f;
                    starBloom = Mathf.Max(starBloom, core * core + halo);
                }

                // alpha via optical depth
                float density = haEmission * 2.5f + wisps * 0.5f + starBloom * 0.5f;
                float edgeNoise = FBM(wx * 1.2f + seed + 30f, wy * 1.2f + seed + 30f, 4);
                float alpha = Mathf.Clamp01(1f - Mathf.Exp(-density * 2.5f));
                alpha *= Mathf.Clamp01(1f - Mathf.Pow(lobeDist * (1.2f + edgeNoise * 0.8f), 2.5f));
                alpha = Mathf.Clamp01(alpha * (1f - dustMask * 0.92f) + pillarRimGlow * 0.4f);

                // color
                Color col = Color.Lerp(new Color(0.7f, 0.15f, 0.2f), new Color(1f, 0.45f, 0.5f), haEmission);
                col += new Color(0.2f, 0.5f, 0.9f) * oiiiRegion;
                col += new Color(0.6f, 0.2f, 0.35f) * wisps;
                col += new Color(1.0f, 0.7f, 0.4f) * pillarRimGlow;
                col += new Color(0.8f, 0.9f, 1.0f) * starBloom * 2f;
                col *= (1f - dustMask * 0.7f);
                col.r = Mathf.Clamp01(col.r);
                col.g = Mathf.Clamp01(col.g);
                col.b = Mathf.Clamp01(col.b);

                pixels[y * size + x] = new Color(col.r, col.g, col.b, alpha);
            }
        }
    }

    // =====================================================================
    // GALAXY — Spiral galaxy
    // Layered: exponential disk, logarithmic spiral arms, bulge,
    //          HII regions, dust lanes, young stars, bar
    // =====================================================================

    static void GenerateGalaxy(Color[] pixels, int size, float seed)
    {
        // --- SEEDED PARAMS ---
        Random.State saved = Random.state;
        Random.InitState((int)(seed * 1000));

        int numArms = Random.Range(2, 5);
        float spiralTightness = Random.Range(3.5f, 6.5f);
        float[] armBrightnesses = new float[numArms];
        for (int a = 0; a < numArms; a++)
            armBrightnesses[a] = Random.Range(0.7f, 1.0f);
        float barLength = Random.Range(0.15f, 0.35f);
        float barWidth = Random.Range(0.04f, 0.08f);

        Random.state = saved;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (size * 0.5f)) - 1f;
                float ny = (y / (size * 0.5f)) - 1f;

                // initial warp
                float warp = FBM(nx * 2f + seed, ny * 2f + seed, 3) - 0.5f;
                float wx = nx + warp * 0.05f;
                float wy = ny + warp * 0.05f;

                float dist = Mathf.Sqrt(wx * wx + wy * wy);
                float angle = Mathf.Atan2(wy, wx);
                float angleDeg = angle * Mathf.Rad2Deg;

                // disk + bulge
                float disk = Mathf.Exp(-dist / 0.35f);
                float bulge = Mathf.Exp(-Mathf.Pow(dist / 0.08f, 1.5f));

                // shared noise (computed once per pixel)
                float breakMask = Mathf.Clamp01((FBM(wx * 6f + seed + 20f, wy * 6f + seed + 20f, 4) - 0.3f) * 2f);
                float fractalNoise = FBM(wx * 4f + seed, wy * 4f + seed, 5);

                // spiral arms
                float armMask = 0f;
                for (int arm = 0; arm < numArms; arm++)
                {
                    float armOffset = (arm / (float)numArms) * Mathf.PI * 2f;
                    float armSeed = seed + arm * 5f;
                    float expectedAngle = Mathf.Log(dist + 0.0001f) * spiralTightness + armOffset;
                    expectedAngle += (FBM(wx * 3f + armSeed, wy * 3f + armSeed, 3) - 0.5f) * 1.2f;

                    float diff = Mathf.Abs(Mathf.DeltaAngle(angleDeg, expectedAngle * Mathf.Rad2Deg));
                    float armVal = Mathf.Exp(-diff * 0.15f) * armBrightnesses[arm];
                    armVal *= Mathf.Lerp(0.5f, 1.5f, Mathf.PerlinNoise(wx * 4f + seed + arm * 10f, wy * 4f + seed + arm * 10f));
                    armMask = Mathf.Max(armMask, armVal);
                }
                armMask *= breakMask * Mathf.Lerp(0.6f, 1.2f, fractalNoise);

                // HII regions
                float clumpNoise = Mathf.PerlinNoise(wx * 30f + seed, wy * 30f + seed);
                float hii = (clumpNoise > 0.92f ? 1f : 0f) * armMask * disk * 2.5f;

                // dust
                float dustCut = Mathf.Clamp01(Mathf.Pow(FBM(wx * 8f + seed + 100f, wy * 8f + seed + 100f, 5), 4f) * disk * armMask * 1.5f);

                // central bar
                float barAlpha = 0f;
                float bx = Mathf.Abs(wx);
                float by = Mathf.Abs(wy);
                if (bx < barLength)
                    barAlpha = Mathf.Clamp01(1f - by / barWidth) * (1f - bx / barLength) * 0.8f;

                // density + alpha
                float density = (disk * 0.1f + armMask * disk * 2.8f + bulge * 2.2f + barAlpha * 0.5f) * (1f - dustCut);
                float alpha = Mathf.Clamp01(1f - Mathf.Exp(-density * 3f))
                              * Mathf.Clamp01(1f - Mathf.Pow(dist, 4f));

                // color
                float youngStars = armMask * (1f - dist);
                float starNoise = Mathf.PerlinNoise(wx * 200f + seed, wy * 200f + seed);
                float smallStars = Mathf.Pow(starNoise, 20f);
                float brightStars = Mathf.Pow(starNoise, 40f);

                Color col = new Color(0.9f, 0.9f, 1.0f);
                col = Color.Lerp(col, new Color(1f, 0.8f, 0.5f), Mathf.Clamp01(bulge * 1.2f));
                col = Color.Lerp(col, new Color(0.4f, 0.6f, 1f), armMask * 0.4f);
                col = Color.Lerp(col, new Color(1f, 0.2f, 0.3f), Mathf.Clamp01(hii * 1.5f));
                col = Color.Lerp(col, new Color(0.5f, 0.7f, 1f), Mathf.Clamp01(youngStars * 0.6f));
                col += new Color(1f, 0.95f, 0.9f) * smallStars * 1.5f;
                col += new Color(1f, 1f, 1f) * brightStars * 4.0f;
                col *= (1f - dustCut * 0.8f);
                col.r = Mathf.Clamp01(col.r);
                col.g = Mathf.Clamp01(col.g);
                col.b = Mathf.Clamp01(col.b);

                pixels[y * size + x] = new Color(col.r, col.g, col.b, alpha);
            }
        }
    }

    // =====================================================================
    // OPEN CLUSTER — Four archetypes:
    // 0 = sparse bright (Pleiades-like)
    // 1 = loose rich (Beehive-like)
    // 2 = compact old (M67-like)
    // 3 = double cluster
    // =====================================================================

    static void GenerateOpenCluster(Color[] pixels, int size, float seed)
    {
        // --- SEEDED PARAMS ---
        Random.State saved = Random.state;
        Random.InitState((int)(seed * 1000));

        int archetype = Random.Range(0, 4);

        Vector2 clusterCenter = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));

        float clusterRadius, coreSize, hazeAmount, faintThresholdOuter, faintThresholdInner;
        int numBrightStars;
        bool hasNebulosity, isOldPopulation;
        Vector2 secondCenter = Vector2.zero;

        switch (archetype)
        {
            case 0: // Pleiades-like
                clusterRadius = Random.Range(0.6f, 0.85f);
                coreSize = Random.Range(0.05f, 0.12f);
                hazeAmount = Random.Range(0.3f, 0.6f);
                numBrightStars = Random.Range(6, 12);
                faintThresholdOuter = 0.93f;
                faintThresholdInner = 0.82f;
                hasNebulosity = true;
                isOldPopulation = false;
                break;
            case 1: // Beehive-like
                clusterRadius = Random.Range(0.7f, 0.9f);
                coreSize = Random.Range(0.03f, 0.08f);
                hazeAmount = Random.Range(0.05f, 0.15f);
                numBrightStars = Random.Range(3, 7);
                faintThresholdOuter = 0.80f;
                faintThresholdInner = 0.65f;
                hasNebulosity = false;
                isOldPopulation = false;
                break;
            case 2: // M67-like
                clusterRadius = Random.Range(0.3f, 0.5f);
                coreSize = Random.Range(0.12f, 0.22f);
                hazeAmount = Random.Range(0.2f, 0.4f);
                numBrightStars = Random.Range(2, 5);
                faintThresholdOuter = 0.78f;
                faintThresholdInner = 0.60f;
                hasNebulosity = false;
                isOldPopulation = true;
                break;
            default: // double cluster
                clusterRadius = Random.Range(0.3f, 0.45f);
                coreSize = Random.Range(0.06f, 0.12f);
                hazeAmount = Random.Range(0.1f, 0.2f);
                numBrightStars = Random.Range(4, 9);
                faintThresholdOuter = 0.85f;
                faintThresholdInner = 0.70f;
                hasNebulosity = false;
                isOldPopulation = false;
                float separation = Random.Range(0.3f, 0.5f);
                float splitAngle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 splitDir = new Vector2(Mathf.Cos(splitAngle), Mathf.Sin(splitAngle));
                secondCenter = clusterCenter + splitDir * separation;
                clusterCenter -= splitDir * separation * 0.5f;
                break;
        }

        float elongation = Random.Range(0.75f, 1.3f);
        float elongationAngle = Random.Range(0f, Mathf.PI);
        float cosA = Mathf.Cos(elongationAngle);
        float sinA = Mathf.Sin(elongationAngle);

        numBrightStars = Mathf.Min(numBrightStars, 15);
        Vector2[] brightPos = new Vector2[15];
        float[] brightSize = new float[15];
        float[] brightLum = new float[15];
        for (int s = 0; s < numBrightStars; s++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(0f, clusterRadius * 0.8f);
            r = r * r / (clusterRadius * 0.8f);
            brightPos[s] = clusterCenter + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            brightSize[s] = isOldPopulation ? Random.Range(0.008f, 0.018f) : Random.Range(0.012f, 0.035f);
            brightLum[s] = Random.Range(0.5f, 1.0f);
        }

        int numGiants = isOldPopulation ? Random.Range(3, 7) : Random.Range(0, 3);
        int[] giantIndices = new int[7];
        for (int g = 0; g < numGiants && g < 7; g++)
            giantIndices[g] = Random.Range(0, numBrightStars);

        // second cluster stars for double archetype
        int numBrightStars2 = 0;
        Vector2[] brightPos2 = new Vector2[15];
        float[] brightSize2 = new float[15];
        float[] brightLum2 = new float[15];
        if (archetype == 3)
        {
            numBrightStars2 = Mathf.Min(Random.Range(4, 9), 15);
            for (int s = 0; s < numBrightStars2; s++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float r = Random.Range(0f, clusterRadius * 0.8f);
                r = r * r / (clusterRadius * 0.8f);
                brightPos2[s] = secondCenter + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                brightSize2[s] = Random.Range(0.012f, 0.030f);
                brightLum2[s] = Random.Range(0.5f, 1.0f);
            }
        }

        Random.state = saved;

        // --- PIXEL LOOP ---
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (size * 0.5f)) - 1f;
                float ny = (y / (size * 0.5f)) - 1f;

                // elongated distance from primary center
                float cx = nx - clusterCenter.x;
                float cy = ny - clusterCenter.y;
                float rotX = (cx * cosA + cy * sinA) / elongation;
                float rotY = (-cx * sinA + cy * cosA) * elongation;
                float elongDist = Mathf.Sqrt(rotX * rotX + rotY * rotY);

                // secondary center distance
                float minDist = elongDist;
                if (archetype == 3)
                {
                    float cx2 = nx - secondCenter.x;
                    float cy2 = ny - secondCenter.y;
                    float rotX2 = (cx2 * cosA + cy2 * sinA) / elongation;
                    float rotY2 = (-cx2 * sinA + cy2 * cosA) * elongation;
                    minDist = Mathf.Min(elongDist, Mathf.Sqrt(rotX2 * rotX2 + rotY2 * rotY2));
                }

                // domain warp
                float wx = nx + (FBM(nx * 2f + seed + 40f, ny * 2f + seed + 40f, 3) - 0.5f) * 0.08f;
                float wy = ny + (FBM(nx * 2f + seed + 45f, ny * 2f + seed + 45f, 3) - 0.5f) * 0.08f;

                // layer 1: unresolved haze
                float hazeFalloff = Mathf.Clamp01(1f - Mathf.Pow(minDist / clusterRadius, 2f));
                float haze = FBM(wx * 3f + seed, wy * 3f + seed, 4) * hazeFalloff * hazeAmount;
                if (hasNebulosity)
                    haze += FBM(wx * 2f + seed + 50f, wy * 2f + seed + 50f, 5)
                          * Mathf.Clamp01(1f - Mathf.Pow(minDist / (clusterRadius * 0.7f), 1.5f)) * 0.4f;
                haze = Mathf.Clamp01(haze);

                // layer 2: faint star field
                float faintNoise = (Mathf.PerlinNoise(nx * 80f + seed, ny * 80f + seed)
                                  + Mathf.PerlinNoise(nx * 120f + seed + 3f, ny * 120f + seed + 3f)) * 0.5f;
                float faintDensity = Mathf.Clamp01(1f - Mathf.Pow(minDist / clusterRadius, 1.5f));
                float fThresh = Mathf.Lerp(faintThresholdOuter, faintThresholdInner, faintDensity);
                float faintStars = faintNoise > fThresh
                                  ? Mathf.Clamp01((faintNoise - fThresh) / (1f - fThresh)) * faintDensity : 0f;

                // layer 3: bright stars
                float brightGlow = 0f;
                bool isGiant = false;
                for (int s = 0; s < numBrightStars; s++)
                {
                    float dx = nx - brightPos[s].x;
                    float dy = ny - brightPos[s].y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Clamp01(1f - d / brightSize[s]);
                    float bloom = Mathf.Clamp01(1f - d / (brightSize[s] * 5f)) * 0.3f;
                    float total = core * core * brightLum[s] + bloom * bloom * brightLum[s];
                    if (total > brightGlow)
                    {
                        brightGlow = total;
                        isGiant = false;
                        for (int g = 0; g < numGiants && g < 7; g++)
                            if (giantIndices[g] == s) { isGiant = true; break; }
                    }
                }

                float brightGlow2 = 0f;
                for (int s = 0; s < numBrightStars2; s++)
                {
                    float dx = nx - brightPos2[s].x;
                    float dy = ny - brightPos2[s].y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Clamp01(1f - d / brightSize2[s]);
                    float bloom = Mathf.Clamp01(1f - d / (brightSize2[s] * 5f)) * 0.3f;
                    brightGlow2 = Mathf.Max(brightGlow2, core * core * brightLum2[s] + bloom * bloom * brightLum2[s]);
                }
                float totalBright = Mathf.Max(brightGlow, brightGlow2);

                // layer 4: core concentration
                float coreDist = Vector2.Distance(new Vector2(nx, ny), clusterCenter);
                float coreGlow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Pow(coreDist / coreSize, 1.5f)), 2f) * 0.6f;
                if (archetype == 3)
                {
                    float coreDist2 = Vector2.Distance(new Vector2(nx, ny), secondCenter);
                    float coreGlow2 = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Pow(coreDist2 / coreSize, 1.5f)), 2f) * 0.6f;
                    coreGlow = Mathf.Max(coreGlow, coreGlow2);
                }

                // alpha
                float density = haze * 0.3f + faintStars * 0.6f + totalBright + coreGlow * 0.5f;
                float alpha = Mathf.Clamp01(1f - Mathf.Exp(-density * 3f))
                              * Mathf.Clamp01(1f - Mathf.Pow(minDist / (clusterRadius * 1.2f), 3f));

                // color
                Color col = isOldPopulation ? new Color(1.0f, 0.92f, 0.78f) : new Color(0.82f, 0.9f, 1.0f);
                col = Color.Lerp(col, isOldPopulation ? new Color(1f, 0.85f, 0.6f) : new Color(1f, 0.97f, 0.88f), coreGlow * 2f);
                col = Color.Lerp(col, isOldPopulation ? new Color(1f, 0.88f, 0.7f) : new Color(0.7f, 0.85f, 1f), faintStars * 0.8f);
                col = Color.Lerp(col, new Color(0.95f, 0.97f, 1f), totalBright * 0.7f);
                if (isGiant && brightGlow > 0.3f)
                    col = Color.Lerp(col, new Color(1f, 0.65f, 0.25f), brightGlow * 0.9f);
                if (hasNebulosity)
                    col = Color.Lerp(col, new Color(0.5f, 0.65f, 1f), haze * 0.5f);
                else
                    col += new Color(0.88f, 0.86f, 0.8f) * haze * 0.2f;

                col.r = Mathf.Clamp01(col.r);
                col.g = Mathf.Clamp01(col.g);
                col.b = Mathf.Clamp01(col.b);

                pixels[y * size + x] = new Color(col.r, col.g, col.b, alpha);
            }
        }
    }

    // =====================================================================
    // GLOBULAR CLUSTER — King profile density model
    // Layered: blazing core, unresolved inner glow, resolved halo stars,
    //          granular texture, outer sparse stars
    // =====================================================================

    static void GenerateGlobularCluster(Color[] pixels, int size, float seed)
    {
        // --- SEEDED PARAMS ---
        Random.State saved = Random.state;
        Random.InitState((int)(seed * 1000));

        Vector2 globCenter = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f));
        float coreRadius = Random.Range(0.04f, 0.12f);
        float tidalRadius = Random.Range(0.6f, 0.95f);
        float richness = Random.Range(0.6f, 1.0f);
        float ellipticity = Random.Range(0.85f, 1.0f);
        float ellipAngle = Random.Range(0f, Mathf.PI);
        float concentration = Random.Range(0.3f, 1.0f);
        float coreBlowout = Mathf.Lerp(0.5f, 1.5f, concentration);

        Random.state = saved;

        float cosE = Mathf.Cos(ellipAngle);
        float sinE = Mathf.Sin(ellipAngle);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (size * 0.5f)) - 1f;
                float ny = (y / (size * 0.5f)) - 1f;

                float gcx = nx - globCenter.x;
                float gcy = ny - globCenter.y;

                // elliptical coordinates
                float ex = (gcx * cosE + gcy * sinE) / ellipticity;
                float ey = (-gcx * sinE + gcy * cosE) * ellipticity;
                float r = Mathf.Sqrt(ex * ex + ey * ey);

                // King profile: (1/sqrt(1+(r/rc)^2) - 1/sqrt(1+(rt/rc)^2))^2
                float kingCore = 1f / Mathf.Sqrt(1f + Mathf.Pow(r / coreRadius, 2f));
                float kingTidal = 1f / Mathf.Sqrt(1f + Mathf.Pow(tidalRadius / coreRadius, 2f));
                float king = Mathf.Clamp01(Mathf.Pow(Mathf.Max(0f, kingCore - kingTidal), 2f) * richness * 6f);

                // layer 1: unresolved inner glow
                float innerGlow = Mathf.Clamp01(Mathf.Exp(-r / (coreRadius * 3f)) * richness);

                // layer 2: blazing core
                float coreGlow = Mathf.Clamp01(Mathf.Exp(-Mathf.Pow(r / coreRadius, 1.2f) * 4f) * coreBlowout);

                // layer 3: resolved halo stars — high freq noise thresholded by King density
                float sn1 = Mathf.PerlinNoise(nx * 150f + seed, ny * 150f + seed);
                float sn2 = Mathf.PerlinNoise(nx * 220f + seed + 7f, ny * 220f + seed + 7f);
                float sn3 = Mathf.PerlinNoise(nx * 300f + seed + 13f, ny * 300f + seed + 13f);
                float snCombined = sn1 * 0.5f + sn2 * 0.3f + sn3 * 0.2f;
                float starThresh = Mathf.Lerp(0.92f, 0.60f, king);
                float resolvedStars = snCombined > starThresh
                                   ? Mathf.Clamp01((snCombined - starThresh) / (1f - starThresh)) * king : 0f;

                float brightThresh = Mathf.Lerp(0.97f, 0.80f, king);
                float brightStars = sn1 > brightThresh
                                   ? Mathf.Clamp01((sn1 - brightThresh) / (1f - brightThresh)) * king : 0f;
                float brightBloom = sn1 > 0.94f
                                   ? Mathf.Clamp01((sn1 - 0.94f) / 0.06f) * king * 0.4f : 0f;

                // layer 4: granular texture
                float grain = FBM(nx * 8f + seed + 60f, ny * 8f + seed + 60f, 3) * king * 0.3f;

                // layer 5: outer sparse stars
                float outerNoise = Mathf.PerlinNoise(nx * 45f + seed + 20f, ny * 45f + seed + 20f);
                float outerFalloff = Mathf.Clamp01(1f - Mathf.Pow(r / tidalRadius, 2f));
                float outerStars = outerNoise > 0.94f
                                   ? Mathf.Clamp01((outerNoise - 0.94f) / 0.06f) * outerFalloff * 0.5f : 0f;

                // alpha
                float density = innerGlow * 1.2f + coreGlow * 1.5f + resolvedStars * 0.5f
                              + brightStars * 0.8f + brightBloom * 0.4f + grain * 0.3f + outerStars * 0.2f;
                float alpha = Mathf.Clamp01(1f - Mathf.Exp(-density * 2.5f))
                              * Mathf.Clamp01(1f - Mathf.Pow(r / tidalRadius, 4f));

                // color — warm yellow-white throughout, Population II
                Color col = new Color(1.0f, 0.93f, 0.78f);
                col = Color.Lerp(col, new Color(1.0f, 0.98f, 0.92f), coreGlow);
                col = Color.Lerp(col, new Color(1.0f, 0.88f, 0.65f), innerGlow * 0.5f);
                col = Color.Lerp(col, new Color(0.95f, 0.95f, 0.90f), resolvedStars * 0.6f);
                col = Color.Lerp(col, new Color(1.0f, 1.0f, 0.98f), brightStars);
                col = Color.Lerp(col, new Color(0.88f, 0.92f, 1.0f), outerStars * 0.5f);
                col.r = Mathf.Clamp01(col.r);
                col.g = Mathf.Clamp01(col.g);
                col.b = Mathf.Clamp01(col.b);

                pixels[y * size + x] = new Color(col.r, col.g, col.b, alpha);
            }
        }
    }

    // =====================================================================
    // FBM — Fractional Brownian Motion
    // Layered Perlin noise with decreasing amplitude per octave
    // =====================================================================

    static float FBM(float x, float y, int octaves)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;
        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            frequency *= 2f;
            amplitude *= 0.5f;
        }
        return value;
    }
}