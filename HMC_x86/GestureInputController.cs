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

    public int GetDetectedFingersNumber()
    {
        return gr.FingerNumStable;
    }

    public PointF GetHandPosition()
    {
        return new PointF(gr.XHandPosition, gr.YHandPosition);
    }

    public PointF GetNormalizedHandPosition()
    {
        float normalizedXHandPosition;
        float normalizedYHandPosition;
        if (gr.XHandPosition < 0.5)
        {
            normalizedXHandPosition = -gr.XHandPosition * 2;
        }
        else if (gr.XHandPosition == 0.5)
        {
            normalizedXHandPosition = 0;
        }
        else
        {
            normalizedXHandPosition = (gr.XHandPosition - 0.5f) * 2;
        }

        if (gr.YHandPosition < 0.5)
        {
            normalizedYHandPosition = -gr.XHandPosition * 2;
        }
        else if (gr.YHandPosition == 0.5)
        {
            normalizedYHandPosition = 0;
        }
        else
        {
            normalizedYHandPosition = (gr.YHandPosition - 0.5f) * 2;
        }
        return new PointF(normalizedXHandPosition, normalizedYHandPosition);
    }

    public float GetAxis(String axis)
    {
        switch (axis)
        {
            case "Horizontal":
                return GetNormalizedHandPosition().X;
            case "Vertical":
                return GetNormalizedHandPosition().Y;
            case "RelativeHorizontal":
                if (GetNormalizedHandPosition().X > 0.5)
                {
                    return 1;
                }
                else if (GetNormalizedHandPosition().X < 0.5)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            case "RelativeVertical":
                if (GetNormalizedHandPosition().Y > 0.5)
                {
                    return 1;
                }
                else if (GetNormalizedHandPosition().Y < 0.5)
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
