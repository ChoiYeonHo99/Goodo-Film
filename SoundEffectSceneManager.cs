using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SoundEffectSceneManager : MonoBehaviour
{
    // 기본 제한시간은 30초이다
    int remainingTime = 30;
    [SerializeField] Button previousButton, nextButton;
    [SerializeField] TMP_Text remainingTimeText;
    // index = 0은 voice button이다
    [SerializeField] List<Button> soundButtonList;
    [SerializeField] List<Image> blueScreenList;
    [SerializeField] Toggle allSelectToggle;
    [SerializeField] TMP_Text voiceText;
    // index = 0은 null이며 1~5에는 효과음이 저장되어 있다
    [SerializeField] List<AudioClip> audioClipList;
    List<AudioClip> selectedSoundEffectList = new List<AudioClip> {};
    List<int> selectSequenceList = new List<int> {};
    // 각 index의 음성들의 선택상태를 나타낸다
    List<bool> selectList = new List<bool> {false, false, false, false, false, false};

    Color white = new Color(1, 1, 1);
    Color blue = new Color(57/255f, 87/255f, 122/255f);
    AudioSource audioSource;

    void Start()
    {
        // 화면을 구성한다
        ScreenConfigurationSetting();
        // 제한시간을 설정한다
        StartCoroutine(CountDown(remainingTimeText));

        audioSource = GetComponent<AudioSource>();
        previousButton.onClick.AddListener( delegate { SceneManager.LoadScene("RecordingStatusScene"); } );
        nextButton.onClick.AddListener( delegate { NextScene(); } );
        nextButton.image.sprite = Resources.Load<Sprite>("Images/SelectedNextButton");
    }

    // 화면을 구성한다
    void ScreenConfigurationSetting()
    {
        if (DataModel.instance != null)
        {
            // 음성 녹음 진행을 했다면 견주 음성 버튼에 함수를 등록한다
            if (DataModel.instance.record == true)
            {
                soundButtonList[0].onClick.AddListener( delegate { SelectSoundEffect(0); } );
            }
            // 음성 녹음을 진행하지 않았다면 견주 음성 버튼 텍스트를 투명하게 설정한다
            else
            {
                Color newColor = voiceText.color;
                newColor.a = 0.5f;
                voiceText.color = newColor;
            }
        }
        // 각 버튼에 소리 1~5를 알맞게 등록한다
        for (int i = 1; i < 6; i++)
        {
            int index = i;
            soundButtonList[i].onClick.AddListener( delegate { SelectSoundEffect(index); } );
        }
        // 전체 선택 / 해제 기능을 등록한다
        allSelectToggle.onValueChanged.AddListener( delegate { AllSelectSoundEffect(allSelectToggle.isOn); } );
    }

    // 음성 효과를 등록한다.
    void SelectSoundEffect(int index)
    {
        // 현재 선택한 음성이 이미 등록된 음성이라면 등록을 해제하고 시각적으로 나타낸다
        if (selectList[index])
        {
            audioSource.Stop();

            selectList[index] = false;
            blueScreenList[index].color = white;
            selectSequenceList.Remove(index);
        }
        // 현재 선택한 음성이 등록되지 않았다면 해당 음성을 재생한 후 등록한 다음 시각적으로 나타낸다
        else
        {
            // index가 0이면 견주 음성을, 1~5라면 Goodo 효과음을 재생한다
            if (index == 0)
            {
                audioSource.clip = DataModel.instance.recordedVoice;
            }
            else
            {
                audioSource.clip = audioClipList[index];
            }
            audioSource.Play();

            // 현재 음성을 등록하고 시각적으로 나타낸다
            selectList[index] = true;
            blueScreenList[index].color = blue;
            selectSequenceList.Add(index);
        }
    }

    // 음성 효과 전체를 선택하거나 해제합니다
    void AllSelectSoundEffect(bool check)
    {
        // 앞 단계에서 음성 녹음 진행 여부에 따라 탐색 범위를 바꿉니다
        int startIndex = 1;
        if (DataModel.instance != null && DataModel.instance.record)
        {
            startIndex = 0;
        }
        else
        {
            startIndex = 1;
        }

        // 선택 순서를 비웁니다
        selectSequenceList.Clear();
        // 전체 선택인지 해제인지를 구분합니다
        if (check)
        {
            // 모든 음성 효과를 선택하고 시각적으로 나타냅니다
            for (int i = startIndex; i < 6; i++)
            {
                selectList[i] = true;
                blueScreenList[i].color = blue;
                selectSequenceList.Add(i);
            }
        }
        else
        {
            // 모든 음성 효과를 해제하고 시각적으로 나타냅니다
            for (int i = startIndex; i < 6; i++)
            {
                selectList[i] = false;
                blueScreenList[i].color = white;
            }
        }
    }

    // 현재 선택된 음성 효과들을 저장한 후 다음 Scene으로 이동합니다
    void NextScene()
    {
        // 음성 효과들을 선택한 순서를 가져옵니다
        foreach (int i in selectSequenceList)
        {
            // selectedSoundEffectList에 해당 음성을 추가한다
            if (i == 0 && DataModel.instance != null)
            {
                selectedSoundEffectList.Add(DataModel.instance.recordedVoice);
            }
            else
            {
                selectedSoundEffectList.Add(audioClipList[i]);
            }
        }

        // 저장소에 현재 선택된 음성 효과들을 저장합니다
        if (DataModel.instance != null)
        {
            DataModel.instance.soundEffectList = selectedSoundEffectList;
        }
        // 다음 Scene으로 이동합니다.
        SceneManager.LoadScene("PhotographyInformationScene");
    }

    // 남은 시간을 초단위로 표시해주고 제한시간이 끝나면 선택된 효과음을 저장한 후 다음 Scene으로 이동합니다
    IEnumerator CountDown(TMP_Text remainingTimeText)
    {
        while (remainingTime >= 0)
        {
            remainingTimeText.text = remainingTime.ToString();
            remainingTime -= 1;
            yield return new WaitForSeconds(1f);
        }
        NextScene();
    }
}