using Unity.Barracuda;
using UnityEngine;

namespace MediaPipe.FaceLandmark {

//
// Face landmark detector class
//
public sealed class FaceLandmarkDetector : System.IDisposable
{
    #region Public methods/properties

    public const int VertexCount = 468;

    public FaceLandmarkDetector(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunModel(image);

    public GraphicsBuffer VertexBuffer
      => _output;

    public System.ReadOnlySpan<Vector4> VertexArray
      => _readCache.Cached;

    #endregion

    #region Private objects

    // Input image size (defined by the model)
    const int ImageSize = 192;

    ResourceSet _resources;
    IWorker _worker;
    (Tensor tensor, ComputeTensorData data) _preprocess;
    GraphicsBuffer _output;
    ReadCache _readCache;

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
        _output = BufferUtil.NewStructured<Vector4>(VertexCount);

        // Read cache
        _readCache = new ReadCache(_output);
    }

    void DeallocateObjects()
    {
        _worker?.Dispose();
        _worker = null;

        _preprocess.tensor?.Dispose();
        _preprocess = (null, null);

        _output?.Dispose();
        _output = null;
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
        post.SetBuffer(0, "_Vertices", _output);
        post.Dispatch(0, VertexCount / 52, 1, 1);
        RenderTexture.ReleaseTemporary(tempRT);

        // Cache data invalidation
        _readCache.Invalidate();
    }

    #endregion
}

} // namespace MediaPipe.FaceLandmark
