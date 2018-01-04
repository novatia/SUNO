using AForge;
using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SUNO.model;

namespace SUNO
{
    [Serializable()]
    public class TrajectorySet
    {
        public string D;
        public string E;
        public string F;
        public string ISO;
        public string EXP;
        public Boolean barlow2;
        public Boolean barlow3;
        public Boolean barlow23;
        public DateTime shot_time;
        public DateTime GPS_shot_position;

        public DateTime elaboration_start_time;
        public DateTime elaboration_end_time;

        public Bitmap image;
        public Bitmap elaborated_image;
        
        public HoughLineTransformation lineTransform;
        public SusanCornersDetector susanTransform;

        public List<IntPoint> corners;
        public HoughLine[] lines;
        //Lista delle rette delle traiettorie presenti

        //Lista dei punti di discontinuità
        public List<List<System.Drawing.Point>> bresenhamsLines = new List<List<System.Drawing.Point>>();

        public List<System.Drawing.Point> black_points = new List<System.Drawing.Point>();
        public List<System.Drawing.Point> break_points = new List<System.Drawing.Point>();
        public List<BlackSegment> black_segments = new List<BlackSegment>();
        public List<System.Drawing.Point> trajectory_points = new List<System.Drawing.Point>();

        public TrajectorySet() { 

        }
    }
}
