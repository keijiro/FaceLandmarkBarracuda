using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

namespace MediaPipe.FaceLandmark {

//
// Face landmark detector class
//
public sealed class FaceLandmarkDetector : System.IDisposable
{
    #region Public accessors

    public const int VertexCount = 468;

    public ComputeBuffer VertexBuffer
      => _postBuffer;

    public IEnumerable<Vector4> VertexArray
      => _postRead ? _postReadCache : UpdatePostReadCache();

    #endregion

    #region Public methods

    public FaceLandmarkDetector(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunModel(image);

    #endregion

    #region Compile-time constants

    // Input image size (defined by the model)
    const int ImageSize = 192;

    #endregion

    #region Private objects

    ResourceSet _resources;
    (Tensor tensor, ComputeTensorData data) _preprocess;
    ComputeBuffer _postBuffer;
    IWorker _worker;

    void AllocateObjects(ResourceSet resources)
    {
        // NN model
        var model = ModelLoader.Load(resources.model);

        // Private objects
        _resources = resources;
        _worker = model.CreateWorker(WorkerFactory.Device.GPU);

        // Preprocessing buffer
#if BARRACUDA_4_0_0_OR_LATER
        var inputShape = new TensorShape(1, 3, ImageSize, ImageSize);
        _preprocess.data = new ComputeTensorData(inputShape, "input", false);
        _preprocess.tensor = TensorFloat.Zeros(inputShape);
        _preprocess.tensor.AttachToDevice(_preprocess.data);
#else
        var inputShape = new TensorShape(1, ImageSize, ImageSize, 3);
        _preprocess.data = new ComputeTensorData
          (inputShape, "input", ComputeInfo.ChannelsOrder.NHWC, false);
        _preprocess.tensor = new Tensor(inputShape, _preprocess.data);
#endif

        // Output buffer
        _postBuffer = new ComputeBuffer(VertexCount, sizeof(float) * 4);
    }

    void DeallocateObjects()
    {
        _worker?.Dispose();
        _worker = null;

        _preprocess.tensor?.Dispose();
        _preprocess = (null, null);

        _postBuffer?.Dispose();
        _postBuffer = null;
    }

    #endregion

    #region Neural network inference function

    void RunModel(Texture source)
    {
#if BARRACUDA_4_0_0_OR_LATER
        const int PrePassNum = 1;
#else
        const int PrePassNum = 0;
#endif

        // Preprocessing
        var pre = _resources.preprocess;
        pre.SetTexture(PrePassNum, "_Texture", source);
        pre.SetBuffer(PrePassNum, "_Tensor", _preprocess.data.buffer);
        pre.Dispatch(PrePassNum, ImageSize / 8, ImageSize / 8, 1);

        // Run the BlazeFace model.
        _worker.Execute(_preprocess.tensor);

        // Postprocessing
        var post = _resources.postprocess;
        var tempRT = _worker.CopyOutputToTempRT(1, VertexCount * 3);
        post.SetTexture(0, "_Tensor", tempRT);
        post.SetBuffer(0, "_Vertices", _postBuffer);
        post.Dispatch(0, VertexCount / 52, 1, 1);
        RenderTexture.ReleaseTemporary(tempRT);

        // Read cache invalidation
        _postRead = false;
    }

    #endregion

    #region GPU to CPU readback

    Vector4[] _postReadCache = new Vector4[VertexCount];
    bool _postRead;

    Vector4[] UpdatePostReadCache()
    {
        _postBuffer.GetData(_postReadCache, 0, 0, VertexCount);
        _postRead = true;
        return _postReadCache;
    }

    #endregion
}

} // namespace MediaPipe.FaceLandmark
