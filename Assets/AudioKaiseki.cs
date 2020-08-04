using MP3Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AudioKaiseki : MonoBehaviour
{
    
    public Transform[] trans;
    [Range(6,12)]public int jd = 9;
    [Range(0.01f, 1f)] public float lerpTime = 0.2f;
    public float Pffset = 80;
    public Text urlText;
    public Text outText;
    AudioSource audioS;
    string audioUrl;
    // Start is called before the first frame update
    void Start()
    {
        audioS = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        int jingdu = (int)Mathf.Pow(2, jd);
        float[] result = new float[jingdu];
        audioS.GetSpectrumData(result,0,FFTWindow.BlackmanHarris);
        for (int i=0;i<trans.Length;i++)
        {
            // apply height multiplier to intensity
            float intensity = result[i] * Pffset;
            // calculate object's scale
            float lerpY = Mathf.Lerp(trans[i].localScale.y, intensity, lerpTime);
            Vector3 newScale = new Vector3(trans[i].localScale.x, lerpY, trans[i].localScale.z);
            // appply new scale to object
            trans[i].localScale = newScale;
        }
    }
    public void ChangeAudioSource()
    {
        Debug.Log("按钮点击");
        outText.text = "";
        if (urlText.text == "") {
            outText.text = "不得为空";
            return;
        }
        if (audioUrl != urlText.text)
        {
            audioUrl = urlText.text;
            Debug.Log("执行获取");
            StartCoroutine(GetAudio());
        }
    }

    //http://music.163.com/song/media/outer/url?id=554245894
    IEnumerator GetAudio() {
        using (UnityWebRequest request = UnityWebRequest.Get(audioUrl))
        {
            Debug.Log("开始加载网络资源");
            yield return request.SendWebRequest();
            while (request.isHttpError)
            {
                Debug.Log("ERROR" + request.error);
                yield break;
            }
            while (!request.isDone)
            {
                Debug.Log("ISnt Done");
                yield break;
            }
            Debug.Log("加载完成");
            byte[] results = request.downloadHandler.data;

            if (results.Length == 89491) {
                outText.text = "地址错误";
            }
            AudioClip audioClip = GetAudioClipFromMP3ByteArray(results);
            if (audioClip)
            {
                audioS.Stop();
                audioS.clip = audioClip;
                audioS.Play();
            }
            else
            {
                outText.text = "错误发生";
            }
        }
    }
    public void ChangeValueA(float value) {
        jd = (int)value;
    }

    public void ChangeValueB(float value)
    {
        lerpTime = value;
    }

    public void ChangeValueC(float value)
    {
        Pffset = (int)value;
    }




    AudioClip GetAudioClipFromMP3ByteArray(byte[] in_aMP3Data)
    {
        try
        {

            AudioClip l_oAudioClip = null;
            Stream l_oByteStream = new MemoryStream(in_aMP3Data);
            MP3Stream l_oMP3Stream = new MP3Stream(l_oByteStream);

            //Get the converted stream data
            MemoryStream l_oConvertedAudioData = new MemoryStream();
            byte[] l_aBuffer = new byte[2048];
            int l_nBytesReturned = -1;
            int l_nTotalBytesReturned = 0;

            while (l_nBytesReturned != 0)
            {
                l_nBytesReturned = l_oMP3Stream.Read(l_aBuffer, 0, l_aBuffer.Length);
                l_oConvertedAudioData.Write(l_aBuffer, 0, l_nBytesReturned);
                l_nTotalBytesReturned += l_nBytesReturned;
            }

            Debug.Log("MP3 file has " + l_oMP3Stream.ChannelCount + " channels with a frequency of " + l_oMP3Stream.Frequency);

            byte[] l_aConvertedAudioData = l_oConvertedAudioData.ToArray();
            Debug.Log("Converted Data has " + l_aConvertedAudioData.Length + " bytes of data");

            //Convert the byte converted byte data into float form in the range of 0.0-1.0
            float[] l_aFloatArray = new float[l_aConvertedAudioData.Length / 2];

            for (int i = 0; i < l_aFloatArray.Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                {
                    //Evaluate earlier when pulling from server and/or local filesystem - not needed here
                    //Array.Reverse( l_aConvertedAudioData, i * 2, 2 );
                }

                //Yikes, remember that it is SIGNED Int16, not unsigned (spent a bit of time before realizing I screwed this up...)
                l_aFloatArray[i] = (float)(BitConverter.ToInt16(l_aConvertedAudioData, i * 2) / 32768.0f);
            }

            //For some reason the MP3 header is readin as single channel despite it containing 2 channels of data (investigate later)
            l_oAudioClip = AudioClip.Create("MySound", l_aFloatArray.Length, 2, l_oMP3Stream.Frequency, false, false);
            l_oAudioClip.SetData(l_aFloatArray, 0);

            return l_oAudioClip;
        }
        catch (Exception e) {
            Debug.Log(e);
            outText.text = e.Message;
        }
        return null;
    }
}
