using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Windows.WebCam;
using TMPro;
using System;
using System.IO;
using System.Diagnostics;
using System.Linq;

public class PhotographingSceneManager : MonoBehaviour
{
    [SerializeField] Button shootButton, autoUIButton, yesButton, noButton;
    [SerializeField] TMP_Text numberOfShotsText, remainingTimeText;
    [SerializeField] RawImage cameraDisplay;
    // cameraDisplay를 적절하게 가려서 사용자에게 알맞은 UI가 보여지는 효과를 주는 가림막
    [SerializeField] Image cameraCover;
    // 자동촬영 안내 UI
    [SerializeField] GameObject autoModeUI;
    AudioSource audioSource;
    WebCamTexture webCamTexture;
    VideoCapture m_VideoCapture = null;
    List<AudioClip> soundEffectList = new List<AudioClip> {};
    // 사람만 촬영, 반려견 자동 촬영시 타이머에 의한 사진 촬영 진행을 저장할 코루틴
    Coroutine coroutine;

    // 현재 날짜 및 시간을 형식에 맞게 저장
    string date = System.DateTime.Now.ToString(("yyyy/MM/dd/HH/mm"));
    // 사진 및 동영상 저장 경로
    string savePath = "";
    // 사진 및 동영상 저장 폴더 경로
    string directoryPath = "";

    // 현재 촬영 진행중인 사진의 번호
    int captureCounter = 1;
    // 남은 제한시간
    int remainingTime = 70;
    // 초기 제한시간
    int resetTime = 10;

    int numberOfFrames = 4;
    int photoWidth = 700;
    int photoHeight = 980;
    int photographPattern = 1;
    int cameraDisplayWidth = 2560;
    int cameraDisplayHeight = 1440;
    float magnification = 1;
    int photographCode = 12;
    string photographType = "pet";
    string shootingMode = "auto";
    // 현재 촬영중임을 나타내는 변수, Critical Section
    bool isShooting = false;

    void Start()
    {
        savePath = @"C:\TestPhoto\" + date + @"\";
        directoryPath = @"C:\TestPhoto\" + date;

        // 저장 폴더 체크 및 경로 설정, UI 설정, DataModel에서 정보 불러오기 및 정보 저장
        RouteAndDisplaySetting();
        try
        {
            // 화면 표시 및 사진 촬영용 카메라를 연결한다
            CameraConnection();
        }
        catch (Exception e)
        {
            // 에러가 발생할 경우 재촬영 안내 Scene으로 이동한다
            UnityEngine.Debug.LogError("사진 촬영 카메라 에러: " + e.Message);
            // 재촬영 안내 Scene으로 이동한다
            SceneManager.LoadScene("CameraErrorScene");
        }

        // 테스트를 위한 버튼으로 실제로는 리모컨에 등록
        shootButton.onClick.AddListener( delegate { TakeSnapshot(); } );

        try
        {
            // 영상 촬영을 시작한다
            VideoCapture.CreateAsync(false, OnVideoCaptureCreated);
        }
        catch (Exception e)
        {
            // 에러가 발생할 경우 재촬영 안내 Scene으로 이동한다
            UnityEngine.Debug.LogError("타입랩스 촬영 에러: " + e.Message);
            ErrorInducedReShooting();
        }

        audioSource = GetComponent<AudioSource>();

        // 자동 촬영 전환 버튼을 설정한다
        SetAutoUIButton();
        // 타이머를 시작한다
        StartCountDown();
        // 효과음을 재생을 시작한다
        StartCoroutine(EffectSoundPlayback());
    }

    // 0번에 연결된 카메라를 연결하여 영상 촬영을 시작한다
    void OnVideoCaptureCreated(VideoCapture videoCapture)
    {
        if (videoCapture != null)
        {
            m_VideoCapture = videoCapture;

            Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();

            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.frameRate = 30;
            UnityEngine.Debug.Log("Video FPS: " + cameraFramerate);
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

            m_VideoCapture.StartVideoModeAsync(cameraParameters,
                                                VideoCapture.AudioState.None,
                                                OnStartedVideoCaptureMode);
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to create VideoCapture Instance!");
        }
    }

    // 저장 경로와 영상 제목을 지정한다
    void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        if (result.success)
        {
            string filename = string.Format(PlayerPrefs.GetString("StoreNumber") + date + ".mp4");
            string filepath = System.IO.Path.Combine(savePath, filename);

            m_VideoCapture.StartRecordingAsync(filepath, OnStartedRecordingVideo);
        }
    }

    // 영상 촬영을 시작한다
    void OnStartedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        UnityEngine.Debug.Log("Started Recording Video!");
    }

    // 영상 촬영을 중지한다
    void StopRecordingVideo()
    {
        m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
    }

    // 영상 촬영을 중지한다
    void OnStoppedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
    }

    // 촬영된 영상을 저장하고 카메라를 제거한다
    void OnStoppedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        m_VideoCapture.Dispose();
        m_VideoCapture = null;
        UnityEngine.Debug.Log("Stopped Recording Video!");
    }

    // 화면 표시 및 사진 촬영용 카메라를 등록한다
    void CameraConnection()
    {
        // 현재 등록된 카메라가 있다면 제거한다
        if (webCamTexture != null)
        {
            cameraDisplay.texture = null;
            webCamTexture.Stop();
            webCamTexture = null;
        }
        // 1번에 연결된 카메라를 연결한다 (0번은 영상 촬영)
        WebCamDevice device = WebCamTexture.devices[0];
        UnityEngine.Debug.Log("Shoot camera name: " + device.name);
        webCamTexture = new WebCamTexture(device.name);
        cameraDisplay.texture = webCamTexture;
        webCamTexture.Play();
        UnityEngine.Debug.Log("Photo width: " + webCamTexture.width + ", Photo height: " + webCamTexture.height);
    }

    // 저장 폴더 체크 및 경로 설정, UI 설정, DataModel에서 정보 불러오기 및 정보 저장
    void RouteAndDisplaySetting()
    {
        // 메인 저장 폴더가 있는지 확인 후 없다면 폴더 생성(컴퓨터별 최초 실행시 생성)
        if (!Directory.Exists(@"C:\TestPhoto"))
        {
            Directory.CreateDirectory(@"C:\TestPhoto");
        }
        // 저장 폴더가 있는지 확인 후 없다면 폴더 생성(프로그램 사이클마다 실행)
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (DataModel.instance != null)
        {
            // DataModel에 저장 경로 및 날짜를 저장한다
            DataModel.instance.savePath = savePath;
            DataModel.instance.date = date;
            // DataModel로부터 사진 유형, 컷수, 촬영방법, 효과음, 확대 배율을 불러온다
            photographType = DataModel.instance.photographType;
            numberOfFrames = DataModel.instance.numberOfFrames;
            shootingMode = DataModel.instance.shootingMode;
            soundEffectList = DataModel.instance.soundEffectList;
            photographPattern = DataModel.instance.photographPattern;

            // 컷수에 따라 사진 크기 및 가림막을 설정한다
            // 컷수, 사진 유형(반려견, 사람)에 따라 촬영할 사진 수를 설정한다
            // 촬영 방법에 따라 제한시간을 설정한다
            if (numberOfFrames == 1)
            {
                if (photographType == "pet")
                {
                    photographCode = 10;
                    if (shootingMode == "auto")
                    {   
                        resetTime = 5;
                    }
                    else
                    {
                        resetTime = 70;
                    }
                }
                else
                {
                    resetTime = 10;
                    photographCode = 8;
                }
                photoWidth = 706;
                photoHeight = 980;
                cameraCover.sprite = Resources.Load<Sprite>("Images/CameraCover1");
            }
            else if (numberOfFrames == 2)
            {
                if (photographType == "pet")
                {
                    photographCode = 10;
                    if (shootingMode == "auto")
                    {   
                        resetTime = 5;
                    }
                    else
                    {
                        resetTime = 70;
                    }
                }
                else
                {
                    resetTime = 10;
                    photographCode = 8;
                }
                photoWidth = 899;
                photoHeight = 980;
                cameraCover.sprite = Resources.Load<Sprite>("Images/CameraCover2");
            }
            else if (numberOfFrames == 4)
            {
                if (photographType == "pet")
                {
                    photographCode = 12;
                    if (shootingMode == "auto")
                    {   
                        resetTime = 5;
                    }
                    else
                    {
                        resetTime = 80;
                    }
                }
                else
                {
                    resetTime = 10;
                    photographCode = 10;
                }
                photoWidth = 700;
                photoHeight = 980;
                cameraCover.sprite = Resources.Load<Sprite>("Images/CameraCover3");
            }
            else if (numberOfFrames == 6)
            {
                if (photographType == "pet")
                {
                    photographCode = 12;
                    if (shootingMode == "auto")
                    {   
                        resetTime = 5;
                    }
                    else
                    {
                        resetTime = 80;
                    }
                }
                else
                {
                    resetTime = 10;
                    photographCode = 10;
                }
                photoWidth = 1069;
                photoHeight = 980;
                cameraCover.sprite = Resources.Load<Sprite>("Images/CameraCover4");
            }
            // DataModel에 촬영할 사진 수를 저장한다
            DataModel.instance.photographCode = photographCode;

            // 확대 배율에 따라 카메라 화면 크기와 사진 크롭 배율을 설정한다
            if (photographPattern == 0)
            {
                cameraDisplayWidth = 1920;
                cameraDisplayHeight = 1080;
                magnification = 1.3333f;
            }
            else if (photographPattern == 1)
            {
                cameraDisplayWidth = 2560;
                cameraDisplayHeight = 1440;
                magnification = 1;
            }
            else if (photographPattern == 2)
            {
                cameraDisplayWidth = 5120;
                cameraDisplayHeight = 2880;
                magnification = 0.5f;
            }
        }

        // 카메라 화면 크기를 변경한다
        RectTransform rectTransform = cameraDisplay.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(cameraDisplayWidth, cameraDisplayHeight);

        numberOfShotsText.text = photographCode.ToString() + " / " + photographCode.ToString();
    }

    // 자동 촬영 전환 버튼을 설정한다
    void SetAutoUIButton()
    {
        // 리모컨 촬영이라면 각 버튼에 알맞은 함수를 지정한다
        if (shootingMode == "remote")
        {
            // 자동 촬영 안내 UI를 표시한다
            autoUIButton.onClick.AddListener( delegate { autoModeUI.SetActive(true); } );
            // 자동 촬영으로 전환한다
            yesButton.onClick.AddListener( delegate { StartAutoMode(); } );
            // 자동 촬영 안내 UI를 숨긴다
            noButton.onClick.AddListener( delegate { autoModeUI.SetActive(false); } );
        }
        // 리모컨 촬영이 아니라면 버튼을 제거한다
        else
        {
            autoUIButton.gameObject.SetActive(false);
        }
        // 자동 촬영 안내 UI를 숨긴다
        autoModeUI.SetActive(false);
    }

    // 자동 촬영으로 전환한다
    void StartAutoMode()
    {
        if (DataModel.instance != null)
        {
            // DataModel에 저장된 촬영 방법을 변경한다
            DataModel.instance.shootingMode = "auto";

            // 연결된 카메라를 제거하고 진행중인 영상 촬영을 중단한다
            OnDestroyWebCamTexture();
            StopRecordingVideo();
            try
            {
                // 저장된 동영상을 삭제한다
                File.Delete(savePath + PlayerPrefs.GetString("StoreNumber") + date + ".mp4");
                UnityEngine.Debug.Log("동영상이 성공적으로 삭제되었습니다.");
            }
            catch (IOException e)
            {
                // 동영상 삭제 중 오류가 발생한 경우 예외 처리
                UnityEngine.Debug.LogError("동영상 삭제 중 오류 발생: " + e.Message);
            }

            // 촬영 안내 화면으로 이동한다
            SceneManager.LoadScene("PhotographyInformationScene");
        }
    }

    // 사진을 촬영한다
    void TakeSnapshot()
    {
        // Critical Section 및 타이머 일시 중지
        if (!isShooting)
        {
            isShooting = true;

            // 자동 촬영을 중지시킨다
            if (photographType == "people" || shootingMode == "auto")
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }

            // 현재 카메라 화면을 저장한다
            Texture2D cameraSnap = new Texture2D(webCamTexture.width, webCamTexture.height);
            cameraSnap.SetPixels(webCamTexture.GetPixels());
            cameraSnap.Apply();

            // 확대 배율에 따라 저장할 사진 크기를 지정한다
            int magnificationWidth = (int)(photoWidth * magnification);
            int magnificationHeight = (int)(photoHeight * magnification);

            // 현재 카메라 화면에서 지정한 사진 크기만큼 가져온다
            Texture2D snap = new Texture2D(magnificationWidth, magnificationHeight);
            int yStart = (webCamTexture.height - magnificationHeight) / 2;
            int xStart = (webCamTexture.width - magnificationWidth) / 2;
            for (int y = yStart; y < yStart + magnificationHeight; y++)
            {
                for (int x = xStart; x < xStart + magnificationWidth; x++)
                {
                    Color pixelColor = cameraSnap.GetPixel(x, y);
                    snap.SetPixel(x - xStart, y - yStart, pixelColor);
                }
            }
            snap.Apply();

            // 가져온 사진을 저장 경로에 저장한다
            System.IO.File.WriteAllBytes(savePath + captureCounter.ToString() + ".png", snap.EncodeToPNG());

            // 촬영 횟수를 검사한다
            if (captureCounter > photographCode - 1)
            {
                // 연결된 카메라를 제거하고 영상을 저장한 후 다음 Scene으로 이동한다
                OnDestroyWebCamTexture();
                StopRecordingVideo();
                SceneManager.LoadScene("PhotoLoadingScene");
            }
            else
            {
                // 현재 촬영 횟수를 표시한다
                numberOfShotsText.text = (photographCode - captureCounter).ToString() + " / " + photographCode.ToString();
                captureCounter++;
                // 자동 촬영을 시작한다
                if (photographType == "people" || shootingMode == "auto")
                {
                    coroutine = StartCoroutine(CountDownAutoMode(remainingTimeText));
                }
            }

            // Critical Section 및 타이머 재시작
            isShooting = false;
        }
    }

    // 연결된 카메라를 제거한다 (화면 표시 및 사진 촬영 카메라)
    void OnDestroyWebCamTexture()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
            webCamTexture = null;
        }
    }

    // 촬영 방법에 따라 CountDown을 실행한다
    void StartCountDown()
    {
        if (photographType == "people" || shootingMode == "auto")
        {
            coroutine = StartCoroutine(CountDownAutoMode(remainingTimeText));
        }
        else
        {
            coroutine = StartCoroutine(CountDownRemoteMode(remainingTimeText));
        }
    }

    // 자동 촬영에서의 시간제한
    IEnumerator CountDownAutoMode(TMP_Text remainingTimeText)
    {
        // 남은 시간을 초기 제한 시간으로 설정한다
        remainingTime = resetTime;
        // 남은 시간을 1초단위로 표시한다
        remainingTimeText.text = remainingTime.ToString() + " 초";
        while (remainingTime >= 0)
        {
            remainingTimeText.text = remainingTime.ToString() + " 초";
            remainingTime -= 1;
            yield return new WaitForSeconds(1f);
        }
        // 사진을 촬영한다
        TakeSnapshot();
    }

    // 리모컨 촬영에서의 시간제한
    IEnumerator CountDownRemoteMode(TMP_Text remainingTimeText)
    {
        // 남은 시간을 초기 제한 시간으로 설정한다
        remainingTime = resetTime;
        // 남은 시간을 1초단위로 표시한다
        remainingTimeText.text = remainingTime.ToString() + " 초";
        while (remainingTime >= 0)
        {
            // 촬영중이라면 타이머를 정지시킨다
            if (!isShooting)
            {
                remainingTimeText.text = remainingTime.ToString() + " 초";
                remainingTime -= 1;
            }
            yield return new WaitForSeconds(1f);
        }

        // 자동 촬영 전환을 금지시킨다
        autoModeUI.SetActive(false);
        autoUIButton.gameObject.SetActive(false);

        // 2초마다 촬영하여 사진 촬영 횟수를 채운다
        while (true)
        {
            TakeSnapshot();
            yield return new WaitForSeconds(2f);
        }
    }

    void ErrorInducedReShooting()
    {
        // 연결된 카메라를 제거하고 진행중인 영상 촬영을 중단한다
        OnDestroyWebCamTexture();
        StopRecordingVideo();
        try
        {
            // 저장된 동영상을 삭제한다
            File.Delete(savePath + PlayerPrefs.GetString("StoreNumber") + date + ".mp4");
            UnityEngine.Debug.Log("동영상이 성공적으로 삭제되었습니다.");
        }
        catch (IOException e)
        {
            // 동영상 삭제 중 오류가 발생한 경우 예외 처리
            UnityEngine.Debug.LogError("동영상 삭제 중 오류 발생: " + e.Message);
        }

        // 재촬영 안내 Scene으로 이동한다
        SceneManager.LoadScene("CameraErrorScene");
    }

    IEnumerator EffectSoundPlayback()
    {
        while (soundEffectList.Count > 0)
        {
            for (int i = 0; i < soundEffectList.Count; i++)
            {
                audioSource.clip = soundEffectList[i];
                audioSource.Play();
                yield return new WaitForSeconds(audioSource.clip.length);
                audioSource.Stop();
            }
        }
    }
}