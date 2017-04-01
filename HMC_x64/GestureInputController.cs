using System;
using System.Drawing;
using UnityEngine;

public class GestureInputController : MonoBehaviour
{
    [SerializeField]
    private int cameraId = 0;
    [SerializeField]
    private int numCyclesFingerChange = 3;
    [SerializeField]
    private int minArea = 5000;
    [SerializeField]
    private int widthProp = 3;
    [SerializeField]
    private int heightProp = 4;
    [SerializeField]
    private float xMarginMultiplier = 0.8f;
    [SerializeField]
    private float yMarginMultiplier = 0.6f;
    [SerializeField]
    private float refreshInterval = 0.1f;

    private GestureRecognition gr;

    void Start()
    {
        gr = new GestureRecognition(cameraId, numCyclesFingerChange, minArea, widthProp, heightProp, xMarginMultiplier, yMarginMultiplier, refreshInterval);
        gr.Start();
    }

    private void FixedUpdate()
    {
        gr.Update();
    }

    public int getDetectedFingersNumber()
    {
        return gr.FingerNumStable;
    }

    public PointF getHandPosition()
    {
        return new PointF(gr.XHandPosition, gr.YHandPosition);
    }

    public float getAxis(String axis)
    {
        switch (axis)
        {
            case "Horizontal":
                return getHandPosition().X;
            case "Vertical":
                return getHandPosition().Y;
            case "RelativeHorizontal":
                if (getHandPosition().X > 0.5)
                {
                    return 1;
                }
                else if (getHandPosition().X < 0.5)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            case "RelativeVertical":
                if (getHandPosition().Y > 0.5)
                {
                    return 1;
                }
                else if (getHandPosition().Y < 0.5)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            default:
                return 0;
        }
    }
}
