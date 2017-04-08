using System;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;
using UnityEngine;

class GestureRecognition 
{
    private Image<Bgr, Byte> background, currentFrame, currentFrameCopy;
    private Capture grabber;
    private bool backgroundSaved = false;
    private Seq<MCvConvexityDefect> defects;
    private MCvConvexityDefect[] defectArray;
    private MCvBox2D box;
    private int fingerNumStable = 0, fingerNumLast = 0, fingerNumCurrent = 0, fingerCycleCount = 0, cameraId = 0, numCyclesFingerChange = 3, minArea = 20000, maxArea = 45000, widthProp = 3, heightProp = 4;
    private float nextTime, xHandPosition = 0.5f, yHandPosition = 0.5f, xMarginMultiplier = 0.8f, yMarginMultiplier = 0.6f, refreshInterval = 0.1f;
    private Image<Gray, byte> thresholded;
    public int NumCyclesFingerChange
    {
        get
        {
            return numCyclesFingerChange;
        }

        set
        {
            numCyclesFingerChange = value;
        }
    }

    public int MinArea
    {
        get
        {
            return minArea;
        }

        set
        {
            minArea = value;
        }
    }

    public int MaxArea
    {
        get
        {
            return maxArea;
        }

        set
        {
            maxArea = value;
        }
    }

    public int WidthProp
    {
        get
        {
            return widthProp;
        }

        set
        {
            widthProp = value;
        }
    }

    public int HeightProp
    {
        get
        {
            return heightProp;
        }

        set
        {
            heightProp = value;
        }
    }

    public float XMarginMultiplier
    {
        get
        {
            return xMarginMultiplier;
        }

        set
        {
            xMarginMultiplier = value;
        }
    }

    public float YMarginMultiplier
    {
        get
        {
            return yMarginMultiplier;
        }

        set
        {
            yMarginMultiplier = value;
        }
    }

    public float RefreshInterval
    {
        get
        {
            return refreshInterval;
        }

        set
        {
            refreshInterval = value;
        }
    }

    public int FingerNumStable
    {
        get
        {
            return fingerNumStable;
        }

        set
        {
            fingerNumStable = value;
        }
    }

    public float XHandPosition
    {
        get
        {
            return xHandPosition;
        }

        set
        {
            xHandPosition = value;
        }
    }

    public float YHandPosition
    {
        get
        {
            return yHandPosition;
        }

        set
        {
            yHandPosition = value;
        }
    }

    public int CameraId
    {
        get
        {
            return cameraId;
        }

        set
        {
            cameraId = value;
        }
    }

    public Image<Bgr, byte> CurrentFrame
    {
        get
        {
            return currentFrame;
        }

        set
        {
            currentFrame = value;
        }
    }

    public Image<Gray, byte> Thresholded
    {
        get
        {
            return thresholded;
        }

        set
        {
            thresholded = value;
        }
    }

    public GestureRecognition(int cameraId, int numCyclesFingerChange, int minArea, int maxArea, int widthProp, int heightProp, float xMarginMultiplier, float yMarginMultiplier, float refreshInterval)
    {
        CameraId = cameraId;
        NumCyclesFingerChange = numCyclesFingerChange;
        MinArea = minArea;
        MaxArea = maxArea;
        WidthProp = widthProp;
        HeightProp = heightProp;
        XMarginMultiplier = xMarginMultiplier;
        YMarginMultiplier = yMarginMultiplier;
        RefreshInterval = refreshInterval;
    }

    public void Start()
    {
        try
        {
            grabber = new Emgu.CV.Capture(CameraId);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        grabber.FlipHorizontal = true;
        grabber.QueryFrame();
        box = new MCvBox2D();

        nextTime = Time.time + refreshInterval;
    }

    public void Update()
    {
        if (Time.time >= nextTime)
        {
            FrameGrabber();
            nextTime = Time.time + refreshInterval;
        }
    }

    void FrameGrabber()
    {
        CurrentFrame = grabber.QueryFrame();

        if (CurrentFrame != null)
        {
            if (!backgroundSaved)
            {
                background = CurrentFrame.Copy();
                background.Draw(new Rectangle(0, 0, CurrentFrame.Width / widthProp, CurrentFrame.Height), new Bgr(0, 0, 0), 0);
                background.Draw(new Rectangle(0, CurrentFrame.Height - CurrentFrame.Height / heightProp, CurrentFrame.Width, CurrentFrame.Height / heightProp), new Bgr(0, 0, 0), 0);
                backgroundSaved = true;
            }
            else
            {
                currentFrameCopy = CurrentFrame.Copy();
                currentFrameCopy.Draw(new Rectangle(0, 0, CurrentFrame.Width / widthProp, CurrentFrame.Height), new Bgr(0, 0, 0), 0);
                currentFrameCopy.Draw(new Rectangle(0, CurrentFrame.Height - CurrentFrame.Height / heightProp, CurrentFrame.Width, CurrentFrame.Height / heightProp), new Bgr(0, 0, 0), 0);
                Image<Gray, Byte> currentFrameGrey = currentFrameCopy.Convert<Gray, Byte>();

                Image<Gray, Byte> backgroundGrey = background.Convert<Gray, Byte>();
                Image<Gray, Byte> currentFrameCopyGreyDiff = currentFrameCopy.CopyBlank().Convert<Gray, Byte>();

                CvInvoke.cvAbsDiff(backgroundGrey, currentFrameGrey, currentFrameCopyGreyDiff);
                Thresholded = currentFrameCopyGreyDiff.ThresholdBinary(new Gray(20), new Gray(255));

                CvInvoke.cvSmooth(Thresholded, Thresholded, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_BLUR, 13, 13, 1.5, 0);
                CvInvoke.cvSmooth(Thresholded, Thresholded, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_GAUSSIAN, 13, 13, 1.5, 0);

                #region draw and extract num
                using (MemStorage storage = new MemStorage())
                {
                    Contour<Point> contours = Thresholded.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);

                    Double Result1 = 0;
                    Double Result2 = 0;
                    Contour<Point> biggestContour = null;
                    while (contours != null)
                    {
                        Result1 = contours.Area;
                        if (Result1 > Result2)
                        {
                            Result2 = Result1;
                            biggestContour = contours;
                        }
                        contours = contours.HNext;
                    }

                    // Drawing
                    if (biggestContour != null)
                    {
                        if (biggestContour.Area > minArea && biggestContour.Area < maxArea)
                        {
                            Contour<Point> contour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                            CurrentFrame.Draw(contour, new Bgr(System.Drawing.Color.LimeGreen), 2);

                            box = biggestContour.GetMinAreaRect();
                            currentFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Bgr(200, 125, 75), 2);
                            XHandPosition = (box.center.X - (float)CurrentFrame.Width / widthProp) / ((CurrentFrame.Width - (float)CurrentFrame.Width / widthProp) * xMarginMultiplier);
                            YHandPosition = 1 - (box.center.Y - (float)CurrentFrame.Height / heightProp) / ((CurrentFrame.Height - (float)CurrentFrame.Height / heightProp) * yMarginMultiplier);

                            defects = biggestContour.GetConvexityDefacts(storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                            defectArray = defects.ToArray();

                            // DRAW & FINGER NUM
                            fingerNumCurrent = 0;

                            for (int i = 0; i < defects.Total; i++)
                            {
                                PointF startPoint = new PointF((float)defectArray[i].StartPoint.X,
                                                                (float)defectArray[i].StartPoint.Y);

                                PointF depthPoint = new PointF((float)defectArray[i].DepthPoint.X,
                                                                (float)defectArray[i].DepthPoint.Y);

                                LineSegment2D startDepthLine = new LineSegment2D(defectArray[i].StartPoint, defectArray[i].DepthPoint);

                                LineSegment2D depthEndLine = new LineSegment2D(defectArray[i].DepthPoint, defectArray[i].EndPoint);

                                CircleF startCircle = new CircleF(startPoint, 5f);

                                CircleF depthCircle = new CircleF(depthPoint, 5f);

                                //Custom heuristic based on some experiment, double check it before use
                                if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 6.5))
                                {
                                    fingerNumCurrent++;
                                    currentFrame.Draw(startDepthLine, new Bgr(System.Drawing.Color.Green), 2);
                                }
                                currentFrame.Draw(startCircle, new Bgr(System.Drawing.Color.Red), 2);
                                currentFrame.Draw(depthCircle, new Bgr(System.Drawing.Color.Yellow), 5);
                            }
                        }
                        else
                        {
                            fingerNumCurrent = 0;
                        }

                        if (fingerNumCurrent == fingerNumLast) fingerCycleCount += 1;
                        else fingerCycleCount = 0;

                        if (fingerCycleCount == numCyclesFingerChange) FingerNumStable = fingerNumCurrent;

                        fingerNumLast = fingerNumCurrent;
                        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_DUPLEX, 5d, 5d);
                        currentFrame.Draw(fingerNumStable.ToString(), ref font, new Point(50, 150), new Bgr(System.Drawing.Color.White));
                    }
                }
                #endregion
            }
        }
    }
}
