using UnityEngine;

public class interaction : MonoBehaviour
{
    [SerializeField] GameObject objectToActivate;

    void OnTriggerEnter2D( Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            objectToActivate.SetActive(true);
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            objectToActivate.SetActive(false);
        }
    }
}
