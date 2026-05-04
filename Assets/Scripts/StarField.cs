using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class StarField : MonoBehaviour
{
    [Header("Star Count")]
    public int starCount = 5000;

    [Header("Sky Sphere")]
    public float sphereRadius = 2000f;

    [Header("Brightness")]
    public float minSize = 0.05f;
    public float maxSize = 0.25f;
    public float minBrightness = 0.3f;
    public float maxBrightness = 1.0f;

    [Header("Follow Target")]
    public Transform player;

    private ParticleSystem ps;

    Color GetStarColor(float brightness)
    {
        float roll = Random.value;

        if (roll < 0.10f) // 10% blue-white (hot stars)
            return new Color(0.8f, 0.9f, 1.0f) * brightness;
        else if (roll < 0.20f) // 10% orange (cool stars)
            return new Color(1.0f, 0.9f, 0.8f) * brightness;
        else if (roll < 0.30f) // 10% yellow
            return new Color(1.0f, 0.95f, 0.7f) * brightness;
        else // 70% white
            return new Color(brightness, brightness, brightness);
    }
    Texture2D CreateStarTexture()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - Mathf.Pow(dist, 0.8f));
                alpha *= alpha; // sharpen falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        GenerateStars();
    }

    void LateUpdate()
    {
        if (player != null)
            transform.position = player.position;
    }

    void GenerateStars()
    {
        // create texture once and assign before the loop
        var renderer = ps.GetComponent<Renderer>();
        renderer.material.mainTexture = CreateStarTexture();

        var particles = new ParticleSystem.Particle[starCount];
        for (int i = 0; i < starCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            particles[i].position = dir * sphereRadius;

            float brightness = Random.Range(minBrightness, maxBrightness);
            particles[i].startSize = Mathf.Lerp(minSize, maxSize, brightness);
            particles[i].startColor = GetStarColor(brightness);
            particles[i].remainingLifetime = float.MaxValue;
            particles[i].startLifetime = float.MaxValue;
        }

        ps.SetParticles(particles, starCount);
    }
}