using System.Collections;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleResourcePreloaderN : MonoBehaviour
    {
        public string resourcePath;
        public Object loadedResource;

        void Start()
        {
            StartCoroutine(LoadAsync());
        }

        IEnumerator LoadAsync()
        {
            ResourceRequest request = Resources.LoadAsync(resourcePath);
            yield return request;
            loadedResource = request.asset;
        }

        public Object GetResource()
        {
            return loadedResource;
        }
    }
}