using System.Collections.Generic;
using UnityEngine;

public class InstancingBatcherSystem : MonoBehaviour
{
    const string FRAME_OFFSETS = "_FrameOffsets";

    static List<InstancingBatch> Batch = new List<InstancingBatch>();

    public static int AddNewBatch(InstancingComponent mono, int offset, Mesh mesh = null, Material material = null)
    {   
        InstancingBatch batch = new InstancingBatch();
        batch.Shared = new SharedParameters { Mesh = mesh, Material = material };
        batch.SharedHash = batch.Shared.GetHashCode();
        batch.ResultProperty = new MaterialPropertyBlock();
        batch.ResultProperty.SetFloatArray(FRAME_OFFSETS, new float[16000]);

        batch.init = true;

        if (Batch == null)
            Batch = new List<InstancingBatch>();

        Batch.Add(batch);
        batch.ID = Batch.Count-1;

        AddInstance(batch.ID, mono, offset);

        return batch.ID;
    }

    public static int Add(int ID, InstancingComponent mono, int offset, Mesh mesh = null, Material material = null)
    {
        int index = IndexOfBatch(mesh, material);

        // remove object from previous batch 
        if (ID != index && ID != -1)
            RemoveInstance(ID, mono);

        // create new batch if there is any form this mesh+material pair
        if (index == -1)
            return AddNewBatch(mono, offset, mesh, material);

        // add instance, skip if it's already in batch
        if (Batch[index].Instances.IndexOf(mono) == -1)
            AddInstance(index, mono, offset);

        return Batch[index].ID;
    }

    public static void Remove(int ID, InstancingComponent mono)
    {
        int index = IndexOfBatch(ID);
        if (index == -1)
            return;

        RemoveInstance(index, mono);
    }

    private static void AddInstance(int index, InstancingComponent mono, int FrameOffset) 
    {
        Batch[index].Instances.Add(mono);
        Batch[index].ResultMatrix.Add(mono.transform.localToWorldMatrix);
        Batch[index].FrameOffsets.Add(FrameOffset);
    }

    private static void RemoveInstance(int index, InstancingComponent mono) 
    {
        int indx = -1;
        indx = Batch[index].Instances.IndexOf(mono);

        if (indx == -1)
            return;
        
        Batch[index].ResultMatrix.RemoveAt(indx);
        Batch[index].Instances.RemoveAt(indx);
        Batch[index].FrameOffsets.RemoveAt(indx);
    }

    private static int IndexOfBatch(int ID) 
    {
        for (int i = 0; i < Batch.Count; i++)
            if (Batch[i].ID == ID)
                return i;
        return -1;
    }

    private static int IndexOfBatch(Mesh mesh, Material material)
    {
        int hash = new SharedParameters { Mesh = mesh , Material = material }.GetHashCode();
        for (int i = 0; i < Batch.Count; i++)
            if (Batch[i].SharedHash == hash)
                return i;
        return -1;
    }

    void UpdateInstances(InstancingBatch batch)
    {
        for (int i = 0; i < batch.Instances.Length; i++)
        {
            batch.ResultMatrix.Items[i] = batch.Instances.Items[i].Matrix;
            batch.FrameOffsets.Items[i] = batch.Instances.Items[i].FrameOffset;
            batch.Instances.Items[i].Changed = false;
        }
    }

    private void Update()
    {
        if (Batch == null)
            return;

        for (int i = 0; i < Batch.Count; i++)
        {
            if (Batch[i].init == false || Batch[i].ResultMatrix.Length < 1)
                continue;
            UpdateInstances(Batch[i]);
            Batch[i].ResultProperty.Clear();
            Batch[i].ResultProperty.SetFloatArray(FRAME_OFFSETS, Batch[i].FrameOffsets.Items);
            Graphics.DrawMeshInstanced(
                mesh: Batch[i].Shared.Mesh,
                submeshIndex: 0,
                castShadows: UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows: true,
                material: Batch[i].Shared.Material,
                matrices: Batch[i].ResultMatrix.Items,
                count: Batch[i].ResultMatrix.Length,
                properties: Batch[i].ResultProperty);
        }
    }

}

internal class InstancingBatch
{
        internal bool init;
        internal int ID;
        internal int SharedHash;
        internal SharedParameters Shared;
        internal MaterialPropertyBlock ResultProperty;
        internal DynamicArray<InstancingComponent> Instances = new DynamicArray<InstancingComponent>();

        // matrix
        internal DynamicArray<Matrix4x4> ResultMatrix = new DynamicArray<Matrix4x4>();
        internal DynamicArray<float> FrameOffsets = new DynamicArray<float>();
}

internal struct SharedParameters
{
    internal Mesh Mesh;
    internal Material Material;
}

