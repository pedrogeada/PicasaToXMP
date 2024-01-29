Google Picasa has a powerful engine to detect faces in photos. However this information is written on a side file .picasa.ini and not on the image file itself. This means it can easily get lost if you reorganize your folders, merge folders or copy images into other folders.

Google Picasa does support an experimental feature to write the information to the XMP headers of the JPEG files. However, this feature does not work with large sets of files because it always restart from the same files, adds unnecessary changes to metadata and crashes if you have more than a few hundreds of thousand files

PicasaToXMP is a .Net console application to solve this problem by reading the .picasa.ini and writing any detected faces to the XMP headers of JPEG files.

PicasaToXMP allows to just check differences so that you know what is going to be written or do the full processing, including writing the XMP headers.

PicasaToXMP follows a conservative approach where faces are never deleted and in case of any error during the read or write process, it will stop any changes to that file.

Writing the XMP headers is done by using the external tool "exiftool", which is a well know console application by Phil Harbey to handle metadata and therefore quite safe in making sure metadata is written correctly to the image files

Once you write the XMP headers, you can still use Google Picasa to browse through specific faces or check the faces detected in specific images

This has been used personally to manage my own files but is provided with source code but **no guarantee whatsover that it will work for your case**. Make sure you have proper backups and check carefully if the changes done were according to what you would expect.

    Usage: PicasaToXMP <folder|filename> [-c <contactsFile>] [-e <ExifToolFile>] [-w] [-r]
        <folder|filename> is mandatory and it will execute on all files in folder or to the specific filename
        -c Optional parameter to provide the location of Picasa contacts file
        -e Optional parameter to provide the location of exiftool (assumed in the same folder or environment path)
        -w Writes the XMP to the image files (without -w it would be only to check the differences but not doing any changes)
        -r Runs recursively to all sub-folders

