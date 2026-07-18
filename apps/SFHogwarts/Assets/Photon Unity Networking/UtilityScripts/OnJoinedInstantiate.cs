using UnityEngine;

public class OnJoinedInstantiate : MonoBehaviour
{
    public float PositionOffset = 2.0f;
    public GameObject[] PrefabsToInstantiate; // set in inspector
    public Transform SpawnPosition;

    public void OnJoinedRoom()
    {
        if (PrefabsToInstantiate != null)
            foreach (var o in PrefabsToInstantiate)
            {
                Debug.Log("Instantiating: " + o.name);

                var spawnPos = Vector3.up;
                if (SpawnPosition != null) spawnPos = SpawnPosition.position;

                var random = Random.insideUnitSphere;
                random.y = 0;
                random = random.normalized;
                var itempos = spawnPos + PositionOffset * random;

                PhotonNetwork.Instantiate(o.name, itempos, Quaternion.identity, 0);
            }
    }
}