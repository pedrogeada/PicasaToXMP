using PicasaToXMP;
using System.Drawing;

string folderName = "";
string contactsFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Google\\Picasa2\\contacts\\contacts.xml";
bool argWrite = false;
bool argRecursive = false;
string singleFilePath = "";
List<FolderInfo> folders=new List<FolderInfo>();

void WriteUsage()
{
    Console.WriteLine("Usage: PicasaToXMP <folder|filename> [-c <contactsFile>] [-e <ExifToolFile>] [-w] [-r]");
    Console.WriteLine("    <folder|filename> is mandatory and it will execute on all files in folder or to the specific filename");
    Console.WriteLine("    -c Optional parameter to provide the location of Picasa contacts file");
    Console.WriteLine("    -e Optional parameter to provide the location of exiftool");
    Console.WriteLine("    -w Writes the XMP to the image files");
    Console.WriteLine("    -r Runs recursively to all sub-folders");
}

static List<string> GetSubFolders(string folderPath)
{
    List<string> subFolders = new List<string>();

    try
    {
        string[] subFolderPaths = Directory.GetDirectories(folderPath);
        subFolders.AddRange(subFolderPaths);

        foreach(string subFolder in subFolderPaths)
            subFolders.AddRange(GetSubFolders(subFolder));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting sub-folders: {ex.Message}");
    }

    return subFolders;
}

if (args.Length > 0)
    folderName = args[0];

if (folderName=="")
{
    WriteUsage();
    return;
}

for (int i = 1; i < args.Length; i++)
{
    string arg = args[i];

    switch (arg)
    {
        case "-c":
            if (i + 1 < args.Length)
            {
                contactsFile = args[i + 1];
                i++; 
            }
            else
            {
                Console.WriteLine("Error: Missing contacts file name after -c option.");
                return;
            }
            break;

        case "-e":
            if (i + 1 < args.Length)
            {
                GlobalVars.exifToolFile = args[i + 1];
                i++;
            }
            else
            {
                Console.WriteLine("Error: Missing exiftool file name after -e option.");
                return;
            }
            break;

        case "-w":
            argWrite = true;
            break;

        case "-r":
            argRecursive = true;
            break;
    }
}

GlobalVars.contacts.ReadContactsFromFile(contactsFile);
//GlobalVars.contacts.WriteContacts();

List<string> foldersNames = new List<string>();
if (!Directory.Exists(folderName))
{
    singleFilePath = folderName;
    folderName = Path.GetDirectoryName(singleFilePath);
}
else
{
    if (argRecursive)
        foldersNames = GetSubFolders(folderName);
}
foldersNames.Add(folderName);

int count = 0;
int countDifFiles = 0;
int countDif = 0;

foreach (string folder in foldersNames)
{
    FolderInfo folderInfo = new FolderInfo(folder);
    if (singleFilePath != "")
        folderInfo.AddSpecificFile(singleFilePath);
    else
        folderInfo.GetJpgFiles();
    folders.Add(folderInfo);

    bool success = true;
    //Console.WriteLine(folder);
    foreach (ImageFileInfo file in folderInfo.Files)
    {
        count++;

        if (!file.ReadXMP())
        {
            Console.WriteLine("Error reading XMP");
            success = false;
            continue;
        }
    }

    if (!folderInfo.ReadPicasaIni())
    {
        Console.WriteLine("Error reading .picasa.ini");
        success = false;
    }

    if (success)
    {
        foreach (ImageFileInfo file in folderInfo.Files) { 
            file.CalcDif();
            countDif += file.difRegions.Count-file.difRegions.Count(region => region.ContactId == "ffffffffffffffff"); 
            if (file.difRegions.Count > 0) 
                countDifFiles++;

            if (argWrite && file.HasDif())
                file.WriteXMP();
        }
        
        folderInfo.ConsoleListDif();
    }
}

Console.WriteLine(count.ToString() + " files analyzed; " + countDifFiles.ToString() + " files with differences; " + countDif.ToString() + " differences");
