using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public IEnumerator RunComplete()
    {
        timerRunning = false;

        // Calculate and apply final score
        currentAttempt.totalTime += Mathf.FloorToInt(sceneTimer);

        if (currentAttempt.currentScore >= PlayerPrefs.GetInt("HighScore", 0)) PlayerPrefs.SetInt("HighScore", currentAttempt.currentScore);
        PlayerPrefs.Save();

        soundEffectAudioSource.PlayOneShot(chestOpenSound);
        chestSprite.sprite = openSprite;

        yield return new WaitForSeconds(1.5f);

        soundEffectAudioSource.PlayOneShot(doorEntrySound);

        endingUI.RunCompleteUI();

        yield return null;
    }

    public void ReturnToMenu()
    {
        if (MusicPlayer.GetInstance() != null)
            MusicPlayer.GetInstance().CrossFade(1);

        destructibleAudioSource.Play();
        destructibleAudioScript.canBeDestroyed = true;

        SceneManager.LoadScene(0);
    }
}
