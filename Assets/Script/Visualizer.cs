using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.FaceLandmark;

public sealed class Visualizer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] ImageSource _source = null;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] RawImage _previewUI = null;
    [SerializeField] Mesh _template = null;
    [SerializeField] Shader _shader = null;

    #endregion

    #region Private members

    FaceLandmarkDetector _detector;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _detector = new FaceLandmarkDetector(_resources);
        _material = new Material(_shader);
    }

    void OnDestroy()
    {
        _detector.Dispose();
        Destroy(_material);
    }

    void LateUpdate()
    {
        // Face landmark detection
        _detector.ProcessImage(_source.Texture);

        // UI update
        _previewUI.texture = _source.Texture;
    }

    void OnRenderObject()
    {
        // Wireframe mesh rendering
        _material.SetBuffer("_Vertices", _detector.VertexBuffer);
        _material.SetPass(0);
        Graphics.DrawMeshNow(_template, Matrix4x4.identity);

        // Keypoint marking
        _material.SetBuffer("_Vertices", _detector.VertexBuffer);
        _material.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 400, 1);
    }

    #endregion
}
