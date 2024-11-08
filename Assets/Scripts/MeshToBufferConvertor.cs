using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

public class MeshToBufferConvertor : MonoBehaviour
{
    ARMeshManager meshManager;
    TrackedPoseDriver trackedPoseDriver;

    [Header("Buffer Settings")]
    [SerializeField] int bufferInitialCapacity = 64000;
    [SerializeField] bool dynamicallyResizeBuffer = false;
    private const int BUFFER_STRIDE = 12; // 12 Bytes for a Vector3 (4,4,4)


    [Header("Debug VFX")]
    [SerializeField] bool debugMode = false;
    [SerializeField] VisualEffect vfx;

    List<Vector3> listVertex;
    GraphicsBuffer bufferVertex;
    public GraphicsBuffer VertexBuffer { get => bufferVertex; }
    public int VertexCount { get => listVertex == null ? 0 : listVertex.Count; }

    List<Vector3> listNormal;
    GraphicsBuffer bufferNormal;
    public GraphicsBuffer NormalBuffer { get => bufferNormal; }

    List<(float, int)> listMeshDistance = new List<(float, int)>();
    List<int> listRandomIndex = new List<int>();

    void Awake()
    {
        meshManager = FindFirstObjectByType<ARMeshManager>();
        if (meshManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find meshManager.");
        }

        trackedPoseDriver = FindFirstObjectByType<TrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find TrackedPoseDriver.");
        }

        // Create initial graphics buffer
        listVertex = new List<Vector3>(bufferInitialCapacity);
        EnsureBufferCapacity(ref bufferVertex, bufferInitialCapacity, BUFFER_STRIDE);

        listNormal = new List<Vector3>(bufferInitialCapacity);
        EnsureBufferCapacity(ref bufferNormal, bufferInitialCapacity, BUFFER_STRIDE);

        if (vfx != null)
            vfx.enabled = debugMode;
    }

    //void OnEnable()
    //{
    //    if(meshManager != null)
    //        meshManager.meshesChanged += UpdateBuffer;
    //}
    //void OnDisable()
    //{
    //    if (meshManager != null)
    //        meshManager.meshesChanged -= UpdateBuffer;
    //}

    //void UpdateBuffer(ARMeshesChangedEventArgs args)
    void LateUpdate()
    {
        if (GameManager.Instance.GameMode == GameMode.Undefined || meshManager == null || trackedPoseDriver == null)
            return;


        IList<MeshFilter> mesh_list = meshManager.meshes;        

        if (mesh_list == null)
        {
            listVertex.Clear();
            listNormal.Clear();

            bufferVertex.SetData(listVertex);
            bufferNormal.SetData(listNormal);

            return;
        }

        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Mesh Count:", mesh_list.Count.ToString());

        // 
        int mesh_count = mesh_list.Count;
        int vertex_count = 0;
        int triangle_count = 0;
        Vector3 head_pos = trackedPoseDriver.transform.position;

        float distance = 0;
        Vector3 min_pos = Vector3.zero;
        Vector3 max_pos = Vector3.zero;


        // Clear list before updating
        listVertex.Clear();
        listNormal.Clear();

        // If dynamicallyResizeBuffer is ON
        if (dynamicallyResizeBuffer)
        {
            // randomize the order of mesh
            listRandomIndex.Clear();
            for (int i = 0; i < mesh_list.Count; i++)
            {
                listRandomIndex.Add(i);
            }

            int n = listRandomIndex.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                int value = listRandomIndex[k];
                listRandomIndex[k] = listRandomIndex[n];
                listRandomIndex[n] = value;
            }

            // push to buffer
            for (int i = 0; i < listRandomIndex.Count; i++)
            {
                int index = listRandomIndex[i];
                MeshFilter mesh = mesh_list[index];

                listVertex.AddRange(mesh.sharedMesh.vertices);
                listNormal.AddRange(mesh.sharedMesh.normals);

                vertex_count += mesh.sharedMesh.vertexCount;
                triangle_count += mesh.sharedMesh.triangles.Length / 3;

                min_pos = Vector3.Min(min_pos, mesh.sharedMesh.bounds.min);
                max_pos = Vector3.Max(max_pos, mesh.sharedMesh.bounds.max);
            }
        }

        // If dynamicallyResizeBuffer is OFF
        else
        {
            // sort all meshes by distance
            listMeshDistance.Clear();
            for (int i = 0; i < mesh_list.Count; i++)
            {
                MeshFilter mesh = mesh_list[i];

                distance = Vector3.Distance(head_pos, mesh.sharedMesh.bounds.center);

                listMeshDistance.Add((distance, i));
            }
            listMeshDistance.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            // push nearest to buffer
            for (int i = 0; i < listMeshDistance.Count; i++)
            {
                int index = listMeshDistance[i].Item2;
                MeshFilter mesh = mesh_list[index];

                listVertex.AddRange(mesh.sharedMesh.vertices);
                listNormal.AddRange(mesh.sharedMesh.normals);

                vertex_count += mesh.sharedMesh.vertexCount;
                triangle_count += mesh.sharedMesh.triangles.Length / 3;

                min_pos = Vector3.Min(min_pos, mesh.sharedMesh.bounds.min);
                max_pos = Vector3.Max(max_pos, mesh.sharedMesh.bounds.max);

                if (vertex_count > bufferInitialCapacity)
                    break;
            }

            if (vertex_count > bufferInitialCapacity)
            {
                listVertex.RemoveRange(bufferInitialCapacity, vertex_count - bufferInitialCapacity);
                listNormal.RemoveRange(bufferInitialCapacity, vertex_count - bufferInitialCapacity);
            }
        }


        // Set Buffer data, but before that ensure there is enough capacity
        EnsureBufferCapacity(ref bufferVertex, listVertex.Count, BUFFER_STRIDE);
        bufferVertex.SetData(listVertex);

        EnsureBufferCapacity(ref bufferNormal, listNormal.Count, BUFFER_STRIDE);
        bufferNormal.SetData(listNormal);

        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("VertexBufferCount", bufferVertex.count.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("listVertex.Count", listVertex.Count.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("NormalBufferCount", bufferNormal.count.ToString());

        DebugVFX();
    }

    void DebugVFX()
    {
        if (debugMode == false || vfx == null)
            return;

        vfx.SetInt("VertexCount", VertexCount);
        vfx.SetGraphicsBuffer("VertexBuffer", VertexBuffer);
        vfx.SetGraphicsBuffer("NormalBuffer", NormalBuffer);
    }

    private void EnsureBufferCapacity(ref GraphicsBuffer buffer, int capacity, int stride)
    {
        // Reallocate new buffer only when null or capacity is not sufficient
        if (buffer == null || (dynamicallyResizeBuffer && buffer.count < capacity)) // remove dynamic allocating function
        {
            // Buffer memory must be released
            buffer?.Release();
            // Vfx Graph uses structured buffer
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, stride);
        }
    }

    private void ReleaseBuffer(ref GraphicsBuffer buffer)
    {
        // Buffer memory must be released
        buffer?.Release();
        buffer = null;
    }

    // 
    // https://forum.unity.com/threads/vfx-graph-siggraph-2021-video.1198156/
    

    void OnDestroy()
    {
        ReleaseBuffer(ref bufferVertex);

        ReleaseBuffer(ref bufferNormal);
    }
}
