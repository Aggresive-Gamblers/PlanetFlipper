using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Range(.1f,1f)]
    public float followDamping;
    public Transform playerTransform;

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, playerTransform.rotation, 1/followDamping * Time.fixedDeltaTime);
        transform.position = Vector3.Lerp(transform.position, playerTransform.position, 1 / followDamping * Time.fixedDeltaTime);
    }

}
