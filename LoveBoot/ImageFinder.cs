using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace LoveBoot
{
    // http://www.emgu.com/forum/viewtopic.php?p=10417#p10417
    // original by "bangonkali"

    /*
        Re: match template
        Post by bangonkali » Fri Jan 09, 2015 3:58 am

        This is what I use. I basically have to throttle the threshold in order to get desired results. 

        This can detect multiple occurrences of a certain image in the base image. 
    */

    public class ImageFinder
    {
        private List<Rectangle> rectangles;
        private Stopwatch stopwatch;
        private Bgr fillColor;

        public double Threshold { get; set; }

        public Dictionary<object, Image<Bgr, Byte>> SubImages = new Dictionary<object, Image<Bgr, byte>>(); 

        public List<Rectangle> Rectangles
        {
            get { return rectangles; }
        }

        public ImageFinder(double threshold)
        {
            rectangles = new List<Rectangle>();
            stopwatch = new Stopwatch();
            Threshold = threshold;
            fillColor = new Bgr(Color.Magenta);
        }

        public ImageFinder()
        {
            rectangles = new List<Rectangle>();
            stopwatch = new Stopwatch();
            Threshold = 0.85;
            fillColor = new Bgr(Color.Magenta);
        }

        /// <summary>
        /// Draw rectangles on the ResultImage.
        /// </summary>
        /// <returns>The image with overlayed rectangles.</returns>
        /*public void DrawRectanglesOnImage()
        {
            ResultImage = BaseImage.Copy();
            foreach (var rectangle in this.rectangles)
            {
                ResultImage.Draw(rectangle, new Bgr(Color.Red), 1);

            }
        }*/

        public IDictionary<object, Rectangle[]> FindAllMatches(Image<Bgr, Byte> source, string match = "", bool copy = true, string ignore = "") // todo: proper order
        {
            var matches = new ConcurrentDictionary<object, Rectangle[]>();

            Image<Bgr, Byte> sourceImage = copy ? source.Copy() : source;

            foreach (KeyValuePair<object, Image<Bgr, Byte>> subImageKeyValuePair in SubImages)
            {
                if (match.Length > 0 && !subImageKeyValuePair.Key.ToString().Contains(match)) continue;
                if (ignore.Length > 0 && subImageKeyValuePair.Key.ToString().Contains(ignore)) continue;

                Rectangle[] subImageMatches = FindMatches(sourceImage, subImageKeyValuePair.Value, copy);
                matches[subImageKeyValuePair.Key] = subImageMatches;
            }

            /*Parallel.ForEach(SubImages, (subImageKeyValuePair) =>
            {
                if (match.Length > 0 && !subImageKeyValuePair.Key.ToString().Contains(match)) return;
                if (ignore.Length > 0 && subImageKeyValuePair.Key.ToString().Contains(ignore)) return;

                Rectangle[] subImageMatches = FindMatches(sourceImage, subImageKeyValuePair.Value, copy);
                matches[subImageKeyValuePair.Key] = subImageMatches;
                //matches.Add(subImageKeyValuePair.Key, subImageMatches);
            });*/

            return matches;
        } 

        public Rectangle[] FindMatches(Image<Bgr, Byte> source, Image<Bgr, Byte> target, bool copy = true)
        {
            //rectangles = new List<Rectangle>();
            rectangles.Clear();
            //stopwatch = new Stopwatch();
            //stopwatch.Start();

            Image<Bgr, Byte> imgSrc = copy ? source.Copy() : source;

            // FindImage all occurences of imgFind
            
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;

            while (true)
            {
                using (
                    Image<Gray, float> result = imgSrc.MatchTemplate(target,
                        Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
                {
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                   
                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                    if (maxValues[0] < Threshold || minValues[0] == maxValues[0]) break;
                    // This is a match. Do something with it, for example draw a rectangle around it.
                    Rectangle match = new Rectangle(maxLocations[0], target.Size);

                    // Fill the drawing with red in order to ellimate this as a source.
                    imgSrc.Draw(match, fillColor, -1);

                    // Add the found rectangle to the results.
                    rectangles.Add(match);
                }
            }

            if(copy) imgSrc.Dispose();

            //stopwatch.Stop();

            return rectangles.ToArray();
        }

        /*public Rectangle[] FindMatches(Image<Bgr, Byte> source, Image<Bgr, Byte> target)
        {
            rectangles = new List<Rectangle>();
            stopwatch = new Stopwatch();
            stopwatch.Start();

            Image<Bgr, Byte> imgSrc = source.Copy();

            // FindImage all occurences of imgFind

            while (true)
            {
                using (
                    Image<Gray, float> result = imgSrc.MatchTemplate(target,
                        Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                    if (maxValues[0] < Threshold || minValues[0] == maxValues[0]) break;
                    // This is a match. Do something with it, for example draw a rectangle around it.
                    Rectangle match = new Rectangle(maxLocations[0], target.Size);

                    // Fill the drawing with red in order to ellimate this as a source.
                    imgSrc.Draw(match, new Bgr(Color.Magenta), -1);

                    // Add the found rectangle to the results.
                    rectangles.Add(match);
                }
            }

            imgSrc.Dispose();

            stopwatch.Stop();

            return rectangles.ToArray();
        }*/

    }
}