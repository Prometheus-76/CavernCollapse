using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Darcy Matheson 2022

// Controls the ending scene
public class EndingManager : MonoBehaviour
{
    public EndingUI endingUI;
    public AttemptStats currentAttempt;

    public AudioSource soundEffectAudioSource;
    public AudioSource destructibleAudioSource;
    public AudioClip doorEntrySound;
    public AudioClip chestOpenSound;
    public KeepWhilePlaying destructibleAudioScript;

    public SpriteRenderer chestSprite;
    public Sprite openSprite;

    private bool timerRunning;
    private float sceneTimer;

    // Start is called before the first frame update
    void Start()
    {
        sceneTimer = 0f;
        timerRunning = true;
        currentAttempt.NewStage();
    }

    // Update is called once per frame
    void Update()
    {
        // Run scene timer
        if (timerRunning) sceneTimer += Time.deltaTime;
        endingUI.sceneTimer = sceneTimer;
    }

    // Called by the player after interacting with the ending chest
    public IEnumerator RunComplete()
    {
        timerRunning = false;

        // Calculate and apply final score
        currentAttempt.totalTime += Mathf.FloorToInt(sceneTimer);

        if (currentAttempt.currentScore >= PlayerPrefs.GetInt("HighScore", 0)) PlayerPrefs.SetInt("HighScore", currentAttempt.currentScore);
        PlayerPrefs.Save();

        // Update chest sprite and play sound
        soundEffectAudioSource.PlayOneShot(chestOpenSound);
        chestSprite.sprite = openSprite;

        yield return new WaitForSeconds(1.5f);

        // Bring up run complete UI
        soundEffectAudioSource.PlayOneShot(doorEntrySound);

        endingUI.RunCompleteUI();

        yield return null;
    }

    // Returns the player to the main menu
    public void ReturnToMenu()
    {
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(1);

        destructibleAudioSource.Play();
        destructibleAudioScript.canBeDestroyed = true;

        SceneManager.LoadScene(0);
    }
}
