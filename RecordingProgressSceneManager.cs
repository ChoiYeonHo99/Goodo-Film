using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RecordingProgressSceneManager : MonoBehaviour
{
    // 기본 제한시간은 30초이다
    int remainingTime = 30;
    [SerializeField] Button previousButton, nextButton;
    [SerializeField] TMP_Text remainingTimeText;
    [SerializeField] Button startButton, playButton, stopButton, restartButton;
    [SerializeField] Image recordingProgressImage;
    [SerializeField] TMP_Text audioClipTimeText;

    bool isRecording = false;
    AudioSource recordedSource;
    string[] microphoneDevices;
    string selectedMicrophone;
    int minFrequency, maxFrequency;
    float audioLength = 4f;

    Coroutine recordCoroutine, audioCoroutine;
    Sprite originalNextButtonSprite;

    void Start()
    {
        // 입력거부상태의 버튼 이미지를 저장해둔다
        originalNextButtonSprite = nextButton.image.sprite;
        // 제한시간을 설정한다
        StartCoroutine(CountDown(remainingTimeText));
        // 녹음 상황을 초기화한다
        RestartRecording();
        // 각 버튼에 알맞은 함수들을 할당한다
        startButton.onClick.AddListener( delegate { recordCoroutine = StartCoroutine(StartRecording()); } );
        playButton.onClick.AddListener( delegate { audioCoroutine = StartCoroutine(PlayAudioClip()); } );
        stopButton.onClick.AddListener( delegate { StopAudioClip(); } );
        restartButton.onClick.AddListener( delegate { RestartRecording(); } );
        nextButton.onClick.AddListener( delegate { NextScene(); } );
        previousButton.onClick.AddListener( delegate { SceneManager.LoadScene("RecordingStatusScene"); } );

        // 연결된 마이크를 불러온다
        recordedSource = GetComponent<AudioSource>();
        microphoneDevices = Microphone.devices;
        if (microphoneDevices.Length == 0)
        {
            // 연결된 마이크가 없다면 녹음을 취소하고 다음 Scene으로 이동한다
            Debug.Log("No microphone devices found.");
            CancelRecording();
            NextScene();
        }
        else
        {
            // 연결된 마이크의 이름과 정보를 저장한다
            selectedMicrophone = microphoneDevices[0];
            Microphone.GetDeviceCaps(selectedMicrophone, out minFrequency, out maxFrequency);
            Debug.Log("Microphone initialized: " + selectedMicrophone);
        }
    }

    // 진행중인 녹음을 취소하고 초기 상태로 돌아간다
    void RestartRecording()
    {
        if (recordCoroutine != null)
        {
            StopCoroutine(recordCoroutine);
            recordCoroutine = null;
            StopRecording();
        }
        startButton.gameObject.SetActive(true);
        playButton.gameObject.SetActive(false);
        recordingProgressImage.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(false);
        audioClipTimeText.text = "00 : 00";

        // nextButton의 입력을 거부하고 Sprite를 거부상태로 변경한다
        nextButton.interactable = false;
        nextButton.image.sprite = originalNextButtonSprite;
    }

    // 녹음을 시작한다
    IEnumerator StartRecording()
    {
        Debug.Log("StartRecording");
        // 녹음중임을 나타내는 isRecording을 true로 전환한다
        isRecording = true;
        // 연결된 마이크 이름과 정보를 이용하여 녹음을 시작한다
        recordedSource.clip = Microphone.Start(selectedMicrophone, true, (int)audioLength, maxFrequency);
        startButton.gameObject.SetActive(false);
        recordingProgressImage.gameObject.SetActive(true);
        // 녹음 진행 시간을 1초단위로 나타낸다
        for (int i = 0; i < audioLength; i++)
        {
            audioClipTimeText.text = "00 : 0" + i.ToString();
            yield return new WaitForSeconds(1);
        }
        audioClipTimeText.text = "00 : 04";
        // 4초가 지나면 녹음을 중지한다
        StopRecording();
    }

    // 녹음을 중지한다
    void StopRecording()
    {
        Debug.Log("StopRecording");
        // 현재 녹음중일 때만 동작한다
        if (isRecording)
        {
            recordingProgressImage.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
            // 녹음중임을 나타내는 isRecording을 false로 전환한다
            isRecording = false;
            // 연결된 마이크의 녹음을 중지한다
            Microphone.End(selectedMicrophone);

            // nextButton의 입력을 허용하고 Sprite를 허용상태로 변경한다
            nextButton.interactable = true;
            nextButton.image.sprite = Resources.Load<Sprite>("Images/SelectedNextButton");
        }
    }

    // 녹음한 파일을 재생한다
    IEnumerator PlayAudioClip()
    {
        recordedSource.Play();
        playButton.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(true);
        // 재생 시간을 초단위로 나타낸다
        for (int i = 0; i < audioLength; i++)
        {
            audioClipTimeText.text = "00 : 0" + i.ToString();
            yield return new WaitForSeconds(1);
        }
        audioClipTimeText.text = "00 : 04";
        // 재생이 끝나면 초기 상태로 돌아간다
        StopAudioClip();
    }

    // 재생중인 오디오를 중지한다
    void StopAudioClip()
    {
        if (audioCoroutine != null)
        {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }
        recordedSource.Stop();
        playButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
        audioClipTimeText.text = "00 : 04";
    }

    // 녹음한 파일을 DataModel에 저장하고 다음 Scene으로 이동한다
    void NextScene()
    {
        if (DataModel.instance != null)
        {
            DataModel.instance.recordedVoice = recordedSource.clip;
        }
        SceneManager.LoadScene("SoundEffectScene");
    }

    // 제한시간이 끝났을 때 호출되어 녹음 여부를 false로 바꾸고 초기 상태도 돌아간다
    void CancelRecording()
    {
        RestartRecording();
        if (DataModel.instance != null)
        {
            DataModel.instance.record = false;
            Debug.Log(DataModel.instance.record);
        }
    }

    // 남은 시간을 초단위로 표시해주고 제한시간이 끝나면 녹음을 취소하고 선택하고 다음 Scene으로 이동한다
    IEnumerator CountDown(TMP_Text remainingTimeText)
    {
        while (remainingTime >= 0)
        {
            remainingTimeText.text = remainingTime.ToString();
            remainingTime -= 1;
            yield return new WaitForSeconds(1f);
        }
        CancelRecording();
        NextScene();
    }
}