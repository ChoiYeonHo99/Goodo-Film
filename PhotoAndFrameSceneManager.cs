using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using ZXing;
using ZXing.QrCode;

public class PhotoAndFrameSceneManager : MonoBehaviour
{
    [SerializeField] RawImage frame;
    // 선택한 사진들을 보여주는 오브젝트 리스트
    [SerializeField] List<PhotoViewer> photoViewerList;
    // 촬영한 사진들을 보여주는 오브젝트 리스트
    [SerializeField] List<Photograph> photographList;
    // 프레임 선택 화면 구성 UI 오브젝트
    [SerializeField] GameObject baseFrame, eventFrame, text1, text2;
    // 프레임 선택 버튼 리스트
    [SerializeField] List<Button> baseFrameList, eventFrameList;

    [SerializeField] Button nextButton, printButton, baseButton, eventButton;
    [SerializeField] TMP_Text remainingTimeText;
    [SerializeField] Image frameButtonBackgroundImage, printWaitImage;
    [SerializeField] Button colorButton, grayButton, swapButton;
    [SerializeField] Sprite petScene, peopleScene;

    // 사진 선택 단계임을 나타내는 변수
    public bool selectMode = true;
    List<int> selectedIndexList = new List<int> {};
    Sprite originalNextButtonSprite;
    // 사진 선택 제한시간 30초, 프레임 선택 제한시간 30초
    int remainingTime = 30;

    string _colorCode = "01";
    string _typeCode = "BF";
    int frameCode = 1;
    int numberOfFrames = 1;
    List<float> photoXPosition;
    List<float> photoYPosition;
    float photoWidth;
    float photoHeight;

    int photographCode = 12;
    List<float> photographXPosition;
    List<float> photographYPosition;
    float photographWidth;
    float photographHeight;

    string photographPath;
    string photographType;
    string date;

    Color white = new Color(1, 1, 1);
    Color gray = new Color(215/255f, 215/255f, 219/255f);

    string responseText;
    Texture2D qrCode;
    bool qrCodeGenerationResult = false;
    bool isColor = true;

    // 최종 결과물 저장과 시간제한에 의한 종료가 겹치지 않게 관리
    bool CriticalSection = false;

    void Start()
    {
        // 촬영한 사진 목록을 불러오고 전체 화면을 배치한다
        SetPhotoViewerAndPhotoTaken();
        StartCoroutine(CountDown(remainingTimeText));
        nextButton.onClick.AddListener( delegate { EndSelectPhoto(); } );
        // 입력거부상태의 버튼 이미지를 저장해둔다.
        originalNextButtonSprite = nextButton.image.sprite;
        // nextButton의 입력을 거부하고 Sprite를 거부상태로 변경한다.
        nextButton.interactable = false;
        nextButton.image.sprite = originalNextButtonSprite;
        printButton.onClick.AddListener( delegate { StartCoroutine(SaveGoodoPhoto()); } );
        printButton.gameObject.SetActive(false);
    }

    // 촬영한 사진 목록을 불러오고 전체 화면을 배치한다
    void SetPhotoViewerAndPhotoTaken()
    {
        // 인쇄 대기화면을 비활성화한다
        printWaitImage.gameObject.SetActive(false);

        if (DataModel.instance != null)
        {
            // DataModel에서 컷수, 촬영 유형, 저장 경로, 촬영한 사진 수를 불러온다
            numberOfFrames = DataModel.instance.numberOfFrames;
            photographType = DataModel.instance.photographType;
            photographPath = DataModel.instance.savePath;
            photographCode = DataModel.instance.photographCode;
            date = DataModel.instance.date;

            // 컷수에 따라 프레임 코드, 선택한 사진들을 보여줄 오브젝트 크기 및 위치
            // 촬영한 사진들을 보여줄 RawImage들의 크기와 위치를 지정한다
            if (numberOfFrames == 1)
            {
                frameCode = 1;
                photoWidth = 540;
                photoHeight = 750;
                photoXPosition = new List<float> {210};
                photoYPosition = new List<float> {155};

                photographWidth = 186;
                photographHeight = 258;
                photographXPosition = new List<float> {861, 1048, 1235, 1422, 861, 1048, 1235, 1422, 861, 1048};
                photographYPosition = new List<float> {152, 152, 152, 152, 411, 411, 411, 411, 670, 670};
            }
            else if (numberOfFrames == 2)
            {
                frameCode = 2;
                photoWidth = 275;
                photoHeight = 300;
                photoXPosition = new List<float> {200, 485};
                photoYPosition = new List<float> {376, 376};

                // 컷수가 2일 때만 전체 프레임의 크기와 위치를 조정한다
                RectTransform rectTransform = frame.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(180, -333);
                rectTransform.sizeDelta = new Vector2(600, 400);

                photographWidth = 186;
                photographHeight = 203;
                photographXPosition = new List<float> {861, 1048, 1235, 1422, 861, 1048, 1235, 1422, 861, 1048};
                photographYPosition = new List<float> {234, 234, 234, 234, 438, 438, 438, 438, 642, 642};
            }
            else if (numberOfFrames == 4)
            {
                frameCode = 3;
                photoWidth = 263;
                photoHeight = 368;
                photoXPosition = new List<float> {210, 487, 210, 487};
                photoYPosition = new List<float> {155, 155, 537, 537};

                photographWidth = 186;
                photographHeight = 260;
                photographXPosition = new List<float> {861, 1048, 1235, 1422, 861, 1048, 1235, 1422, 861, 1048, 1235, 1422};
                photographYPosition = new List<float> {149, 149, 149, 149, 410, 410, 410, 410, 671, 671, 671, 671};
            }
            else if (numberOfFrames == 6)
            {
                frameCode = 4;
                photoWidth = 263;
                photoHeight = 241;
                photoXPosition = new List<float> {210, 487, 210, 487, 210, 487};
                photoYPosition = new List<float> {155, 155, 410, 410, 665, 665};

                photographWidth = 186;
                photographHeight = 170;
                photographXPosition = new List<float> {861, 1048, 1235, 1422, 861, 1048, 1235, 1422, 861, 1048, 1235, 1422};
                photographYPosition = new List<float> {284, 284, 284, 284, 455, 455, 455, 455, 626, 626, 626, 626};
            }

            // 선택한 사진들을 보여줄 photoViewer들의 크기와 위치를 조정한다
            for (int i = 0; i < numberOfFrames; i++)
            {
                RectTransform rectTransform = photoViewerList[i].gameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(photoXPosition[i], -1 * photoYPosition[i]);
                rectTransform.sizeDelta = new Vector2(photoWidth, photoHeight);
            }
            // 사용하지 않는 photoViewer들을 비활성화한다
            for (int i = numberOfFrames; i < 6; i++)
            {
                photoViewerList[i].gameObject.gameObject.SetActive(false);
            }

            // 촬영한 사진들을 보여줄 Photograph들의 index를 할당하고 크기와 위치를 조정한다
            for (int i = 0; i < photographCode; i++)
            {
                int index = i;
                photographList[i].index = index;

                RectTransform rectTransform = photographList[i].GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(photographXPosition[i], -1 * photographYPosition[i]);
                rectTransform.sizeDelta = new Vector2(photographWidth, photographHeight);

                // 촬영한 사진을 불러와서 RawImage에 등록한다
                byte[] byteTexture = System.IO.File.ReadAllBytes(photographPath + (i+1).ToString() + ".png");
                if (byteTexture.Length > 0)
                {
                    Texture2D texture = new Texture2D(0, 0);
                    texture.LoadImage(byteTexture);
                    photographList[i].originalTexture = texture;
                    photographList[i].ShowImage();
                    photographList[i].CreateGrayRawImage();
                }
            }
            // 사용하지 않는 RawImage들을 비활성화한다
            for (int i = photographCode; i < 12; i++)
            {
                photographList[i].gameObject.SetActive(false);
            }

            // 프레임 코드에 맞는 기본 프레임을 불러와서 보여준다
            frame.texture = Resources.Load<Texture2D>("Frame/Show/Base/BF" + frameCode.ToString() + "_01");

            // 촬영 유형에 따라 인쇄 대기 화면의 이미지를 지정한다
            if (DataModel.instance.photographType == "pet")
            {
                printWaitImage.sprite = petScene;
            }
            else
            {
                printWaitImage.sprite = peopleScene;
            }
        }

        // 프레임 선택 버튼들에 알맞은 함수들을 지정한 후 비활성화한다
        for (int i = 0; i < 12; i++)
        {
            int colorCode = i+1;
            baseFrameList[i].onClick.AddListener( delegate { ChangeFrame("BF", colorCode); } );
            eventFrameList[i].onClick.AddListener( delegate { ChangeFrame("EF", colorCode); } );
        }
        baseFrame.SetActive(false);
        eventFrame.SetActive(false);

        // 각 버튼들에 알맞은 함수를 지정하고 프레임 선택 단계에 해당하는 오브젝드들은 비활성화한다
        baseButton.onClick.AddListener( delegate { ChangeFrameType(true); } );
        eventButton.onClick.AddListener( delegate { ChangeFrameType(false); } );
        baseButton.gameObject.SetActive(false);
        eventButton.gameObject.SetActive(false);
        frameButtonBackgroundImage.gameObject.SetActive(false);
        colorButton.onClick.AddListener( delegate { ColorFilter(); } );
        grayButton.onClick.AddListener( delegate { GrayFilter(); } );
        swapButton.onClick.AddListener( delegate { Swap(); } );
        colorButton.gameObject.SetActive(false);
        grayButton.gameObject.SetActive(false);
        swapButton.gameObject.SetActive(false);
        text1.SetActive(false);
        text2.SetActive(false);
    }

    // 기본 프레임 리스트와 이벤트 프레임 리스트를 바꿔주는 함수
    void ChangeFrameType(bool type)
    {
        if (type)
        {
            baseFrame.SetActive(true);
            eventFrame.SetActive(false);
            baseButton.image.color = gray;
            eventButton.image.color = white;
        }
        else
        {
            baseFrame.SetActive(false);
            eventFrame.SetActive(true);
            baseButton.image.color = white;
            eventButton.image.color = gray;
        }
    }

    // 선택한 번호에 맞는 프레임을 불러오는 함수
    void ChangeFrame(string typeCode, int colorCode)
    {
        _typeCode = typeCode;
        // 번호를 2글자 형식으로 수정한다.
        if (colorCode < 10)
        {
            _colorCode = "0" + colorCode.ToString();
        }
        else
        {
            _colorCode = colorCode.ToString();
        }

        // 기본 프레임과 이벤트 프레임에 따라 프레임 이미지를 불러온다
        if (typeCode == "BF")
        {
            frame.texture = Resources.Load<Texture2D>("Frame/Show/Base/" + typeCode + frameCode.ToString() + "_" + _colorCode);
        }
        else
        {
            frame.texture = Resources.Load<Texture2D>("Frame/Show/Event/" + typeCode + frameCode.ToString() + "_" + _colorCode);
        }
    }

    // PhotoViewerList의 왼쪽 라인과 오른쪽 라인을 서로 바꾼다
    void Swap()
    {
        // 프레임 수가 1일 경우 종료한다
        if (numberOfFrames == 1)
        {
            return;
        }
        // i = 0, 2, 4로 (0, 1), (2, 3), (4, 5)로 작동한다
        for (int i = 0; i < numberOfFrames; i+=2)
        {
            // (i, i+1)의 PhotoViewer끼리 데이터를 교환한다
            Texture2D tempOriginalTexture = photoViewerList[i].originalTexture;
            Texture2D tempGrayTexture = photoViewerList[i].grayTexture;
            int tempIndex = photoViewerList[i].index;

            photoViewerList[i].originalTexture = photoViewerList[i+1].originalTexture;
            photoViewerList[i].grayTexture = photoViewerList[i+1].grayTexture;
            photoViewerList[i].index = photoViewerList[i+1].index;

            photoViewerList[i+1].originalTexture = tempOriginalTexture;
            photoViewerList[i+1].grayTexture = tempGrayTexture;
            photoViewerList[i+1].index = tempIndex;

            // 현재 적용중인 Filter에 따라 다시 적용시켜준다
            if (isColor)
            {
                photoViewerList[i].ApplyColorFilter();
                photoViewerList[i+1].ApplyColorFilter();
            }
            else
            {
                photoViewerList[i].ApplyGrayFilter();
                photoViewerList[i+1].ApplyGrayFilter();
            }
        }
    }

    // Photograph가 터치되어 사진 선택을 요청
    public bool AddPhoto(int index)
    {
        // 현재 선택된 사진 수가 최대면 false를 return한다
        if (selectedIndexList.Count >= numberOfFrames)
        {
            return false;
        }

        // photoViewer중 앞에서부터 비어있는 photoViewer를 찾아 선택한 사진을 저장한다
        for (int i = 0; i < numberOfFrames; i++)
        {
            if (photoViewerList[i].originalTexture == null)
            {
                photoViewerList[i].originalTexture = photographList[index].originalTexture;
                photoViewerList[i].grayTexture = photographList[index].grayTexture;
                photoViewerList[i].index = photographList[index].index;
                photoViewerList[i].ShowImage();
                break;
            }
        }
        // 선택한 사진 번호를 저장한다
        selectedIndexList.Add(index);

        // 현재 선택된 사진 수가 최대면 nextButton의 입력을 허용하고 Sprite를 허용상태로 변경한다
        if (selectedIndexList.Count == numberOfFrames)
        {
            nextButton.interactable = true;
            nextButton.image.sprite = Resources.Load<Sprite>("Images/SelectedNextButton");
        }

        // 사진이 성공적으로 선택되어 true를 return한다
        return true;
    }

    // PhotoViewer가 터치되어 사진 제거를 요청
    public void RemovePhoto(int index)
    {
        // 선택한 사진 번호를 제거한다
        selectedIndexList.Remove(index);
        // 제거 후 사진 번호가 포함된 개수를 불러온다
        int num = InformNumOfPhoto(index);
        // 해당 Photograph에게 사진 번호가 포함된 개수를 알려준다
        photographList[index].GetNumOfPhoto(num);

        // nextButton의 입력을 거부하고 Sprite를 거부상태로 변경한다
        nextButton.interactable = false;
        nextButton.image.sprite = originalNextButtonSprite;
    }

    // 현재 선택한 사진 번호들 중에 index의 개수를 반환한다
    public int InformNumOfPhoto(int index)
    {
        int result = 0;
        for (int i = 0; i < selectedIndexList.Count; i++)
        {
            if (selectedIndexList[i] == index)
            {
                result++;
            }
        }
        return result;
    }

    // PhotoViewer에 할당된 이미지들을 컬러로 표시
    void ColorFilter()
    {
        isColor = true;
        for (int i = 0; i < numberOfFrames; i++)
        {
            photoViewerList[i].ApplyColorFilter();
        }
    }

    // PhotoViewer에 할당된 이미지들을 흑백으로 표시
    void GrayFilter()
    {
        isColor = false;
        for (int i = 0; i < numberOfFrames; i++)
        {
            photoViewerList[i].ApplyGrayFilter();
        }
    }

    // 사진 선택 단계를 종료한다
    void EndSelectPhoto()
    {
        // 사진 선택 단계를 종료한다
        selectMode = false;
        // 프레임 선택 제한시간을 30초로 초기화한다
        remainingTime = 30;

        // 사용하지 않는 오브젝트들은 비활성화하고 사용하는 오브젝트들은 활성화한다
        nextButton.gameObject.SetActive(false);
        printButton.gameObject.SetActive(true);
        for (int i = 0; i < photographCode; i++)
        {
            photographList[i].gameObject.SetActive(false);
        }
        baseFrame.SetActive(true);
        baseButton.gameObject.SetActive(true);
        eventButton.gameObject.SetActive(true);
        frameButtonBackgroundImage.gameObject.SetActive(true);
        colorButton.gameObject.SetActive(true);
        grayButton.gameObject.SetActive(true);
        swapButton.gameObject.SetActive(true);
        text1.SetActive(true);
        text2.SetActive(true);
    }

    // 최종 결과물을 저장하는 함수
    
    IEnumerator SaveGoodoPhoto()
    {
        // 제한시간에 의한 CriticalSection을 설정하고 인쇄 대기화면을 활성화한 후 진행한다
        yield return StartCoroutine(OpenWaitingScreen());

        // 인쇄될 사진의 크기를 지정한다
        int resultPhotoWidth, resultPhotoHeight;
        if (numberOfFrames == 2)
        {
            resultPhotoWidth = 1800;
            resultPhotoHeight = 1200;
        }
        else
        {
            resultPhotoWidth = 1200;
            resultPhotoHeight = 1800;
        }

        // 최종 결과물을 저장할 Texture2D를 생성한다
        Texture2D combinedTexture = new Texture2D(resultPhotoWidth, resultPhotoHeight, TextureFormat.RGBA32, false);

        // 선택된 프레임의 인쇄버전을 불러온다
        Texture2D printFrame = new Texture2D(resultPhotoWidth, resultPhotoHeight, TextureFormat.RGBA32, false);
        if (_typeCode == "BF")
        {
            printFrame = Resources.Load<Texture2D>("Frame/Print/Base/" + _typeCode + frameCode.ToString() + "_" + _colorCode);
        }
        else
        {
            printFrame = Resources.Load<Texture2D>("Frame/Print/Event/" + _typeCode + frameCode.ToString() + "_" + _colorCode);
        }
        // 최종 결과물에 프레임을 합성시킨다
        for (int y = 0; y < combinedTexture.height; y++)
        {
            for (int x = 0; x < combinedTexture.width; x++)
            {
                Color pixelColor = printFrame.GetPixel(x, y);
                combinedTexture.SetPixel(x, y, pixelColor);
            }
        }

        // frameCode(컷수)에 따른 사진이 합성될 위치와 크기를 지정한다
        List<int> printXPosition = new List<int> {};
        List<int> printYPosition = new List<int> {};
        int targetWidth = 0;
        int targetHeight = 0;
        if (frameCode == 1)
        {
            printXPosition = new List<int> {60};
            printYPosition = new List<int> {170};
            targetWidth = 1080;
            targetHeight = 1500;
        }
        else if (frameCode == 2)
        {
            printXPosition = new List<int> {60, 915};
            printYPosition = new List<int> {170, 170};
            targetWidth = 825;
            targetHeight = 900;
        }
        else if (frameCode == 3)
        {
            printXPosition = new List<int> {60, 610, 60, 610};
            printYPosition = new List<int> {930, 930, 170, 170};
            targetWidth = 530;
            targetHeight = 740;
        }
        else if (frameCode == 4)
        {
            printXPosition = new List<int> {60, 615, 60, 615, 60, 615};
            printYPosition = new List<int> {1190, 1190, 680, 680, 170, 170};
            targetWidth = 530;
            targetHeight = 480;
        }

        // 최종 결과물에 선택된 사진들을 합성한다
        for (int i = 0; i < numberOfFrames; i++)
        {
            Texture2D originalTexture = photoViewerList[i].rawImage.texture as Texture2D;
            // photoViewer에 지정된 사진의 크기를 합성할 크기에 맞춰 변경한다
            Texture2D resizedPhoto = ScaleTexture(originalTexture, targetWidth, targetHeight);

            // 최종 결과물에 선택된 사진들을 합성한다
            for (int y = printYPosition[i]; y < printYPosition[i] + resizedPhoto.height; y++)
            {
                for (int x = printXPosition[i]; x < printXPosition[i] + resizedPhoto.width; x++)
                {
                    Color pixelColor = resizedPhoto.GetPixel(x - printXPosition[i], y - printYPosition[i]);
                    combinedTexture.SetPixel(x, y, pixelColor);
                }
            }
        }

        // 합성 내용을 적용한다
        combinedTexture.Apply();
        // 최종 결과물을 저장 경로에 저장한다
        System.IO.File.WriteAllBytes(photographPath + PlayerPrefs.GetString("StoreNumber") + date + ".png", combinedTexture.EncodeToPNG());

        // QR코드 생성 여부에 따라 QR코드를 생성한다
        if (DataModel.instance != null && DataModel.instance.qrCode)
        {
            // QR코드가 생성될 때까지 대기한다
            yield return StartCoroutine(MakeQrCode());

            // QR코드가 성공적으로 발급될 경우에만 합성을 시작한다
            if (qrCodeGenerationResult)
            {
                // QR코드의 크기를 변경한다
                Texture2D resizedQrCode = ScaleTexture(qrCode, 120, 120);

                // 최종 결과물에 QR코드를 합성한다
                if (numberOfFrames == 2)
                {
                    for (int y = 20; y < 20 + resizedQrCode.height; y++)
                    {
                        for (int x = 1620; x < 1620 + resizedQrCode.width; x++)
                        {
                            Color pixelColor = resizedQrCode.GetPixel(x - 1620, y - 20);
                            combinedTexture.SetPixel(x, y, pixelColor);
                        }
                    }
                }
                else
                {
                    for (int y = 20; y < 20 + resizedQrCode.height; y++)
                    {
                        for (int x = 1020; x < 1020 + resizedQrCode.width; x++)
                        {
                            Color pixelColor = resizedQrCode.GetPixel(x - 1020, y - 20);
                            combinedTexture.SetPixel(x, y, pixelColor);
                        }
                    }
                }

                // 합성 내용을 적용한다
                combinedTexture.Apply();
                // 최종 결과물을 저장 경로에 저장한다
                System.IO.File.WriteAllBytes(photographPath + PlayerPrefs.GetString("StoreNumber") + date + ".png", combinedTexture.EncodeToPNG());
            }
        }

        // 다음 Scene으로 이동한다
        NextSecene();
    }

    // 서버와 http 통신으로 QR코드에 할당할 url을 발급받는다
    IEnumerator MakeQrCode()
    {
        // 서버 주소
        string url = "http://127.0.0.1:5000/upload";
        // png사진 전송
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", System.IO.File.ReadAllBytes(photographPath + PlayerPrefs.GetString("StoreNumber") + date + ".png"), PlayerPrefs.GetString("StoreNumber") + date + ".png");

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                responseText = www.downloadHandler.text;
                Debug.Log("Response: " + responseText);
                qrCode = GenerateQR(responseText.Replace("\"", ""));
                // QR코드 발급 결과를 true로 저장한다
                qrCodeGenerationResult = true;
            }
            else
            {
                // QR코드 발급 결과를 false로 저장한다
                qrCodeGenerationResult = false;
                Debug.LogError("Request failed: " + www.error);
            }
        }

        // mp4 동영상 전송
        form = new WWWForm();
        form.AddBinaryData("file", System.IO.File.ReadAllBytes(photographPath + PlayerPrefs.GetString("StoreNumber") + date + ".mp4"), PlayerPrefs.GetString("StoreNumber") + date + ".mp4");

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                responseText = www.downloadHandler.text;
                Debug.Log("Response: " + responseText);
            }
            else
            {
                Debug.LogError("Request failed: " + www.error);
            }
        }
    }

    Texture2D GenerateQR(string text)
    {
        var encoded = new Texture2D(256, 256);
        var color32 = Encode(text, encoded.width, encoded.height);
        encoded.SetPixels32(color32);
        encoded.Apply();

        return encoded;
    }

    Color32[] Encode(string textForEncoding, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width
            }
        };
        return writer.Write(textForEncoding);
    }

    // Texture2D를 원하는 크기로 재구성하는 함수
    Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);

        Color[] rpixels = result.GetPixels(0);
        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);

        for (int px = 0; px < rpixels.Length; px++)
        {
            rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth),
                                                  incY * ((float)Mathf.Floor(px / targetWidth)));
        }

        result.SetPixels(rpixels, 0);
        result.Apply();

        return result;
    }

    // 남은 시간을 초단위로 표시해주고 제한시간이 끝나면 랜덤으로 사진을 선택한 후 인쇄 화면으로 넘어간다.
    IEnumerator CountDown(TMP_Text remainingTimeText)
    {
        // 남은 시간을 초단위로 표시한다
        while (remainingTime >= 0)
        {
            remainingTimeText.text = remainingTime.ToString();
            remainingTime -= 1;
            yield return new WaitForSeconds(1f);
        }

        // 사진 선택 단계일 경우 사진 선택 단계를 마무리한다
        if (selectMode)
        {
            selectMode = false;

            // 랜덤 사진 추가를 반복할 횟수를 계산한다
            int iterations = numberOfFrames - selectedIndexList.Count;
            for (int n = 0; n < iterations; n++)
            {
                // 사진 번호를 랜덤으로 생성하고 이미 포함된지 검사한다
                int randInt = Random.Range(0, photographCode);
                while (DuplicatedInspection(randInt))
                {
                    randInt = Random.Range(0, photographCode);
                }

                // 사진 번호에 해당하는 사진을 추가한다
                for (int i = 0; i < numberOfFrames; i++)
                {
                    if (photoViewerList[i].originalTexture == null)
                    {
                        photoViewerList[i].originalTexture = photographList[randInt].originalTexture;
                        photoViewerList[i].grayTexture = photographList[randInt].grayTexture;
                        photoViewerList[i].index = photographList[randInt].index;
                        photoViewerList[i].ShowImage();
                        selectedIndexList.Add(randInt);
                        break;
                    }
                }
            }
        }

        // 이미 최종 결과물 저장이 진행중이라면 종료하고 아니라면 최종 진행을 시작한다
        if (!CriticalSection)
        {
            EndSelectPhoto();

            StartCoroutine(SaveGoodoPhoto());
        }
    }

    // 사진 번호가 이미 포함된 번호인지 검사한다
    bool DuplicatedInspection(int randInt)
    {
        for (int i = 0; i < selectedIndexList.Count; i++)
        {
            if (randInt == selectedIndexList[i])
            {
                return true;
            }
        }
        return false;
    }

    // 인쇄 대기화면을 활성화하며 제한시간에 의한 CriticalSection을 막는다
    IEnumerator OpenWaitingScreen()
    {
        CriticalSection = true;
        remainingTime += 10000;
        printWaitImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(0);
    }

    void NextSecene()
    {
        SceneManager.LoadScene("PrintScene");
    }
}