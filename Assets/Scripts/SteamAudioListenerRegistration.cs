
using System.Collections;
using UnityEngine;
using SteamAudio;

public class SteamAudioListenerRegistration : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Ждём завершения инициализации сцены
        yield return null;

        // Регистрируем активный Listener
        SteamAudioManager.NotifyAudioListenerChangedTo(transform);

        Debug.Log("[Steam Audio] Listener registered.");
    }
}
