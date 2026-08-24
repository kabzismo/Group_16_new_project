using FPSStarter;
using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    public string Prompt => "[E] Interact";

    public void Interact(GameObject player)
    {
        Debug.Log("Player interacted with this object.");
    }
}