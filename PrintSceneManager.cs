using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;

public class PrintSceneManager : MonoBehaviour
{
    // 반려견과 촬영시, 사람만 촬영시 나타낼 이미지를 등록한다
    [SerializeField] Sprite petScene, peopleScene;
    // 반려견과 촬영시, 사람만 촬영시 등록된 이미지를 나타낸다
    [SerializeField] UnityEngine.UI.Image sceneImage;
    // 저장된 타임랩스를 보여줄 화면
    [SerializeField] RawImage timelabseRawImage;

    VideoPlayer videoPlayer;
    RenderTexture renderTexture;

    // 사진과 영상이 저장된 경로
    string savePath;
    string date;
    // 사진 수량
    int photoQuantity = 2;

    void Start()
    {
        if (DataModel.instance != null)
        {
            // 저장 경로를 불러온다
            savePath = DataModel.instance.savePath;
            date = DataModel.instance.date;
            // 사진 수량을 불러온다
            photoQuantity = DataModel.instance.photoQuantity;

            // 촬영 유형에 따라 대기화면의 이미지를 바꾼다
            if (DataModel.instance.photographType == "pet")
            {
                sceneImage.sprite = petScene;
            }
            else
            {
                sceneImage.sprite = peopleScene;
            }
        }
        // 사진 인쇄
        // PrintPhoto();
        // 타임랩스 재생
        PlayTimeLabs();
        // DataModel 초기화 후 MainScene으로 이동
        StartCoroutine(Initialization());
    }

    // 저장된 사진을 인쇄한다
    void PrintPhoto()
    {
        // 프린터 생성
        PrintDocument pd = new PrintDocument();
        UnityEngine.Debug.Log("현재 기본 프린터: " + pd.PrinterSettings.PrinterName);
        UnityEngine.Debug.Log("최대 출력 가능 매수: " + pd.PrinterSettings.MaximumCopies);

        // 인쇄 작업 설정
        pd.PrintPage += new PrintPageEventHandler(PrintPage);

        // 인쇄 작업 시작
        try
        {
            // 사진 수량만큼 인쇄를 반복한다
            for (int i = 0; i < photoQuantity; i++)
            {
                pd.Print();
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("인쇄 중 오류 발생: " + ex.Message);
        }
    }

    void PrintPage(object sender, PrintPageEventArgs e)
    {
        // 인쇄할 이미지 불러오기
        System.Drawing.Image image = System.Drawing.Image.FromFile(savePath + PlayerPrefs.GetString("StoreNumber") + date + ".png");

        // 2컷 사진이면 90도 회전시키기
        if (DataModel.instance != null && DataModel.instance.numberOfFrames == 2)
        {
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }

        // 이미지를 프린터 페이지에 그리기
        e.Graphics.DrawImage(image, 0, 0, 412, 618);
        // 다음 페이지가 없음을 알림
        e.HasMorePages = false;
    }

    // 타임랩스를 재생하여 화면에 표시한다
    void PlayTimeLabs()
    {
        try
        {
            // RawImage에 표시할 RenderTexture 생성
            renderTexture = new RenderTexture(640, 480, 24);
            timelabseRawImage.texture = renderTexture;

            // VideoPlayer 컴포넌트 생성 및 설정
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.targetTexture = renderTexture;
            // 동영상 파일 경로 설정
            videoPlayer.url = savePath + PlayerPrefs.GetString("StoreNumber") + date + ".mp4";

            // 동영상 재생 속도 설정 (2배 속도)
            videoPlayer.playbackSpeed = 2.0f;

            // 동영상 재생 준비
            videoPlayer.Prepare();

            // 동영상 재생 준비 완료 시 이벤트 처리
            videoPlayer.prepareCompleted += (source) =>
            {
                // 동영상 재생 시작
                videoPlayer.Play();
            };
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("타임랩스 재생 오류: " + e.Message);
        }
    }

    // DataModel을 초기화하고 MainScene으로 이동한다
    IEnumerator Initialization()
    {
        // 프린트 완료까지 30초를 대기한다(이는 임시 코드이며 추후 프린트 완료를 추적해서 진행해야 한다)
        yield return new WaitForSeconds(30f);
        // DataModel을 초기화한다
        DataModel.instance.Initialization();
        // MainScene으로 이동한다
        SceneManager.LoadScene("MainScene");
    }
}