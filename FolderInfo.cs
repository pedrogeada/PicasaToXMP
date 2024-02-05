using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PicasaToXMP
{
    internal class FolderInfo
    {
        public string FolderName { get; private set; }
        public List<ImageFileInfo> Files { get; private set; }

        public FolderInfo(string folderName)
        {
            FolderName = folderName;
            Files = new List<ImageFileInfo>();
        }

        public void GetJpgFiles()
        {
            try
            {
                string[] allFiles = Directory.GetFiles(FolderName);

                foreach (var filePath in allFiles)
                {
                    if (IsJpgFile(filePath))
                    {
                        Files.Add(new ImageFileInfo(filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting JPG files: {ex.Message}");
            }
        }

        public void AddSpecificFile(string filePath)
        {
            try
            {
                if (IsJpgFile(filePath))
                {
                    Files.Add(new ImageFileInfo(filePath));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting JPG file: {ex.Message}");
            }
        }


        static bool IsJpgFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        public bool ReadPicasaIni()
        {
            try
            {
                string filePath = FolderName + "\\.picasa.ini";
                if (!File.Exists(filePath))
                    return true;

                string fileContent = File.ReadAllText(filePath);

                string pattern = @"\[(.+?)\]\s*faces=(.*)[\r]";
                Regex regex = new Regex(pattern, RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(fileContent);
                int index = 0;

                foreach (Match match in matches)
                {
                    string fileName = match.Groups[1].Value;
                    string[] regions = match.Groups[2].Value.Split(';');

                    ImageFileInfo fileInfo = Files.Find(file => Path.GetFileName(file.FileName) == fileName);
                    if (fileInfo != null)
                    {
                        foreach (string region in regions)
                        {
                            string[] parts = region.Split(',');
                            string rect64 = parts[0].Substring(parts[0].IndexOf("(") + 1, parts[0].IndexOf(")") - parts[0].IndexOf("(") - 1);
                            string contactId = parts[1];

                            Rectangle rect = GetRectangleFromRect64(rect64, fileInfo.ImageWidth, fileInfo.ImageHeight);
                            FaceRegion face = new FaceRegion(index++, rect, contactId);
                            fileInfo.picasaRegions.Add(face);
                        }
                    }
                }

                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        static Rectangle GetRectangleFromRect64(string rect64String, int imageSizeWidth, int imageSizeHeight)
        {
            // Parse the hexadecimal string to a long
            long rect64Value = long.Parse(rect64String, System.Globalization.NumberStyles.HexNumber);

            // Extract individual numbers from the rect64Value
            int x1 = (int)((rect64Value >> 48) & 0xFFFF);
            int y1 = (int)((rect64Value >> 32) & 0xFFFF);
            int x2 = (int)((rect64Value >> 16) & 0xFFFF);
            int y2 = (int)(rect64Value & 0xFFFF);

            int x = (int) Math.Round((x1 + x2) / 2.0);
            int y = (int) Math.Round((y1 + y2) / 2.0);
            int w = Math.Abs(x2 - x1);
            int h = Math.Abs(y2 - y1);

            // Calculate actual values based on image size
            int actualX = (int) Math.Round((double)x / 65536 * imageSizeWidth);
            int actualY = (int) Math.Round((double)y / 65536 * imageSizeHeight);
            int actualW = (int) Math.Round((double)w / 65536 * imageSizeWidth);
            int actualH = (int) Math.Round((double)h / 65536 * imageSizeHeight);

            // Create and return the Rectangle object
            return new Rectangle(actualX, actualY, actualW, actualH);
        }

        public void ConsoleList()
        {
            foreach (ImageFileInfo f in Files)
            {
                Console.WriteLine(f.FileName);
                if (f.regions.Count>0)
                    Console.WriteLine("XMP:");
                foreach (FaceRegion region in f.regions)
                {
                    string name = GlobalVars.contacts.GetContactName(region.ContactId);
                    Console.WriteLine(name + " (" + region.Rect.X + "," + region.Rect.Y + "," + region.Rect.Width + "," + region.Rect.Height + ")");
                }

                if (f.picasaRegions.Count>0)
                    Console.WriteLine("Picasa:");
                foreach (FaceRegion region in f.picasaRegions)
                {
                    string name = GlobalVars.contacts.GetContactName(region.ContactId);
                    Console.WriteLine(name + " (" + region.Rect.X + "," + region.Rect.Y + "," + region.Rect.Width + "," + region.Rect.Height + ")");
                }

                if (f.difRegions.Count > 0)
                    Console.WriteLine("Dif:");
                foreach (FaceRegion region in f.difRegions)
                {
                    string name = GlobalVars.contacts.GetContactName(region.ContactId);
                    Console.WriteLine(name + " (" + region.Rect.X + "," + region.Rect.Y + "," + region.Rect.Width + "," + region.Rect.Height + ")");
                }
            }
        }

        public void ConsoleListDif()
        {
            foreach (ImageFileInfo f in Files)
            {
                foreach (FaceRegion region in f.difRegions)
                {
                    string name = GlobalVars.contacts.GetContactName(region.ContactId);
                    if (!String.IsNullOrEmpty(name))
                        Console.WriteLine(f.FileName + ": " + name + " (" + region.Rect.X + "," + region.Rect.Y + "," + region.Rect.Width + "," + region.Rect.Height + ")");
                }
            }
        }

        public void ConsoleListXmp()
        {
            foreach (ImageFileInfo f in Files)
            {
                foreach (FaceRegion region in f.regions)
                {
                    string name = GlobalVars.contacts.GetContactName(region.ContactId);
                    if (!String.IsNullOrEmpty(name))
                    {
                        int areaperm = -1;
                        if (f.ImageHeight > 0 && f.ImageWidth > 0)
                        {
                            areaperm = (int)(Math.Round((double)region.Rect.Width * region.Rect.Height * 1000 / ((double)f.ImageWidth * f.ImageHeight), 0) + 0.1);
                        }

                        Console.WriteLine(f.FileName + "\t" + name + "\t" + areaperm.ToString());
                    }
                }
            }
        }


        static public double CalculateRectOverlapPercentage(Rectangle rect1, Rectangle rect2)
        {
            Rectangle intersection = Rectangle.Intersect(rect1, rect2);
            int intersectionArea = intersection.Width * intersection.Height;
            int averageArea = (rect1.Width * rect1.Height + rect2.Width * rect2.Height) / 2;

            double overlapPercentage = (double)intersectionArea / averageArea * 100;
            return overlapPercentage;
        }

    }
}
