using System.Net;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleNetworkSyncN: MonoBehaviour
    {
        public string state;
        public float syncRate;
        private float syncTimer;

        void Start()
        {
            syncTimer = 0f;
        }

        void Update()
        {
            syncTimer += Time.deltaTime;
            if (!(syncTimer >= 1f / syncRate)) return;
            // do a post
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://example.com/sync");
            request.Method = "POST";
            request.ContentType = "application/json";
            using (var streamWriter = new System.IO.StreamWriter(request.GetRequestStream()))
            {
                string json = "{\"state\":\"" + state + "\"}";
                streamWriter.Write(json);
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Debug.LogError("Network sync failed with status: " + response.StatusCode);
            }
            response.Close();
            syncTimer = 0f;
        }
    }
}