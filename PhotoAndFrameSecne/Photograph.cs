using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Photograph : MonoBehaviour
{
    [SerializeField] PhotoAndFrameSceneManager manager;
    [SerializeField] TMP_Text numText;
    public Texture2D originalTexture;
    public Texture2D grayTexture;
    public int index;

    RawImage rawImage;
    int numOfPhoto;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();

        // EventTrigger 컴포넌트를 추가합니다
        EventTrigger trigger = rawImage.gameObject.AddComponent<EventTrigger>();
        // 클릭 이벤트에 대한 콜백 함수를 등록합니다
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        // 클릭 시 실행할 함수 지정
        entry.callback.AddListener((eventData) => SelectPhotograph());
        trigger.triggers.Add(entry);
    }

    // 사진 선택은 Photograph를 터치하여 시작한다
    void SelectPhotograph()
    {
        // 현재 Select된 Photo의 수에 따라 가능성을 받는다
        bool result = manager.AddPhoto(index);
        if (result)
        {
            // 현재 Photograph의 texture가 몇개가 포함되어 있는지 검사하고 text로 표시한다
            numOfPhoto = manager.InformNumOfPhoto(index);
            numText.text = numOfPhoto.ToString();
        }
    }

    // originalTexture를 표시한다
    public void ShowImage()
    {
        rawImage.texture = originalTexture;
    }

    // grayTexture를 생성한다
    public void CreateGrayRawImage()
    {
        grayTexture = new Texture2D(originalTexture.width, originalTexture.height);

        // 각 픽셀을 순회하며 흑백으로 변환
        for (int y = 0; y < originalTexture.height; y++)
        {
            for (int x = 0; x < originalTexture.width; x++)
            {
                Color pixelColor = originalTexture.GetPixel(x, y);
                float grayscale = pixelColor.grayscale;
                Color newColor = new Color(grayscale, grayscale, grayscale, pixelColor.a);
                grayTexture.SetPixel(x, y, newColor);
            }
        }

        // Texture 변경사항을 적용
        grayTexture.Apply();
    }

    // 현재 Photograph의 texture가 몇개가 포함되어 있는지 통보받고 text로 표시한다
    public void GetNumOfPhoto(int num)
    {
        numOfPhoto = num;
        numText.text = numOfPhoto.ToString();
    }
}