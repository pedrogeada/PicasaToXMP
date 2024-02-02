using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using MetadataExtractor.Formats.Jpeg;
using System.Text.RegularExpressions;
using ExifLibrary;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using System.Globalization;
using XmpCore.Impl;

namespace PicasaToXMP
{
    class ImageFileInfo
    {
        public string FileName { get; set; }
        public int ImageWidth { get; set; } = 0;
        public int ImageHeight { get; set; } = 0;
        public int AppliedImageWidth { get; set; } = 0;
        public int AppliedImageHeight { get; set; } = 0;

        public List<FaceRegion> regions = new List<FaceRegion>();
        public List<FaceRegion> picasaRegions = new List<FaceRegion>();
        public List<FaceRegion> difRegions = new List<FaceRegion>();

        public ImageFileInfo(string name)
        {
            FileName = name;
        }

        public void ConsoleList()
        {
            foreach (FaceRegion region in regions)
            {
                Contact? c = GlobalVars.contacts.Contacts.Find(contact => contact.Id == region.ContactId);
                if (c!=null)
                    Console.WriteLine(c.Name);
            }

        }
        public bool ReadXMP()

        {
            try
            {
                Rectangle defrect = new Rectangle(0, 0, 0, 0);
                int width=0, height=0;

                IReadOnlyList<MetadataExtractor.Directory> dir=JpegMetadataReader.ReadMetadata(FileName);
                JpegDirectory jpeg=dir.OfType<JpegDirectory>().FirstOrDefault();
                if (jpeg!=null)
                {
                    ImageWidth=jpeg.GetImageWidth();
                    ImageHeight=jpeg.GetImageHeight();

                    width = ImageWidth;
                    height = ImageHeight;
                }

                XmpDirectory xmp = dir.OfType<XmpDirectory>().FirstOrDefault();
                if (xmp != null && xmp.XmpMeta != null)
                    foreach (var property in xmp.XmpMeta.Properties)
                    {
                        //&& property.Path.EndsWith("mwg-rs:Name") 
                        if (property.Path != null && property.Path.StartsWith("mwg-rs:Regions") && property.Value != null)
                        {
                            if (property.Path.EndsWith("mwg-rs:AppliedToDimensions/stDim:w"))
                            {
                                width = int.Parse(property.Value);
                                AppliedImageWidth = width;
                                if (width != ImageWidth)
                                {
                                    Console.WriteLine("Warning: AppliedToDimensions Width mismatch " + this.FileName);
                                    width = ImageWidth;
                                }
                            }

                            if (property.Path.EndsWith("mwg-rs:AppliedToDimensions/stDim:h"))
                            {
                                height = int.Parse(property.Value);
                                AppliedImageHeight = height;
                                if (height != ImageHeight)
                                {
                                    Console.WriteLine("Warning: AppliedToDimensions Height mismatch " + this.FileName);
                                    height = ImageHeight;
                                }
                            }

                            if (property.Path.Contains("mwg-rs:RegionList"))
                            {
                                string pattern = @"RegionList\[(\d+)\]/mwg-rs:(.*)";
                                //string pattern = @"\.mwg-rs:RegionList\[(\d+)\]\.mwg-rs:(Name|Type|Area\/stArea:x|Area\/stArea:y|Area\/stArea:w|Area\/stArea:h)";
                                Regex regex = new Regex(pattern);
                                Match match = regex.Match(property.Path.Substring("mwg-rs:Regions".Length));

                                if (match.Success)
                                {
                                    string numberString = match.Groups[1].Value;
                                    int number;
                                    string suffix;
                                    if (int.TryParse(numberString, out number))
                                    {
                                        suffix = match.Groups[2].Value;
                                        FaceRegion foundRegion = regions.Find(region => region.Index == number);

                                        if (foundRegion == null)
                                        {
                                            foundRegion = new FaceRegion(number, defrect, "");
                                            foundRegion.Rect = new Rectangle(0, 0, 0, 0);
                                            regions.Add(foundRegion);
                                        }

                                        Rectangle rect = foundRegion.Rect;

                                        switch (suffix)
                                        {
                                            case "Name":
                                                foundRegion.ContactId = GlobalVars.contacts.GetContactId(property.Value);
                                                break;

                                            case "Type":
                                                // check for "Face" type
                                                break;

                                            case "Area/stArea:x":
                                                rect.X = (int)Math.Round(double.Parse(property.Value) * width);
                                                foundRegion.Rect = rect;
                                                break;

                                            case "Area/stArea:y":
                                                rect.Y = (int)Math.Round(double.Parse(property.Value) * height);
                                                foundRegion.Rect = rect;
                                                break;

                                            case "Area/stArea:w":
                                                rect.Width = (int)Math.Round(double.Parse(property.Value) * width);
                                                foundRegion.Rect = rect;
                                                break;

                                            case "Area/stArea:h":
                                                rect.Height = (int)Math.Round(double.Parse(property.Value) * height);
                                                foundRegion.Rect = rect;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Reading XMP from" + FileName+" "+e.Message);
                return false;
            }

        }

        public void CalcDif()
        {
            difRegions.Clear();

            foreach(FaceRegion pRegion in picasaRegions)
            {
                if (pRegion.ContactId == "ffffffffffffffff")
                    continue;

                int find=regions.FindIndex(fr => fr.ContactId == pRegion.ContactId);
                if (find<0)
                {
                    int findr=regions.FindIndex(fr => FolderInfo.CalculateRectOverlapPercentage(fr.Rect,pRegion.Rect)>0.8);
                    if (findr < 0)
                        difRegions.Add(pRegion);
                    else
                        Console.WriteLine("Warning: Different face in similar rect "+this.FileName);
                }
                else
                {
                    bool lowOverlap = true;
                    foreach (var region in regions)
                    {
                        if (region.ContactId == pRegion.ContactId)
                        {
                            if (FolderInfo.CalculateRectOverlapPercentage(region.Rect, pRegion.Rect) >= 0.8)
                                lowOverlap = false;
                        }
                    }

                    if (lowOverlap)
                        Console.WriteLine("Warning: Face exists but with low overlap " + this.FileName);
                }

            }
        }

        public void WriteXMP()
        {
            if (difRegions.Count == 0)
                return;

            try
            {
                string regionlist = "";
                int count = 0;
                foreach(FaceRegion pRegion in difRegions) {
                    if (pRegion.ContactId == "ffffffffffffffff" || string.IsNullOrEmpty(GlobalVars.contacts.GetContactName(pRegion.ContactId)))
                        continue;

                    if (count == 0)
                        regionlist += "["+RegionListString(pRegion,regions.Count==0);
                    else
                        regionlist += "," + RegionListString(pRegion,regions.Count==0);

                    count++;
                }

                if (count>0)
                    regionlist += "]";

                if (regionlist!="")
                {
                    string arg = "";
                    if (regions.Count==0)
                        arg = "-overwrite_original -L -XMP-mwg-rs:RegionInfo=" +
                            "{AppliedToDimensions={W=" + ImageWidth + ",H=" + ImageHeight + ",Unit=pixel}," +
                            "RegionList=" + regionlist +
                            " \"" + this.FileName + "\"";
                    else
                        arg = "-overwrite_original -L -XMP-mwg-rs:RegionList+=" + regionlist +
                        " \"" + this.FileName + "\"";

                    ExecuteCommand(GlobalVars.exifToolFile, arg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string RegionListString(FaceRegion r, bool isNew)
        {
            int w = ImageWidth;
            int h = ImageHeight;
            if (!isNew)
            {
                if (AppliedImageWidth != 0)
                    w = AppliedImageWidth;

                if (AppliedImageHeight!=0)
                    h = AppliedImageHeight; 
            }

            return "{" +
                "Area=" +
                    "{W=" + ((double)r.Rect.Width / w).ToString("F6") +
                    ",H=" + ((double)r.Rect.Height / h).ToString("F6") +
                    ",X=" + ((double)r.Rect.X / w).ToString("F6") +
                    ",Y=" + ((double)r.Rect.Y / h).ToString("F6") +
                    ",Unit=normalized}," +
                "Name=\"" + GlobalVars.contacts.GetContactName(r.ContactId) + "\",Type=Face}";
        }

        public bool HasDif()
        {
            return difRegions.Count > 0;
        }

        static void ExecuteCommand(string command, string arguments)
        {
            // Create a ProcessStartInfo object to configure the process
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = command;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Create a new Process object and start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Read the output of the process
            string output = process.StandardOutput.ReadToEnd();

            // Wait for the process to finish and close it
            process.WaitForExit();
            process.Close();
        }
    }
}

