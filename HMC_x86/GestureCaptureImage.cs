using UnityEngine;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System;
using UnityEngine.UI;
using System.Drawing;

public class GestureCaptureImage : MonoBehaviour
{
    [SerializeField]
    private GestureInputController controller;
    public enum outputEnum { CAPTURE, THRESHOLD };
    [SerializeField]
    private outputEnum output;
    private Texture2D camera;
    Image<Bgr, byte> capture;
    Image<Gray, byte> thresholded;

    void Update()
    {
        if (output == outputEnum.CAPTURE)
        {
            Image<Bgr, byte> nextFrame = controller.GetCapture();

            if (nextFrame != null && nextFrame.ToBitmap() != null)
            {
                Bitmap bitmapCurrentFrame = new Bitmap(nextFrame.Bitmap, Convert.ToInt32(GetComponent<RectTransform>().rect.width), Convert.ToInt32(GetComponent<RectTransform>().rect.height));
                MemoryStream m = new MemoryStream();
                bitmapCurrentFrame.Save(m, bitmapCurrentFrame.RawFormat);

                if (camera != null)
                {
                    GameObject.Destroy(camera);
                }
                camera = new Texture2D(Convert.ToInt32(GetComponent<RectTransform>().rect.width), Convert.ToInt32(GetComponent<RectTransform>().rect.height));

                camera.LoadImage(m.ToArray());
                GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(camera, new Rect(0, 0, Convert.ToInt32(GetComponent<RectTransform>().rect.width), Convert.ToInt32(GetComponent<RectTransform>().rect.height)), new Vector2(0.5f, 0.5f));
            }

        }
        else if (output == outputEnum.THRESHOLD)
        {
            Image<Gray, byte> nextFrame = controller.GetThresholded();

            if (nextFrame != null && nextFrame.ToBitmap() != null)
            {
                Bitmap bitmapCurrentFrame = new Bitmap(nextFrame.Bitmap, Convert.ToInt32(GetComponent<RectTransform>().rect.width), Convert.ToInt32(GetComponent<RectTransform>().rect.height));
                MemoryStream m = new MemoryStream();
                bitmapCurrentFrame.Save(m, bitmapCurrentFrame.RawFormat);

                if (camera != null)
                {
                    GameObject.Destroy(camera);
                }
                camera = new Texture2D(Convert.ToInt32(GetComponent<RectTransform>().rect.width), Convert.ToInt32(GetComponent<RectTransform>().rect.height));

                camera.LoadImage(m.ToArray());
                GetComponent<UnityEngine.UI.Image>().sprite = Sprite.Create(camera, new Rect(0, 0, Convert.ToInt32(GetComponent<RectTransform>().rect.width), Convert.ToInt32(GetComponent<RectTransform>().rect.height)), new Vector2(0.5f, 0.5f));
            }

        }


    }
}
