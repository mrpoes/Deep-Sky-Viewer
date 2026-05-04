using UnityEngine;

public class DSOManager : MonoBehaviour
{
    public float sphereRadius = 1900f; // just inside starfield sphere
    public int nebulaCount = 10;
    public int galaxyCount = 10;
    public int openClusterCount = 10;
    public int globClusterCount = 10;



    void Start()
    {
        Spawn(DeepSkyObject.DSOType.Nebula, nebulaCount, 0.5f);
        Spawn(DeepSkyObject.DSOType.Galaxy, galaxyCount, 0.4f);
        Spawn(DeepSkyObject.DSOType.OpenCluster, openClusterCount, 0.6f);
        Spawn(DeepSkyObject.DSOType.OpenCluster, globClusterCount, 0.6f);
    }

    void Spawn(DeepSkyObject.DSOType type, int count, float brightness)
    {
        float scale = 50f; // default

        switch (type)
        {
            case DeepSkyObject.DSOType.Nebula:
                {
                    scale = Random.Range(50f, 200f);
                    break;
                }
            case DeepSkyObject.DSOType.Galaxy:
                {
                    scale = Random.Range(20f, 100f);
                    break;
                }
            case DeepSkyObject.DSOType.OpenCluster or DeepSkyObject.DSOType.GlobularCluster:
                {
                    scale = Random.Range(20f, 100f);
                    break;
                }
        }
        for (int i = 0; i < count; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"{type}_{i}";
            go.layer = LayerMask.NameToLayer("SkyLayer");
            Destroy(go.GetComponent<MeshCollider>());

            var dso = go.AddComponent<DeepSkyObject>();
            dso.Init(Random.onUnitSphere, type, brightness, scale, sphereRadius);
        }
    }
}