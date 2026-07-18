using UnityEngine;

/*
NPC detection is separated into a empty child to prevent having 2 collider components in the same GameObject (root has already 1)
 */

public class NPCDetector : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        canBeTargeted(other);
    }

    public void OnTriggerStay(Collider other)
    {
        canBeTargeted(other);
    }

    public void canBeTargeted(Collider other)
    {
        if (other.gameObject.tag != "NPC") return;

        Vector3 hitPoint;

        var ray = new Ray(transform.position, other.gameObject.transform.position);
        var hitTransform = FindClosestHitObject(ray, 100, out hitPoint);

        // looks like there is nothing between us and the player
        if (hitTransform == null)
            other.gameObject.GetComponent<NPC>().setTarget(gameObject.transform.parent.gameObject);
    }

    private Transform FindClosestHitObject(Ray ray, float distance, out Vector3 hitPoint)
    {
        var hits = Physics.RaycastAll(ray);

        Transform closestHit = null;
        hitPoint = Vector3.zero;

        foreach (var hit in hits)
            if (hit.transform != transform && (closestHit == null || hit.distance < distance))
            {
                closestHit = hit.transform;
                distance = hit.distance;
                hitPoint = hit.point;
            }

        return closestHit;
    }
}