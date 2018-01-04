using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge;
using SUNO.model;

namespace SUNO.logic
{
    public class SunoLogic
    {
        public static Boolean SerializeTrajectorySet(List<TrajectorySet> ts) {
            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(List<TrajectorySet>));

            System.IO.StreamWriter file = new System.IO.StreamWriter(
                @".\DATA\TrajectorySet.xml");
            writer.Serialize(file, ts);
            file.Close();

            return true;
        }

        public static List<TrajectorySet> DeserializeTrajectorySet()
        {
            List<TrajectorySet> overview = new List<TrajectorySet>();
            /*
            try
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(List<TrajectorySet>));
                System.IO.StreamReader file = new System.IO.StreamReader(@".\DATA\TrajectorySet.xml");

                overview = (List<TrajectorySet>)reader.Deserialize(file);

            }
            catch (Exception ex) {
                Console.WriteLine("--------- Outer Exception Data ---------");

                Console.WriteLine("Message: {0}", ex.Message);   
                ex = ex.InnerException;
                if (null != ex)
                {
                    Console.WriteLine("--------- Inner Exception Data ---------");

                    Console.WriteLine("Message: {0}", ex.Message);   
                    ex = ex.InnerException;
                }
            }*/
            return overview;
        }

        public TrajectorySet CalculateDiscontinuityPoints(TrajectorySet ts, int lineScanThreshold, int lineScanRadius, int radialThreshold,Boolean geometrical)
        {
            
            //foreach hough line
            foreach (HoughLine line in ts.lines)
            {
                int r = line.Radius;
                double t = line.Theta;

                // check if line is in lower part of the image
                if (r < 0)
                {
                    t += 180;
                    r = -r;
                }

                // convert degrees to radians
                t = (t / 180) * Math.PI;

                // get image centers (all coordinate are measured relative
                // to center)
                int w2 = ts.image.Width / 2;
                int h2 = ts.image.Height / 2;

                double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

                if (line.Theta != 0)
                {
                    // none vertical line
                    x0 = -w2; // most left point
                    x1 = w2;  // most right point

                    // calculate corresponding y values
                    y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                    y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);
                }
                else
                {
                    // vertical line
                    x0 = line.Radius;
                    x1 = line.Radius;

                    y0 = h2;
                    y1 = -h2;
                }

                int X0, Y0, X1, Y1;

                X0 = (int)x0 + w2;
                Y0 = h2 - (int)y0;
                X1 = (int)x1 + w2;
                Y1 = h2 - (int)y1;

                PlotFunction plot = AddPlotPoint;
                Line((int)X0, (int)Y0, (int)X1, (int)Y1, plot);

                double R = -((double)Y1 - (double)Y0) / ((double)X1 - (double)X0);

                ts.bresenhamsLines.Add(current_line_points);

                List<System.Drawing.Point> black_points = new List<System.Drawing.Point>();
                int end_segments = 10;
                Boolean start_segment = false;
                BlackSegment current_segment = new BlackSegment();
                int segment_tollerance = 10;

                foreach (System.Drawing.Point current in current_line_points) {
                    //get pixel neighbours average colour
                    double average ;
                    if (geometrical)
                    {
                        average = GetNeightbourAveragePixelColor(ts.elaborated_image, current, lineScanRadius);
                    } else {
                        average = GetNeightbourAveragePixelColor(ts.elaborated_image, current, lineScanRadius, R);
                    }
                    
                        if (average > lineScanThreshold)
                        {
                            if (!start_segment)
                            {
                                current_segment.start = current;
                                start_segment = true;
                            }
                            black_points.Add(current);
                        }
                        else {
                            if (start_segment) {
                                segment_tollerance--;

                                if (segment_tollerance <= 0)
                                {
                                    current_segment.end = current;
                                    start_segment = false;
                                    ts.black_segments.Add(current_segment);
                                    current_segment = new BlackSegment();
                                    segment_tollerance = 10;
                                }
                            }
                        }
                }

                current_line_points = new List<System.Drawing.Point>();
                ts.black_points = new List<System.Drawing.Point>(black_points);

                //foreach segment check the size and get the central pixel
                foreach (BlackSegment current in ts.black_segments) {

                    if (geometrical)
                    {
                        

                        /* Algoritmo basato su punto medio dei segmenti
                         * restituisce alcuni falsi positivi a causa dell'imprecisione introdotta dal calcolo della media dei punti 
                         * inoltre non restituisce i punti esatti intermedi della discontinuità.*/
                     
                        int len = SunoLogic.distanceBetween2Points(current.start, current.end);

                        int mid_len = len / 2;
                        Boolean start = false;
                        foreach (System.Drawing.Point current_point in ts.black_points) {
                            if (current_point.X == current.start.X && current_point.Y == current.start.Y) {
                                start = true;
                            }

                            if (start)
                            {
                                mid_len--;
                                if (mid_len <= 0)
                                {
                                    ts.trajectory_points.Add(current_point);
                                    start = false;
                                    break;
                                }
                            }
                      
                        }
                    }
                    else
                    {
                        List<System.Drawing.Point> start_neightbour = new List<System.Drawing.Point>();
                        List<System.Drawing.Point> end_neightbour = new List<System.Drawing.Point>();

                        foreach (IntPoint current_corner in ts.corners)
                        {

                            System.Drawing.Point current_corner_point = new System.Drawing.Point(current_corner.X, current_corner.Y);

                            //check start points
                            int distance_from_start = SunoLogic.distanceBetween2Points(current_corner_point, current.start);

                            if (distance_from_start - radialThreshold <= 0)
                            {
                                start_neightbour.Add(current_corner_point);
                            }

                            int distance_from_end = SunoLogic.distanceBetween2Points(current_corner_point, current.end);

                            if (distance_from_end - radialThreshold <= 0)
                            {
                                end_neightbour.Add(current_corner_point);
                            }
                        }

                        int ts_x = 0, ts_y = 0, te_x = 0, te_y = 0;
                        int count = 0;

                        foreach (System.Drawing.Point current_pount in start_neightbour)
                        {
                            //media tra le coordinate X dello start
                            //media tra le coordinate Y dello start
                            ts_x += current_pount.X;
                            ts_y += current_pount.Y;

                            count++;
                        }

                        if (count == 0)
                        {
                            ts_x = current.start.X;
                            ts_y = current.start.Y;
                        }
                        else
                        {
                            ts_x /= count;
                            ts_y /= count;
                        }
                        count = 0;

                        foreach (System.Drawing.Point current_pount in end_neightbour)
                        {
                            //media tra le coordinate X dell'end
                            //media tra le coordinate Y dell'end

                            te_x += current_pount.X;
                            te_y += current_pount.Y;

                            count++;
                        }

                        if (count == 0)
                        {
                            te_x = current.end.X;
                            te_y = current.end.Y;

                        }
                        else
                        {
                            te_x /= count;
                            te_y /= count;
                        }

                        //calculate segment lenght
                        System.Drawing.Point mid = SunoLogic.midPoint(new System.Drawing.Point(ts_x, ts_y), new System.Drawing.Point(te_x, te_y));

                        //add point
                        ts.trajectory_points.Add(mid);
                    }
                }
            }

            return ts;
        }

        private static System.Drawing.Point midPoint(System.Drawing.Point point, System.Drawing.Point point_2)
        {
            int dx = (point.X + point_2.X)/2;
            int dy = (point.Y + point_2.Y)/2;

            return new System.Drawing.Point(dx,dy);
        }

        private static int distanceBetween2Points(System.Drawing.Point point, System.Drawing.Point point_2)
        {
            int dx = Math.Abs(point.X - point_2.X);
            int dy = Math.Abs(point.Y - point_2.Y);

            int res =  (int)Math.Round(Math.Sqrt(dx * dx + dy * dy), 0);

            return res;
        }
        
        public double GetNeightbourAveragePixelColor(Bitmap image, System.Drawing.Point current, int lineScanRadius,double R)
        {
            if (current.X < 0 || current.Y < 0)
                return -1;

            if (current.X >= image.Width || current.Y >= image.Height)
                return -1;
            double average=0;

            System.Drawing.Point p_start, p_end;

            p_start = new System.Drawing.Point((int)(current.X + Math.Sqrt(lineScanRadius * lineScanRadius / (1 + 1 / R * R))), (int)(current.Y + (-1 / R) * Math.Sqrt(lineScanRadius * lineScanRadius / (1 + 1 / (R * R)))));
            p_end = new System.Drawing.Point((int)(current.X - Math.Sqrt(lineScanRadius * lineScanRadius / (1 + 1 / R * R))), (int)(current.Y - (-1 / R) * Math.Sqrt(lineScanRadius * lineScanRadius / (1 + 1 / (R * R)))));

            PlotFunction plot = AddPerpPoint;
            current_perp_points = new List<System.Drawing.Point>();
            Line((int)p_start.X, (int)p_start.Y, (int)p_end.X, (int)p_end.Y, plot);

            Color c;
            
            int count=0;

            foreach (System.Drawing.Point current_point in current_perp_points)
            {
                if (current_point.X < 0 || current_point.Y < 0)
                    continue;

                if (current_point.X >= image.Width || current_point.Y >= image.Height)
                    continue;

                c = image.GetPixel(current_point.X, current_point.Y);

                average += Math.Abs(c.R - 255);
                count++;
            }


            return average;
        }

        public double GetNeightbourAveragePixelColor(Bitmap image, System.Drawing.Point current, int lineScanRadius)
        {
            //Color c = image.GetPixel(point.X, point.Y);
            if (current.X < 0 || current.Y < 0)
                return -1;

            if (current.X >= image.Width || current.Y >= image.Height)
                return -1;

            Color c;
            double average=0;
            int count=0;
            int startx = (current.X - lineScanRadius / 2);
            int endx = (current.X + lineScanRadius / 2);
            int starty  =(current.Y - lineScanRadius / 2);
            int endy =(current.Y + lineScanRadius / 2);


            if (startx < 0)
                startx = 0;
            if (starty < 0)
                starty = 0;

            if (endx > image.Width)
                endx = image.Width - 1;

            if (endy > image.Height)
                endy = image.Height - 1;

            for (int i= startx;i<endx;i++)
                for (int j =starty ; j < endy; j++)
                {
                    c = image.GetPixel(i, j);
                    
                    average += Math.Abs(c.R - 255);
                    count++;
                }

            return average / count;
        }

        List<System.Drawing.Point> current_line_points = new List<System.Drawing.Point>();
        private Boolean AddPlotPoint(int x, int y) {
            current_line_points.Add(new System.Drawing.Point(x,y));
            return true;
        }

        List<System.Drawing.Point> current_perp_points = new List<System.Drawing.Point>();
        private Boolean AddPerpPoint(int x, int y)
        {
            current_perp_points.Add(new System.Drawing.Point(x, y));
            return true;
        }

        public TrajectorySet CalculateTrajectorySet(Bitmap image, Double HoughRelativeIntensity, Int32 SusanCornerDifferenceTreshold, Int32 SusanCornerGeometricalTreshold, Int32 lineScanThreshold, Int32 lineScanRadius, Int32 radialThreshold,Boolean geometrical) 
        {
            TrajectorySet ts = new TrajectorySet();

            ts.elaboration_start_time = DateTime.Now;

            Bitmap hough_image = AForge.Imaging.Image.Clone(image, PixelFormat.Format24bppRgb);
            Bitmap susan_image = AForge.Imaging.Image.Clone(image, PixelFormat.Format24bppRgb);

           // AForge.Imaging.Image.FormatImage(ref hough_image);
           // AForge.Imaging.Image.FormatImage(ref susan_image);

            //FILTERS AND SOURCE INIT
            hough_image = Grayscale.CommonAlgorithms.RMY.Apply(hough_image);
            susan_image = Grayscale.CommonAlgorithms.RMY.Apply(susan_image);

            hough_image = ApplyFilter(hough_image, new Threshold());
            susan_image = ApplyFilter(susan_image, new Threshold());
            

            
            //INIT OBJECTS
            

            HoughLineTransformation lineTransform = new HoughLineTransformation();
            SusanCornersDetector susanTransform   = new SusanCornersDetector(SusanCornerDifferenceTreshold, SusanCornerGeometricalTreshold);
           
            FiltersSequence filter = new FiltersSequence(
                Grayscale.CommonAlgorithms.BT709,
                new Threshold(64)
            );

            //APPLY ALGOS AND GET ENTITIES
            BitmapData houghSourceData = hough_image.LockBits(
                new Rectangle(0, 0, hough_image.Width, hough_image.Height),
                ImageLockMode.ReadOnly, hough_image.PixelFormat);

            lineTransform.ProcessImage(houghSourceData);

            
            hough_image.UnlockBits(houghSourceData);
            ts.elaborated_image = AForge.Imaging.Image.Clone(hough_image, PixelFormat.Format24bppRgb); ;
            hough_image.Dispose();

            HoughLine[] lines = lineTransform.GetLinesByRelativeIntensity(HoughRelativeIntensity);

            BitmapData sousanSourceData = susan_image.LockBits(
                new Rectangle(0, 0, susan_image.Width, susan_image.Height),
                ImageLockMode.ReadOnly, susan_image.PixelFormat);

            List<IntPoint> corners = susanTransform.ProcessImage(sousanSourceData);
            susan_image.UnlockBits(sousanSourceData);
            susan_image.Dispose();

            //populat trajectoryset
            ts.image = image;
            ts.lineTransform = lineTransform;
            ts.lines = lines;
            ts.susanTransform = susanTransform;
            ts.corners = corners;

            //get discontinuity points
            this.CalculateDiscontinuityPoints(ts, lineScanThreshold, lineScanRadius, radialThreshold, geometrical);

            //get trajectory lines (W=3)

            //get trajectory lines (W=2)

            //get trajectory lines (W=1)

            //get quadrant foreach lines W=3, W=2, W=1
            ts.elaboration_end_time = DateTime.Now;

            return ts;
        }

        public NeighbourQuadrantList GetNeighbourQuadrantList(TrajectorySet img)
        {
            return null;
        }

        public Bitmap renderSunoPreview(TrajectorySet ts, int susanCornerPixelRadius, int houghLineWidth,Boolean susanRender,Boolean houghRender, Boolean blackRender,Boolean trajectoryPointsRender)
        {
            HoughLineTransformation lineTransform = ts.lineTransform;
            HoughLine[] lines = ts.lines;
            List<IntPoint> corners = ts.corners;

            Bitmap image = AForge.Imaging.Image.Clone(ts.image, PixelFormat.Format24bppRgb);

            Graphics g = Graphics.FromImage(image);

            if (susanRender)
            foreach (IntPoint corner in corners)
            {
                g.FillEllipse(new SolidBrush(Color.Red), corner.X, corner.Y, susanCornerPixelRadius, susanCornerPixelRadius);
            }


            if (blackRender)
            foreach (System.Drawing.Point current_point in ts.black_points)
            {
                g.FillEllipse(new SolidBrush(Color.Yellow), current_point.X, current_point.Y, susanCornerPixelRadius, susanCornerPixelRadius);
            }

            if (trajectoryPointsRender) 
                foreach (System.Drawing.Point current_point in ts.trajectory_points)
                {
                    g.FillEllipse(new SolidBrush(Color.Coral), current_point.X, current_point.Y, susanCornerPixelRadius, susanCornerPixelRadius);
                }



            BitmapData sourceData = image.LockBits(
               new Rectangle(0, 0, image.Width, image.Height),
               ImageLockMode.ReadOnly, image.PixelFormat);
            if (houghRender)
            foreach (HoughLine line in lines)
            {
                drawHoughLine(line, image, sourceData, houghLineWidth);
            }
            image.UnlockBits(sourceData);
            return image;
        }

        private void drawHoughLine(HoughLine line, Bitmap image, BitmapData sourceData, int houghLineWidth)
        {
            int r = line.Radius;
            double t = line.Theta;

            // check if line is in lower part of the image
            if (r < 0)
            {
                t += 180;
                r = -r;
            }

            // convert degrees to radians
            t = (t / 180) * Math.PI;

            // get image centers (all coordinate are measured relative
            // to center)
            int w2 = image.Width / 2;
            int h2 = image.Height / 2;

            double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

            if (line.Theta != 0)
            {
                // none vertical line
                x0 = -w2; // most left point
                x1 = w2;  // most right point

                // calculate corresponding y values
                y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);
            }
            else
            {
                // vertical line
                x0 = line.Radius;
                x1 = line.Radius;

                y0 = h2;
                y1 = -h2;
            }

            // draw line on the image
            int offset = -houghLineWidth / 2;
            for (int i = 0; i < houghLineWidth; i++)
            {
                Drawing.Line(sourceData,
                    new IntPoint((int)x0 + w2 + offset, h2 - (int)y0),
                    new IntPoint((int)x1 + w2 + offset, h2 - (int)y1),
                    Color.Green);
                offset+=1;
            }
        }

        private  System.Drawing.Bitmap ApplyFilter(System.Drawing.Bitmap sourceImage, AForge.Imaging.Filters.IFilter filter)
        {
            // apply filter
            //System.Drawing.Bitmap tempImage = AForge.Imaging.Image.Clone(sourceImage, PixelFormat.Format24bppRgb);
            System.Drawing.Bitmap filteredImage = filter.Apply(sourceImage);
            // display filtered image
            return filteredImage;
        }


        private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

        /// <summary>
        /// The plot function delegate
        /// </summary>
        /// <param name="x">The x co-ord being plotted</param>
        /// <param name="y">The y co-ord being plotted</param>
        /// <returns>True to continue, false to stop the algorithm</returns>
        public delegate bool PlotFunction(int x, int y);

        /// <summary>
        /// Plot the line from (x0, y0) to (x1, y10
        /// </summary>
        /// <param name="x0">The start x</param>
        /// <param name="y0">The start y</param>
        /// <param name="x1">The end x</param>
        /// <param name="y1">The end y</param>
        /// <param name="plot">The plotting function (if this returns false, the algorithm stops early)</param>
        public static void Line(int x0, int y0, int x1, int y1, PlotFunction plot)
        {
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (steep) { Swap<int>(ref x0, ref y0); Swap<int>(ref x1, ref y1); }
            if (x0 > x1) { Swap<int>(ref x0, ref x1); Swap<int>(ref y0, ref y1); }
            int dX = (x1 - x0), dY = Math.Abs(y1 - y0), err = (dX / 2), ystep = (y0 < y1 ? 1 : -1), y = y0;

            for (int x = x0; x <= x1; ++x)
            {
                if (!(steep ? plot(y, x) : plot(x, y))) return;
                err = err - dY;
                if (err < 0) { y += ystep; err += dX; }
            }
        }
    }
}
