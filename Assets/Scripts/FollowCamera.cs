using UnityEngine;
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    void LateUpdate() { if (target) transform.position = new Vector3(target.position.x, target.position.y, -10); }
}
