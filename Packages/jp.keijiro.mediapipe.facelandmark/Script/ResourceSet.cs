using UnityEngine;
using Unity.Barracuda;

namespace MediaPipe.FaceLandmark {

//
// ScriptableObject class used to hold references to internal assets
//
[CreateAssetMenu(fileName = "FaceLandmark",
                 menuName = "ScriptableObjects/MediaPipe/FaceLandmark Resource Set")]
public sealed class ResourceSet : ScriptableObject
{
    public NNModel model;
    public ComputeShader preprocess;
    public ComputeShader postprocess;
}

} // namespace MediaPipe.FaceLandmark
