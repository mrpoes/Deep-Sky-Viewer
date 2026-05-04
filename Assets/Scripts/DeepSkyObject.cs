using UnityEngine;

public class DeepSkyObject : MonoBehaviour
{
    public enum DSOType { Nebula, Galaxy, OpenCluster, GlobularCluster }
    public DSOType type;
    public float brightness = 0.6f;
    public float scale = 1f;

    private Camera targetCamera;

    void Start()
    {
        ApplyTexture();
    }

    public void Init(Vector3 direction, DSOType dsoType, float brightness, float scale, float sphereRadius)
    {
        this.type = dsoType;
        this.brightness = brightness;
        this.scale = scale;
        transform.position = direction.normalized * sphereRadius;
        transform.localScale = Vector3.one * scale;
        ApplyTexture();
    }

    void ApplyTexture()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        Texture2D tex = DSOTextureGenerator.Generate(type);
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        mat.mainTexture = tex;
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 3f);  // additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.color = GetTypeColor();
        renderer.material = mat;
    }

    Color GetTypeColor()
    {
        float b = brightness;
        switch (type)
        {
            case DSOType.Nebula: return new Color(0.4f * b, 0.8f * b, 0.6f * b, 1f);
            case DSOType.Galaxy: return new Color(b, b, b, 1f);
            case DSOType.OpenCluster: return new Color(0.7f * b, 0.85f * b, 1.0f * b, 1f);
            case DSOType.GlobularCluster: return new Color(b, b, b, 1f);
            default: return Color.white * b;
        }
    }

    void LateUpdate()
    {
        // billboard: always face the active camera
        Camera cam = Camera.main ?? FindAnyObjectByType<Camera>();
        if (cam != null)
            transform.LookAt(transform.position + (transform.position - cam.transform.position));
    }
}