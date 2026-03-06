using UnityEngine;
using UnityEngine.Rendering;

public class  InstancingComponent : MonoBehaviour, ITestAnimator
{
    [SerializeField] int BatchId = -1;
    [SerializeField] Mesh Mesh;
    [SerializeField] Material Material;

    public bool Changed = false;

    public Transform CachedTransform;
    public Matrix4x4 Matrix;
    public float FrameOffset;

    private void Awake()
    {
        CachedTransform = transform;
        Matrix = CachedTransform.localToWorldMatrix;
        FrameOffset = 0;
        AddToSystem();
    }

    private void AddToSystem() 
    {
        UpdateData();
        BatchId = InstancingBatcherSystem.Add(BatchId, this, 0, Mesh, Material);
    }

    public void OnDestroy()
    {
        InstancingBatcherSystem.Remove(BatchId, this);
    }

    public void UpdateRenderer(int offset) 
    {
        FrameOffset = offset;
        Changed = true;
    }

    private void UpdateData()
    {
        if (Mesh == null)
            Mesh = this.GetComponent<MeshFilter>().sharedMesh;
        if (Material == null)
        {
            MeshRenderer renderer = this.GetComponent<MeshRenderer>();
            Material = renderer.sharedMaterial;
            renderer.enabled = false;
        }
    } 
}
