using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoViewer : MonoBehaviour
{
    [SerializeField] PhotoAndFrameSceneManager manager;
    public Texture2D originalTexture = null;
    public Texture2D grayTexture = null;
    public int index;
    public RawImage rawImage;

    void Start()
    {
        rawImage = GetComponent<RawImage>();

        // EventTrigger 컴포넌트를 추가합니다
        EventTrigger trigger = rawImage.gameObject.AddComponent<EventTrigger>();
        // 클릭 이벤트에 대한 콜백 함수를 등록합니다
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        // 클릭 시 실행할 함수 지정
        entry.callback.AddListener((eventData) => RemovePhotograph());
        trigger.triggers.Add(entry);
    }

    // 선택한 사진 제거는 PhotoViewer를 터치하여 시작한다
    void RemovePhotograph()
    {
        // 현재 PhtoViewer가 비어있거나 사진선택단계가 아니라면 종료한다
        if (originalTexture == null || manager.selectMode == false)
        {
            return;
        }

        // PhtoViewer를 비우고 Texture의 소유자(Photograph)에게 Remove를 알린다
        originalTexture = null;
        grayTexture = null;
        rawImage.texture = null;
        manager.RemovePhoto(index);
    }

    // originalTexture를 표시한다
    public void ShowImage()
    {
        rawImage.texture = originalTexture;
    }

    // originalTexture를 표시한다
    public void ApplyColorFilter()
    {
        rawImage.texture = originalTexture;
    }

    // gartTexture를 표시한다
    public void ApplyGrayFilter()
    {
        rawImage.texture = grayTexture;
    }
}