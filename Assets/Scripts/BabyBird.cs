using UnityEngine;

public class BabyBird : MonoBehaviour
{
    public float speed = 0.4f;
    public float rotation_damping = 4f;
    public Transform MamaBird;

    private float nextSearch;

    private void Update()
    {
        if (MamaBird == null)
        {
            if (Time.time < nextSearch)
                return;

            nextSearch = Time.time + 0.5f;
            GameObject[] mothers = GameObject.FindGameObjectsWithTag("MamaBird");
            if (mothers.Length == 0)
                return;
            MamaBird = mothers[Random.Range(0, mothers.Length)].transform;
        }

        Vector3 direction = MamaBird.position - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotation_damping);
        }
        transform.position = Vector3.MoveTowards(transform.position, MamaBird.position, speed * Time.deltaTime);
    }
}
