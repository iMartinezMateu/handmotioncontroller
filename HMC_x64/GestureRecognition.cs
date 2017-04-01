using System;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;
using UnityEngine;

class GestureRecognition
{
    Image<Bgr, Byte> background, currentFrame, currentFrameCopy;
    Capture grabber;
    bool backgroundSaved = false;
    Seq<MCvConvexityDefect> defects;
    MCvConvexityDefect[] defectArray;
    MCvBox2D box;
    int fingerNumStable = 0, fingerNumLast = 0, fingerNumCurrent = 0, fingerCycleCount = 0;
    float nextTime, xHandPosition = 0.5f, yHandPosition = 0.5f;

    public void Start()
    {
        try
        {
            grabber = new Emgu.CV.Capture();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        grabber.FlipHorizontal = true;
        grabber.QueryFrame();
        box = new MCvBox2D();

        nextTime = Time.time + Constants.REFRESH_INTERVAL;
    }

    public void Update()
    {
        if (Time.time >= nextTime)
        {
            FrameGrabber();
            nextTime = Time.time + Constants.REFRESH_INTERVAL;
        }
    }

    void FrameGrabber() 
    {
        currentFrame = grabber.QueryFrame();

        if (currentFrame != null)
        {
            if (!backgroundSaved)
            {
                background = currentFrame.Copy();
                background.Draw(new Rectangle(0, 0, currentFrame.Width / Constants.WIDTH_PROP, currentFrame.Height), new Bgr(0, 0, 0), 0);
                background.Draw(new Rectangle(0, currentFrame.Height - currentFrame.Height / Constants.HEIGHT_PROP, currentFrame.Width, currentFrame.Height / Constants.HEIGHT_PROP), new Bgr(0, 0, 0), 0);
                backgroundSaved = true;
            }
            else
            {
                currentFrameCopy = currentFrame.Copy();
                currentFrameCopy.Draw(new Rectangle(0, 0, currentFrame.Width / Constants.WIDTH_PROP, currentFrame.Height), new Bgr(0, 0, 0), 0);
                currentFrameCopy.Draw(new Rectangle(0, currentFrame.Height - currentFrame.Height / Constants.HEIGHT_PROP, currentFrame.Width, currentFrame.Height / Constants.HEIGHT_PROP), new Bgr(0, 0, 0), 0);
                Image<Gray, Byte> currentFrameGrey = currentFrameCopy.Convert<Gray, Byte>();

                Image<Gray, Byte> backgroundGrey = background.Convert<Gray, Byte>();
                Image<Gray, Byte> currentFrameCopyGreyDiff = currentFrameCopy.CopyBlank().Convert<Gray, Byte>();

                CvInvoke.cvAbsDiff(backgroundGrey, currentFrameGrey, currentFrameCopyGreyDiff);
                Image<Gray, Byte> thresholded = currentFrameCopyGreyDiff.ThresholdBinary(new Gray(20), new Gray(255));

                CvInvoke.cvSmooth(thresholded, thresholded, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_BLUR, 13, 13, 1.5, 0);
                CvInvoke.cvSmooth(thresholded, thresholded, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_GAUSSIAN, 13, 13, 1.5, 0);

                #region draw and extract num
                using (MemStorage storage = new MemStorage())
                {
                    Contour<Point> contours = thresholded.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);

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
                        if (biggestContour.Area > Constants.MIN_AREA)
                        {
                            Contour<Point> contour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                            currentFrame.Draw(contour, new Bgr(System.Drawing.Color.LimeGreen), 2);

                            box = biggestContour.GetMinAreaRect();

                            xHandPosition = (box.center.X - (float)currentFrame.Width / Constants.WIDTH_PROP) / ((currentFrame.Width - (float)currentFrame.Width / Constants.WIDTH_PROP) * Constants.X_MARGIN_MULTIPLIER);
                            yHandPosition = 1 - (box.center.Y - (float)currentFrame.Height / Constants.HEIGHT_PROP) / ((currentFrame.Height - (float)currentFrame.Height / Constants.HEIGHT_PROP) * Constants.Y_MARGIN_MULTIPLIER);

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

                                CircleF startCircle = new CircleF(startPoint, 5f);

                                CircleF depthCircle = new CircleF(depthPoint, 5f);

                                //Custom heuristic based on some experiment, double check it before use
                                if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 6.5))
                                {
                                    fingerNumCurrent++;
                                }
                            }
                        }
                        else
                        {
                            fingerNumCurrent = 0;
                        }

                        if (fingerNumCurrent == fingerNumLast) fingerCycleCount += 1;
                        else fingerCycleCount = 0;

                        if (fingerCycleCount == Constants.CYCLES_TO_CHANGE_FINGER_NUM) fingerNumStable = fingerNumCurrent;

                        fingerNumLast = fingerNumCurrent;
                    }
                }
                #endregion
            }
        }
    }

    public int getNumFingers()
    {
        return fingerNumStable;
    }

    public PointF getHandPosition()
    {
        return new PointF(xHandPosition, yHandPosition);
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
